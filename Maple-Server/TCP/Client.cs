using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Maple_Server.Crypto;
using Maple_Server.HTTP;
using Maple_Server.Logging;
using Maple_Server.Mapping;
using Maple_Server.Packets;
using Maple_Server.Packets.Requests;
using Maple_Server.Packets.Responses;

namespace Maple_Server.TCP;

public class Client : IDisposable
{
    private readonly TcpClient _client;
    private Stream _stream;
    private PacketStreamer _packetStreamer;

    private readonly byte[] _key = new byte[32];
    private readonly byte[] _iv = new byte[16];

    private string _sessionToken = string.Empty;
    public string SessionToken => _sessionToken;

    private bool _handledHandshake;

    private readonly List<long> _epochs;

    private ImageMapper _imageMapper;

    public IntPtr Handle => _client.Client.Handle;
    public IPAddress IP => (_client.Client.RemoteEndPoint as IPEndPoint)?.Address;

    public Client(TcpClient client)
    {
        _client = client;

        _epochs = new List<long>();
        
        Random random = new Random();
        random.NextBytes(_key);
        random.NextBytes(_iv);
    }

    public void Connect()
    {
        Logger.Instance.Log(LogSeverity.Info, $"New client connected. [{Handle}]({IP})");
        
        _stream = _client.GetStream();
        _packetStreamer = new PacketStreamer(receiveCallback);
    }

    public void Disconnect()
    {
        if (!_client.Connected)
        {
            Logger.Instance.Log(LogSeverity.Warning, "Tried to disconnect a client that was already disconnected!");
            
            return;
        }
        
        Logger.Instance.Log(LogSeverity.Info, $"Client disconnected. [{Handle}]({IP})");
        
        _client.Close();
    }

    public void MainThread()
    {
        while (_client.Connected)
        {
            var buffer = new byte[512];
            int bytesRead = _stream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
            {
                Logger.Instance.Log(LogSeverity.Info, $"Client disconnected. [{Handle}]({IP})");
                _client.Close();

                return;
            }

            _packetStreamer.Receive(buffer, bytesRead);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _stream.Dispose();
    }

    private void receiveCallback(List<byte> buffer)
    {
        PacketType type = (PacketType)buffer.ToArray()[0];
        byte[] encryptedPayload = buffer.Count > 1 ? buffer.Take(new Range(1, buffer.Count)).ToArray() : new byte[]{};

        switch (type)
        {
            case PacketType.Handshake:
                handleHandshake(encryptedPayload);
                break;
            case PacketType.Login:
                handleLogin(encryptedPayload);
                break;
            case PacketType.LoaderStream:
                handleLoaderStream(encryptedPayload);
                break;
            case PacketType.ImageStreamStageOne:
                handleImageStreamStageOne(encryptedPayload);
                break;
            case PacketType.ImageStreamStageTwo:
                handleImageStreamStageTwo(encryptedPayload);
                break;
            case PacketType.Heartbeat:
                handleHeartbeat(encryptedPayload);
                break;
            default:
                Logger.Instance.Log(LogSeverity.Error, $"Client [{Handle}]({IP}) sent unknown packet!");
                Disconnect();
                break;
        }
    }

    private void handleHandshake(byte[] encryptedPayload)
    {
        var a = Encoding.ASCII.GetString(CryptoProvider.Instance.XOR(encryptedPayload));
        HandshakeRequest payload = JsonSerializer.Deserialize<HandshakeRequest>(Encoding.ASCII.GetString(CryptoProvider.Instance.XOR(encryptedPayload)));

        if (_handledHandshake)
        {
            Logger.Instance.Log(LogSeverity.Warning, $"Client [{Handle}]({IP}) sent multiple handshake packets!");
            Disconnect();

            return;
        }
        
        if (_epochs.Contains(payload.Epoch) || !_epochs.All(e => e < payload.Epoch))
        {
            Logger.Instance.Log(LogSeverity.Warning, $"Client [{Handle}]({IP}) sent invalid epoch!");
            Disconnect();

            return;
        }

        _epochs.Add(payload.Epoch);

        HandshakeResponse responsePayload = new HandshakeResponse
        {
            Key = _key,
            IV = _iv
        };

        string responseJsonPayload = JsonSerializer.Serialize(responsePayload);
        
        List<byte> responsePacket = new();
        responsePacket.Add((byte)PacketType.Handshake);
        responsePacket.AddRange(CryptoProvider.Instance.RsaEncrypt(Encoding.ASCII.GetBytes(responseJsonPayload)));
        
        _packetStreamer.Send(responsePacket.ToArray(), _stream);

        _handledHandshake = true;
    }

    private void handleLogin(byte[] encryptedPayload)
    {
        LoginRequest payload = JsonSerializer.Deserialize<LoginRequest>(Encoding.ASCII.GetString(CryptoProvider.Instance.AesDecrypt(encryptedPayload, _key, _iv)));

        var loginParams = new Dictionary<string, string>
        {
            { "t", "0" },
            { "u", payload.Username },
            { "p", payload.Password },
            { "v", payload.LoaderVersion },
            { "h", payload.HWID },
            { "i", IP.ToString() }
        };

        var loginResponse = JsonObject.Parse(HTTPWrapper.Instance.Post("https://maple.software/backend/auth_new", loginParams));
        
        LoginResponse responsePayload = new LoginResponse();
        responsePayload.Result = (int)loginResponse["code"] < 0 || (int)loginResponse["code"] > 4
            ? LoginResult.UnknownError
            : (LoginResult)(uint)loginResponse["code"];

        switch (responsePayload.Result)
        {
            case LoginResult.Success:
                Logger.Instance.Log(LogSeverity.Info, $"Client [{Handle}]({IP}) successfully logged in under \"{payload.Username}\" username.");
                break;
            default:
                Logger.Instance.Log(LogSeverity.Warning, $"Client [{Handle}]({IP}) failed to log in under \"{payload.Username}\" username. ({responsePayload.Result.ToString()})");
                break;
        }

        if (responsePayload.Result == (uint)LoginResult.Success)
        {
            responsePayload.SessionToken = (string)loginResponse["SessionToken"];
            responsePayload.DiscordID = (string)loginResponse["DiscordID"];
            responsePayload.DiscordAvatarHash = (string)loginResponse["DiscordAvatarHash"];

            responsePayload.Games = new List<Game>();
            responsePayload.Cheats = new List<Cheat>();
            
            foreach (var game in loginResponse["Games"].AsArray())
            {
                responsePayload.Games.Add(new Game()
                {
                    ID = (uint)game["ID"],
                    Name = (string)game["Name"]
                });
            }
            
            foreach (var cheat in loginResponse["Cheats"].AsArray())
            {
                responsePayload.Cheats.Add(new Cheat()
                {
                    ID = (uint)cheat["ID"],
                    GameID = (uint)cheat["GameID"],
                    Name = (string)cheat["Name"],
                    ReleaseStreams = ((string)cheat["ReleaseStreams"]).Split(',').ToList(),
                    StartingPrice = (uint)cheat["StartingPrice"],
                    Status = (CheatStatus)(uint)cheat["Status"],
                    ExpiresOn = (string)cheat["ExpiresOn"]
                });
            }

            _sessionToken = responsePayload.SessionToken;
        }

        string responseJsonPayload = JsonSerializer.Serialize(responsePayload);

        List<byte> responsePacket = new();
        responsePacket.Add((byte)PacketType.Login);
        responsePacket.AddRange(CryptoProvider.Instance.AesEncrypt(Encoding.ASCII.GetBytes(responseJsonPayload), _key, _iv));
                
        _packetStreamer.Send(responsePacket.ToArray(), _stream);
    }

    private void handleLoaderStream(byte[] encryptedPayload)
    {
        LoaderStreamRequest payload = JsonSerializer.Deserialize<LoaderStreamRequest>(Encoding.ASCII.GetString(CryptoProvider.Instance.AesDecrypt(encryptedPayload, _key, _iv)));
        
        var streamParams = new Dictionary<string, string>
        {
            { "t", "1" },
            { "st", "0" },
            { "s", payload.SessionToken },
            { "c", payload.CheatID.ToString() }
        };

        var streamResponse = JsonObject.Parse(HTTPWrapper.Instance.Post("https://maple.software/backend/auth_new", streamParams));

        LoaderStreamResponse responsePayload = new LoaderStreamResponse();
        
        string targetFilePath = $@"C:\MapleStorage\Loaders\{payload.CheatID}.exe";
        
        if (payload.SessionToken != _sessionToken || (int)streamResponse["code"] == 5)
            responsePayload.Result = LoaderStreamResult.InvalidSession;
        else if ((int)streamResponse["code"] == 6)
            responsePayload.Result = LoaderStreamResult.NotSubscribed;
        else if (!File.Exists(targetFilePath) || ((int)streamResponse["code"] != 0 && (int)streamResponse["code"] != 5 && (int)streamResponse["code"] != 6))
            responsePayload.Result = LoaderStreamResult.UnknownError;
        else
        {
            responsePayload.Result = LoaderStreamResult.Success;
            
            byte[] loaderData = File.ReadAllBytes(targetFilePath);

            responsePayload.LoaderData = loaderData;
        }

        if (responsePayload.Result == LoaderStreamResult.Success)
            Logger.Instance.Log(LogSeverity.Info, $"Successfully sent loader stream to client [{Handle}]({IP}).");
        else
            Logger.Instance.Log(LogSeverity.Warning, $"Failed to send loader stream to client [{Handle}]({IP}). ({responsePayload.Result})");

        string responseJsonPayload = JsonSerializer.Serialize(responsePayload);
        
        List<byte> responsePacket = new();
        responsePacket.Add((byte)PacketType.LoaderStream);
        responsePacket.AddRange(CryptoProvider.Instance.AesEncrypt(Encoding.ASCII.GetBytes(responseJsonPayload), _key, _iv));
                
        _packetStreamer.Send(responsePacket.ToArray(), _stream);
    }

    private void handleImageStreamStageOne(byte[] encryptedPayload)
    {
        ImageStreamStageOneRequest payload = JsonSerializer.Deserialize<ImageStreamStageOneRequest>(Encoding.ASCII.GetString(CryptoProvider.Instance.AesDecrypt(encryptedPayload, _key, _iv)));
        
        var streamParams = new Dictionary<string, string>
        {
            { "t", "1" },
            { "st", "1" },
            { "s", payload.SessionToken },
            { "c", payload.CheatID.ToString() }
        };

        var streamResponse = JsonObject.Parse(HTTPWrapper.Instance.Post("https://maple.software/backend/auth_new", streamParams));

        ImageStreamStageOneResponse responsePayload = new ImageStreamStageOneResponse();
        
        string targetFilePath = $@"C:\MapleStorage\Cheats\{payload.CheatID}_{payload.ReleaseStream}.dll";
        
        if (payload.SessionToken != _sessionToken || (int)streamResponse["code"] == 5)
            responsePayload.Result = ImageStreamStageOneResult.InvalidSession;
        else if ((int)streamResponse["code"] == 6)
            responsePayload.Result = ImageStreamStageOneResult.NotSubscribed;
        else if (!File.Exists(targetFilePath) || ((int)streamResponse["code"] != 0 && (int)streamResponse["code"] != 5 && (int)streamResponse["code"] != 6))
            responsePayload.Result = ImageStreamStageOneResult.UnknownError;
        else
        {
            responsePayload.Result = ImageStreamStageOneResult.Success;

            _imageMapper = new ImageMapper(File.ReadAllBytes(targetFilePath));

            responsePayload.ImageSize = _imageMapper.GetSizeOfImage();
            responsePayload.Imports = _imageMapper.GetImports();
        }

        if (responsePayload.Result == ImageStreamStageOneResult.Success)
            Logger.Instance.Log(LogSeverity.Info, $"Successfully sent stage one of image stream to client [{Handle}]({IP}).");
        else
            Logger.Instance.Log(LogSeverity.Warning, $"Failed to send stage one of image stream to client [{Handle}]({IP}). ({responsePayload.Result})");

        string responseJsonPayload = JsonSerializer.Serialize(responsePayload);
        
        List<byte> responsePacket = new();
        responsePacket.Add((byte)PacketType.ImageStreamStageOne);
        responsePacket.AddRange(CryptoProvider.Instance.AesEncrypt(Encoding.ASCII.GetBytes(responseJsonPayload), _key, _iv));
                
        _packetStreamer.Send(responsePacket.ToArray(), _stream);
    }
    
    private void handleImageStreamStageTwo(byte[] encryptedPayload)
    {
        ImageStreamStageTwoRequest payload = JsonSerializer.Deserialize<ImageStreamStageTwoRequest>(Encoding.ASCII.GetString(CryptoProvider.Instance.AesDecrypt(encryptedPayload, _key, _iv)));
        
        _imageMapper.SetImageBaseAddress(payload.ImageBaseAddress);
        _imageMapper.SetImports(payload.ResolvedImports);

        ImageStreamStageTwoResponse responsePayload = new ImageStreamStageTwoResponse
        {
            EntryPointOffset = _imageMapper.GetEntryPointOffset(),
            Image = _imageMapper.MapImage()
        };
        
        Logger.Instance.Log(LogSeverity.Info, $"Successfully sent stage two of image stream to client [{Handle}]({IP}).");

        string responseJsonPayload = JsonSerializer.Serialize(responsePayload);
        
        List<byte> responsePacket = new();
        responsePacket.Add((byte)PacketType.ImageStreamStageTwo);
        responsePacket.AddRange(CryptoProvider.Instance.AesEncrypt(Encoding.ASCII.GetBytes(responseJsonPayload), _key, _iv));
                
        _packetStreamer.Send(responsePacket.ToArray(), _stream);
    }

    private void handleHeartbeat(byte[] encryptedPayload)
    {
        HeartbeatRequest payload = JsonSerializer.Deserialize<HeartbeatRequest>(Encoding.ASCII.GetString(CryptoProvider.Instance.AesDecrypt(encryptedPayload, _key, _iv)));

        if (_epochs.Contains(payload.Epoch) || !_epochs.All(e => e < payload.Epoch))
        {
            Logger.Instance.Log(LogSeverity.Warning, $"Client [{Handle}]({IP}) sent invalid epoch!");
            Disconnect();

            return;
        }

        _epochs.Add(payload.Epoch);
        
        var heartbeatParams = new Dictionary<string, string>
        {
            { "t", "2" },
            { "s", payload.SessionToken }
        };

        var heartbeatResponse = JsonObject.Parse(HTTPWrapper.Instance.Post("https://maple.software/backend/auth_new", heartbeatParams));

        HeartbeatResponse responsePayload = new HeartbeatResponse
        {
            Result = (int)heartbeatResponse["code"] == 0 ? HeartbeatResult.Success : ((int)heartbeatResponse["code"] == 5 ? HeartbeatResult.InvalidSession : HeartbeatResult.UnknownError)
        };
        
        if (responsePayload.Result == HeartbeatResult.Success)
            Logger.Instance.Log(LogSeverity.Info, $"Client's [{Handle}]({IP}) heartbeat succeeded.");
        else
            Logger.Instance.Log(LogSeverity.Warning, $"Client's [{Handle}]({IP}) heartbeat failed!");
        
        string responseJsonPayload = JsonSerializer.Serialize(responsePayload);
        
        List<byte> responsePacket = new();
        responsePacket.Add((byte)PacketType.Heartbeat);
        responsePacket.AddRange(CryptoProvider.Instance.AesEncrypt(Encoding.ASCII.GetBytes(responseJsonPayload), _key, _iv));
                
        _packetStreamer.Send(responsePacket.ToArray(), _stream);
    }
}
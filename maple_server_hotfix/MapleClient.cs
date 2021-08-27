using System;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using maple_server_hotfix.Logging;
using maple_server_hotfix.Services;

namespace maple_server_hotfix
{
    public class MapleClient : IDisposable
    {
        private readonly IClientConnection _client;
        private readonly IFileProvider _fileProvider;
        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Random _rng = new Random();
        private Stream _stream;

        public readonly IPAddress Ip;
        public readonly ICryptoProvider Crypto;
        public readonly byte[] Iv = new byte[16];
        public readonly byte[] Key = new byte[32];
        public string SessionId;

        public MapleClient(IClientConnection client, IFileProvider fileProvider, ICryptoProvider crypto, HttpClient http)
        {
            _client = client;
            _fileProvider = fileProvider;
            Crypto = crypto;
            _http = http;
            Ip = client.IpAddress;

            // TODO: should receive logger factory actually
            _logger = new Logger(Ip.ToString());
        }

        public bool ReplacedByNewClient { get; set; }

        public void StartConnection()
        {
            _logger.Info("StartConnection");
            _stream = _client.GetStream();
        }

        public void Disconnect(string reason)
        {
            if (!_client.Connected)
            {
                _logger.Warn("Tried to disconnect a client that was already disconnected!");
                return;
            }

            GetFatalErrorPacket(reason).WriteToStream(_stream);
            _client.Close();
        }

        public void RunLoopBlocking()
        {
            // 🤝 handshake 🤝
            // Sends the client a Key, IV and Timestamp for future use
            HandleHandshake();

            var buffer = new byte[4096];
            while (_client.Connected)
            {
                int read = _stream.Read(buffer, 0, 4096);
                if (read <= 0)
                {
                    _client.Close();
                    return;
                }

                var splitData = Split(buffer);
                var operation = splitData[0][0];
                byte[] decrypted = Crypto.AesDecrypt(splitData[1], Key, Iv);

                switch (operation)
                {
                    // 🔒 login 🔒
                    case 0xF3:
                    {
                        HandleLogin(decrypted);
                        break;
                    }
                    // 🦀 dll stream 🦀
                    case 0xB1:
                    {
                        var success = HandleDllStream(decrypted);
                        if (success)
                            return; // dll streamed, exit out. injected dll will make its own connection
                        break;
                    }
                    // ♥ heartbeat ♥
                    case 0xC2:
                    {
                        // TODO: is this fixup needed here? should it always be used?
                        decrypted = fixup(decrypted);
                        HandleHeartbeat(decrypted);
                        break;
                    }
                }
            }
        }

        internal void HandleHandshake()
        {
            int readByte = _stream.ReadByte();
            if (readByte == -1)
                throw new Exception("Failed to read handshake: stream ended");
            if (readByte != 0xA0)
                throw new Exception($"Received wrong handshake byte: 0x{readByte:X2}");

            _rng.NextBytes(Key);
            _rng.NextBytes(Iv);

            long unixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            unixTimeMilliseconds /= 2;
            unixTimeMilliseconds ^= 0xDA; // "obfuscation"

            var toEnc = new List<byte>();
            toEnc.AddRange(Iv); // "obfuscation"
            toEnc.AddRange(Key);
            toEnc.AddRange(Encoding.ASCII.GetBytes(unixTimeMilliseconds.ToString()));

            var encryptedKeyInfo = Crypto.RsaEncrypt(toEnc.ToArray(), out int payloadLen);

            // sending OP | LEN | DATA
            var packetData = new DelimitedPacketBuffer();
            packetData.AddByte(0xA0);
            packetData.Add(payloadLen.ToString());
            packetData.Add(encryptedKeyInfo);
            packetData.WriteToStream(_stream);
        }

        internal void HandleLogin(byte[] decrypted)
        {
            // DATA: enc(HWID | HASH | USER | PASS)
            var stuff = Split(decrypted);
            var hwid = Encoding.ASCII.GetString(stuff[0]);
            var hash = Encoding.ASCII.GetString(stuff[1]);
            var user = Encoding.ASCII.GetString(stuff[2]);
            var pass = Encoding.ASCII.GetString(fixup(stuff[3]));

            Console.WriteLine(_client.IpAddress + " => " + user);

            var values = new Dictionary<string, string>
            {
                { "t", "0" },
                { "u", user },
                { "p", pass },
                { "h", hwid },
                { "ha", "55F3B22D9107DF7B2CC0124A5FCB66E376A5C94F3991B3EB59D20881D58E21EE" },
            };

            var content = new FormUrlEncodedContent(values);

            var response = _http.PostAsync("https://maple.software/backend/auth", content)
                .GetAwaiter().GetResult();

            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var json = JObject.Parse(responseString);
            var errorCode = (int)json["code"];

            /*
                            Success = 0x0,
                            IncorrectCredentials = 0x1,
                            HashMismatch = 0x2,
                            HWIDMismatch = 0x3,
                            Banned = 0x4,
                            InternalError = 0x5
                         */

            if (errorCode == 0 || errorCode == 0x2) // all is gud
            {
                var packet = new DelimitedPacketBuffer();
                packet.AddByte(0xF3);

                var sessionInfo = new DelimitedPacketBuffer();
                sessionInfo.AddByte((byte)errorCode); // guaranteed to be 0
                sessionInfo.Add((string)json["sessionID"]);
                packet.Add(sessionInfo.GetEncryptedBuffer(this));

                var gameList = new DelimitedPacketBuffer();
                foreach (var game in json["games"])
                {
                    var id = (int)game["ID"];
                    var name = (string)game["Name"];
                    var moduleName = (string)game["ModuleName"];

                    var clearGameData = new DelimitedPacketBuffer();
                    clearGameData.Add(id.ToString());
                    clearGameData.Add(name);
                    clearGameData.Add(moduleName);
                    gameList.Add(clearGameData.GetEncryptedBuffer(this));
                }

                packet.Add(gameList.GetEncryptedBuffer(this));

                var cheatList = new DelimitedPacketBuffer();
                foreach (var cheat in json["cheats"])
                {
                    var id = (int)cheat["ID"];
                    var gameid = (int)cheat["GameID"];
                    var relstreams = (string)cheat["ReleaseStreams"];
                    var name = (string)cheat["Name"];
                    var price = (int)cheat["Price"];
                    var status = (int)cheat["Status"];
                    var features = (string)cheat["Features"];
                    var expiresAt = (string)cheat["ExpiresAt"];

                    var clearCheatData = new DelimitedPacketBuffer();
                    clearCheatData.Add(id.ToString());
                    clearCheatData.Add(gameid.ToString());
                    clearCheatData.Add(relstreams);
                    clearCheatData.Add(name);
                    clearCheatData.Add(price.ToString());
                    clearCheatData.Add(status.ToString());
                    clearCheatData.Add(features);
                    clearCheatData.Add(expiresAt);
                    cheatList.Add(clearCheatData.GetEncryptedBuffer(this));
                }

                packet.Add(cheatList.GetEncryptedBuffer(this));

                SessionId = (string)json["sessionID"];

                packet.WriteToStream(_stream);
            }
            else
            {
                if (errorCode < 1 || errorCode > 4) // unknown error -> 5
                    errorCode = 5;

                var errorCodeData = new DelimitedPacketBuffer();
                errorCodeData.AddByte((byte)errorCode);

                var packet = new DelimitedPacketBuffer();
                packet.AddByte(0xF3);
                byte[] encrypt = errorCodeData.GetEncryptedBuffer(this);

                packet.Add(errorCodeData.GetEncryptedBuffer(this));
                packet.WriteToStream(_stream);
            }
        }

        internal bool HandleDllStream(byte[] decrypted)
        {
            var splitDecryptedBuffer = Split(decrypted);
            splitDecryptedBuffer[1] = fixup(splitDecryptedBuffer[1]);

            var cheatId = Encoding.ASCII.GetString(splitDecryptedBuffer[0]);
            var releaseStream = Encoding.ASCII.GetString(splitDecryptedBuffer[1]);

            var values = new Dictionary<string, string>
            {
                { "t", "1" },
                { "e", "0" },
                { "s", SessionId },
            };

            var content = new FormUrlEncodedContent(values);

            var response = _http.PostAsync("https://maple.software/backend/auth", content)
                .GetAwaiter().GetResult();

            string responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var json = JObject.Parse(responseString);
            var code = (int)json["code"];

            if (code != 0)
            {
                var responseCode = new DelimitedPacketBuffer();
                responseCode.AddByte((byte)(code == 5 ? 0x02 : 0x03));

                var badResponsePacket = new DelimitedPacketBuffer();
                badResponsePacket.AddByte(0xB1);
                badResponsePacket.Add(responseCode.GetEncryptedBuffer(this));
                badResponsePacket.WriteToStream(_stream);
                return false;
            }

            var dllData = _fileProvider.Get(cheatId, releaseStream);

            var clearPayload = new DelimitedPacketBuffer();
            clearPayload.AddByte((byte)code);
            clearPayload.Add(Crypto.AesEncrypt(dllData.ToArray(), Key, Iv));

            var dllStreamPacket = new DelimitedPacketBuffer();
            dllStreamPacket.AddByte(0xB1);
            dllStreamPacket.Add(clearPayload.GetEncryptedBuffer(this));
            dllStreamPacket.WriteToStream(_stream);
            return true;
        }

        internal void HandleHeartbeat(byte[] decrypted)
        {
            var values = new Dictionary<string, string>
            {
                { "t", "1" },
                { "e", "1" },
                { "s", Encoding.ASCII.GetString(decrypted) },
            };

            var content = new FormUrlEncodedContent(values);
            var response = _http.PostAsync("https://maple.software/backend/auth", content)
                .GetAwaiter().GetResult();

            string responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var json = JObject.Parse(responseString);
            var code = (int)json["code"];

            var result = (byte)(code switch
            {
                0 => 0, // success
                5 => 2, // invalid session
                _ => 3, // internal error
            });

            var packet = new DelimitedPacketBuffer();
            packet.AddByte(0xC2);

            long unixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            unixTimeMilliseconds /= 2;
            unixTimeMilliseconds ^= 0xDA;

            var responseData = new DelimitedPacketBuffer();
            responseData.AddByte(result);
            responseData.Add(unixTimeMilliseconds.ToString());

            packet.Add(responseData.GetEncryptedBuffer(this));
            packet.WriteToStream(_stream);
        }

        public void Dispose()
        {
            _logger?.Info("Client disconnected");

            _client?.Dispose();
        }

        private DelimitedPacketBuffer GetFatalErrorPacket(string message)
        {
            var data = Crypto.RsaEncrypt(Encoding.ASCII.GetBytes(message), out int sigLen);

            var packet = new DelimitedPacketBuffer();
            packet.AddByte(0xE0);
            packet.Add(sigLen.ToString());
            packet.Add(data);

            return packet;
        }

        private static byte[][] Split(byte[] input)
        {
            var curr = new List<byte>();
            var stuff = new List<byte[]>();
            for (var i = 0; i < input.Length - Constants.Delimiter.Length; i++)
            {
                if (!memcmp(input, i, Constants.Delimiter))
                {
                    curr.Add(input[i]);
                }
                else
                {
                    stuff.Add(curr.ToArray());
                    curr.Clear();
                    i += Constants.Delimiter.Length - 1;
                }
            }
            stuff.Add(curr.ToArray());
            return stuff.ToArray();
        }

        /// <summary>
        /// <c>input.TakeWhile(b => b &lt; 0x20).ToArray()</c>
        /// </summary>
        private static byte[] fixup(byte[] input)
        {
            // PERF: return Span<byte> instead?
            var output = new List<byte>();

            foreach (var c in input)
                if (c < 0x20)
                    break;
                else
                    output.Add(c);

            return output.ToArray();
        }

        private static bool memcmp(byte[] src, int off, byte[] cmp)
        {
            for (var i = 0; i < cmp.Length; i++)
            {
                if (src[i + off] != cmp[i])
                    return false;
            }
            return true;
        }
    }
}
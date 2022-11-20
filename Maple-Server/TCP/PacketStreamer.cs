namespace Maple_Server.TCP;

public class PacketStreamer
{
    private const int PacketHeaderSize = 8;
    private const uint PacketHeaderSignature = 0xdeadbeef;
    
    private readonly Action<List<byte>> _receiveCallback;
    
    private readonly List<byte> _receiveStreamData;
    private bool _isReceiving;
    private int _receiveStreamLength;
    private int _receiveStreamRemainingLength;

    public PacketStreamer(Action<List<byte>> receiveCallback)
    {
        _receiveCallback = receiveCallback;
        _receiveStreamData = new();
    }

    public void Receive(byte[] buffer, int readCount)
    {
        // all our packets should always come in the following format: {header_signature}{packet_length}{packet}
        if (!_isReceiving && BitConverter.ToUInt32(buffer, 0) == PacketHeaderSignature)
        {
            _isReceiving = true;
            _receiveStreamLength = BitConverter.ToInt32(buffer, sizeof(uint)) + PacketHeaderSize;
            _receiveStreamRemainingLength = _receiveStreamLength;
        }

        _receiveStreamRemainingLength -= readCount;
        _receiveStreamData.AddRange(buffer.Take(readCount));

        if (_receiveStreamRemainingLength == 0)
        {
            // getting rid of the packet header
            _receiveStreamData.RemoveRange(0, PacketHeaderSize);

            // calling receive callback
            _receiveCallback(_receiveStreamData);

            // cleanup
            _receiveStreamLength = 0;
            _receiveStreamRemainingLength = 0;
            _isReceiving = false;
            _receiveStreamData.Clear();
        }
    }

    public void Send(byte[] buffer, Stream stream)
    {
        List<byte> packet = new List<byte>(buffer);
        
        // inserting packet header
        packet.InsertRange(0, BitConverter.GetBytes(buffer.Length));
        packet.InsertRange(0, BitConverter.GetBytes(PacketHeaderSignature));

        int remainingBytes = packet.Count;
        int offset = 0;
        while (remainingBytes > 0)
        {
            int bytesToSend = Math.Min(4096, remainingBytes);
            
            stream.Write(packet.ToArray(), offset, bytesToSend);

            offset += bytesToSend;
            remainingBytes -= bytesToSend;
        }
    }
}
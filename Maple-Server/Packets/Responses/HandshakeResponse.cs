namespace Maple_Server.Packets.Responses;

public class HandshakeResponse
{
    public byte[] Key { get; set; }
    public byte[] IV { get; set; }
}
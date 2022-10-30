namespace Maple_Server.Packets;

public enum PacketType : byte
{
    Handshake = 0xA0,
    Login = 0xB0,
    LoaderStream = 0xC0,
    ImageStream = 0xD0,
    Heartbeat = 0xE0
}
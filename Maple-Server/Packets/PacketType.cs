namespace Maple_Server.Packets;

public enum PacketType : byte
{
    Handshake = 0xA0,
    Login = 0xB0,
    LoaderStream = 0xC0,
    ImageStreamStageOne = 0xD0,
    ImageStreamStageTwo = 0xD1,
    Heartbeat = 0xE0,
    Ping = 0xF0
}
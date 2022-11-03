namespace Maple_Server.Packets;

public enum PacketType : byte
{
    Handshake = 0xA0,
    Login = 0xB0,
    LoaderStream = 0xC0,
    ImageStream_StageOne = 0xD0,
    ImageStream_StageTwo = 0xD1,
    Heartbeat = 0xE0
}
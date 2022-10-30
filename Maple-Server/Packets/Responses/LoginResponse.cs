namespace Maple_Server.Packets.Responses;

public class LoginResponse
{
    public LoginResult Result { get; set; }
    public string SessionToken { get; set; }
    public string DiscordID { get; set; }
    public string DiscordAvatarHash { get; set; }
    public List<Game> Games { get; set; }
    public List<Cheat> Cheats { get; set; }
}

public enum LoginResult : uint
{
    Success = 0,
    IncorrectCredentials = 1,
    VersionMismatch = 2,
    HWIDMismatch = 3,
    Banned = 4,
    UnknownError = 5
}

public enum CheatStatus : uint
{
    Undetected = 0,
    Outdated = 1,
    Detected = 2,
    Unknown = 3
}

public class Game
{
    public uint ID { get; set; }
    public string Name { get; set; }
}

public class Cheat
{
    public uint ID { get; set; }
    public uint GameID { get; set; }
    public string Name { get; set; }
    public List<string> ReleaseStreams { get; set; }
    public uint StartingPrice { get; set; }
    public CheatStatus Status { get; set; }
    public string ExpiresOn { get; set; }
}
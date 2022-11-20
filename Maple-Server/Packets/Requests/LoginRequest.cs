namespace Maple_Server.Packets.Requests;

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string HWID { get; set; }
    public string LoaderVersion { get; set; }
}
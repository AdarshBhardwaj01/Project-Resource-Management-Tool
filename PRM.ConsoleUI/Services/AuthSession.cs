namespace PRM.ConsoleUI.Services;

public class AuthSession
{
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool ForcePasswordChange { get; set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public void Clear()
    {
        Token = string.Empty;
        UserId = 0;
        FullName = string.Empty;
        Username = string.Empty;
        Role = string.Empty;
        ForcePasswordChange = false;
    }
}

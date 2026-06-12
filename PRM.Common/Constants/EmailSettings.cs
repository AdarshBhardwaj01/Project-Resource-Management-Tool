namespace PRM.Common.Constants;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public string FromName { get; set; } = "PRM Tool";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(Host);
}

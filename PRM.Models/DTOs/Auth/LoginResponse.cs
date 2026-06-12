using PRM.Models.Enums;

namespace PRM.Models.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public ApplicationRole Role { get; set; }

    public bool ForcePasswordChange { get; set; }
}

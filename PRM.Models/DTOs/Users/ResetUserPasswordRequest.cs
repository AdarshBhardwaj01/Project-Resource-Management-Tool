namespace PRM.Models.DTOs.Users;

public class ResetUserPasswordRequest
{
    public string NewTemporaryPassword { get; set; } = string.Empty;
}

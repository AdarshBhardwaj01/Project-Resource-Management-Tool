namespace PRM.Models.DTOs.Users;

public class UserListResponse
{
    public List<UserListItemDto> Users { get; set; } = new();

    public int Total { get; set; }

    public int ActiveCount { get; set; }

    public int InactiveCount { get; set; }
}

namespace PRM.Models.Entities;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Designation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool ForcePasswordChange { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Resource? Resource { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
}

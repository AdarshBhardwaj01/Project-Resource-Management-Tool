using PRM.Models.Entities;

namespace PRM.DataAccess.Helpers;

public static class RoleQueryExtensions
{
    public static bool HasRoleName(this User user, string roleName)
    {
        return user.UserRoles.Any(userRole =>
            userRole.Role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }
}

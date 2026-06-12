using PRM.Common.Constants;
using PRM.Models.Entities;
using PRM.Models.Enums;

namespace PRM.Business.Helpers;

public static class UserRoleHelper
{
    private static readonly string[] RolePriority = [RoleNames.Admin, RoleNames.Manager, RoleNames.Employee];

    public static string? GetPrimaryRoleName(User user)
    {
        var roleNames = user.UserRoles
            .Select(userRole => userRole.Role?.RoleName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return RolePriority.FirstOrDefault(roleNames.Contains)
            ?? user.UserRoles.FirstOrDefault()?.Role?.RoleName;
    }

    public static ApplicationRole? GetPrimaryApplicationRole(User user)
    {
        var roleName = GetPrimaryRoleName(user);
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }
        return Enum.TryParse<ApplicationRole>(roleName, out var role) ? role : null;
    }

    public static bool HasRole(User user, ApplicationRole role)
    {
        return user.UserRoles.Any(userRole =>
            userRole.Role.RoleName.Equals(role.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasRoleName(User user, string roleName)
    {
        return user.UserRoles.Any(userRole =>
            userRole.Role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }
}

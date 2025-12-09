using AuthService.Constants;

namespace AuthService.Authorization;

public static class PermissionMap
{
    public static readonly Dictionary<string, string[]> AccessLevels = new()
    {
        { RoleAccessLevel.Viewer, new[] { Permission.View } },
        { RoleAccessLevel.Reader, new[] { Permission.View, Permission.Read } },
        {
            RoleAccessLevel.Contributor,
            new[] { Permission.View, Permission.Read, Permission.Write }
        },
        {
            RoleAccessLevel.Moderator,
            new[]
            {
                Permission.View,
                Permission.Read,
                Permission.Write,
                Permission.Edit,
                Permission.Delete,
            }
        },
        {
            RoleAccessLevel.Administrator,
            new[]
            {
                Permission.View,
                Permission.Read,
                Permission.Write,
                Permission.Edit,
                Permission.Delete,
                Permission.Special,
            }
        },
    };

    public static IEnumerable<string> ResolvePermissions(string tool, string access)
    {
        foreach (var p in AccessLevels[access])
            yield return $"role.{tool.ToLower()}.{p}";
    }
}

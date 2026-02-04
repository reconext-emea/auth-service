namespace AuthService.Constants;

public static class RoleAccessLevelPermissions
{
    private static readonly Dictionary<string, IReadOnlyList<string>> Map = new()
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

    public static IReadOnlyList<string> From(string roleAccessLevel)
    {
        return Map.TryGetValue(roleAccessLevel, out var permissions) ? permissions : [];
    }
}

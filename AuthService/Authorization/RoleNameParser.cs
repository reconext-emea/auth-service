namespace AuthService.Authorization;

public static class RoleNameParser
{
    public static (string Tool, string Access) Parse(string role)
    {
        var parts = role.Split('.');
        return (parts[0], parts[1]);
    }
}

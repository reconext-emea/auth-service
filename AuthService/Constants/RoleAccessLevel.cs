namespace AuthService.Constants;

public static class RoleAccessLevel
{
    public const string Viewer = "Viewer";
    public const string Reader = "Reader";
    public const string Contributor = "Contributor";
    public const string Moderator = "Moderator";
    public const string Administrator = "Administrator";

    public static readonly string[] Options =
    [
        Viewer,
        Reader,
        Contributor,
        Moderator,
        Administrator,
    ];

    public static bool IsValid(string value) => Options.Contains(value);
}

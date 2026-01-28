namespace AuthService.Constants;

public static class PreferredColorTheme
{
    public const string Light = "light";
    public const string Dark = "dark";

    public static readonly string[] Options = [Light, Dark];

    public static bool IsValid(string value) => Options.Contains(value);
}

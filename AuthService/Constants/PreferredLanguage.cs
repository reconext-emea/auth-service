namespace AuthService.Constants;

public static class PreferredLanguage
{
    public const string English = "en";
    public const string Polish = "pl";
    public const string Ukrainian = "ua";
    public const string Czech = "cs";

    public static readonly string[] Options = [English, Polish, Ukrainian, Czech];

    public static bool IsValid(string value) => Options.Contains(value);
}

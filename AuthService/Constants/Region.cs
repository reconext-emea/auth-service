namespace AuthService.Constants;

public static class Region
{
    public const string Amer = "amer";
    public const string Emea = "emea";

    public static readonly string[] Options = [Amer, Emea];

    public static bool IsValid(string value) => Options.Contains(value);
}

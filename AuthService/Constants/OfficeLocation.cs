namespace AuthService.Constants;

public static class OfficeLocation
{
    public const string Bydgoszcz = "Bydgoszcz Site (PL)";
    public const string Havant = "Havant Site (UK)";
    public const string Prague = "Prague Site (CZ)";
    public const string Tallinn = "Tallinn Site (EE)";
    public const string Zoetermeer = "Zoetermeer Site (NL)";

    public static readonly string[] Options = [Bydgoszcz, Havant, Prague, Tallinn, Zoetermeer];

    public static bool IsValid(string value) => Options.Contains(value);
}

namespace AuthService.Constants;

public static class ConfidentialityClass
{
    public const string Class1 = "Class 1";
    public const string Class2 = "Class 2";
    public const string Class3 = "Class 3";

    public static readonly string[] Options = [Class1, Class2, Class3];

    public static bool IsValid(string value) => Options.Contains(value);
}

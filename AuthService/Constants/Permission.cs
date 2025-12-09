namespace AuthService.Constants;

public static class Permission
{
    public const string View = "view";
    public const string Read = "read";
    public const string Write = "write";
    public const string Edit = "edit";
    public const string Delete = "delete";
    public const string Special = "special";

    public static readonly string[] Options = [View, Read, Write, Edit, Delete, Special];

    public static bool IsValid(string value) => Options.Contains(value);
}

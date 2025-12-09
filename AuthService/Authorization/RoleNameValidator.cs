using System.Text.RegularExpressions;

namespace AuthService.Authorization;

public static partial class RoleNameValidator
{
    public static bool IsValid(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;
        return RoleRegex().IsMatch(role);
    }

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]+\.([A-Za-z]+)$")]
    private static partial Regex RoleRegex();
}

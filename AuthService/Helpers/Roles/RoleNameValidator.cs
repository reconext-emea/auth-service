using System.Text.RegularExpressions;
using AuthService.Constants;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Roles;

namespace AuthService.Helpers.Roles;

public static partial class RoleNameValidator
{
    public static bool IsPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return PascalCaseRegex().IsMatch(value);
    }

    [GeneratedRegex("^[A-Z][a-z]+(?:[A-Z][a-z]+)*$")]
    private static partial Regex PascalCaseRegex();

    public static bool IsValid(CreateRoleDto role, out string name, out ErrorResponseDto error)
    {
        name = $"{role.Tool}.{role.Access}";

        if (!IsPascalCase(role.Tool))
        {
            error = new ErrorResponseDto
            {
                Error = $"Tool '{role.Tool}' must be PascalCase with no spaces.",
            };
            return false;
        }

        if (role.Tool.Contains('.'))
        {
            error = new ErrorResponseDto
            {
                Error = $"Tool '{role.Tool}' cannot contain '.' character.",
            };
            return false;
        }

        if (!RoleAccessLevel.IsValid(role.Access))
        {
            error = new ErrorResponseDto { Error = $"Unknown access level: {role.Access}." };
            return false;
        }

        error = null!;
        return true;
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Users;

namespace AuthService.Services.Identity;

public static partial class PermissionClaimService
{
    [GeneratedRegex("^[a-z]+(-[a-z]+)*$", RegexOptions.Compiled)]
    private static partial Regex PermissionClaimRegex();

    private static string PermissionClaimRegexError(string property) =>
        $"{property} must be non-empty and contain only lowercase letters (a-z) and single hyphens (-) between words.";

    private static bool ValidateAddClaimToUserDto(
        AddClaimToUserDto dto,
        out ErrorResponseDto? error
    )
    {
        error = null;

        if (!PermissionClaimRegex().IsMatch(dto.Tool))
        {
            error = new ErrorResponseDto(PermissionClaimRegexError("Tool"), dto.Tool);
            return false;
        }

        if (!PermissionClaimRegex().IsMatch(dto.Privilege))
        {
            error = new ErrorResponseDto(PermissionClaimRegexError("Privilege"), dto.Privilege);
            return false;
        }

        return true;
    }

    public static bool TryCreateUserClaim(
        AddClaimToUserDto dto,
        [NotNullWhen(true)] out Claim? claim,
        out ErrorResponseDto? error
    )
    {
        claim = null;

        if (!ValidateAddClaimToUserDto(dto, out error))
            return false;

        claim = new Claim("permission", $"user.{dto.Tool}.{dto.Privilege}");

        return true;
    }

    private static Claim CreateRoleClaim(string kebabCaseTool, string permission)
    {
        return new Claim("permission", $"role.{kebabCaseTool}.{permission}");
    }

    public static Claim[] CreateRoleClaims(string kebabCaseTool, IReadOnlyList<string> permissions)
    {
        if (permissions == null || permissions.Count == 0)
            return [];

        var claims = new Claim[permissions.Count];

        for (int i = 0; i < permissions.Count; i++)
        {
            claims[i] = CreateRoleClaim(kebabCaseTool, permissions[i]);
        }

        return claims;
    }
}

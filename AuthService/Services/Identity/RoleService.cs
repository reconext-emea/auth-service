using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AuthService.Constants;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Roles;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;

namespace AuthService.Services.Identity;

public partial class RoleService
{
    [GeneratedRegex("^[A-Z][a-z]*(?:[A-Z][a-z]*)*$")]
    private static partial Regex PascalCaseLettersOnlyRegex();

    public const string PascalCaseLettersOnlyRegexError =
        "Tool must be non-empty, use PascalCase, and contain only letters (e.g., 'AzureBlobStorage').";

    private static string AccessLevelError =>
        $"Access level must be non-empty and be one of the following values: {string.Join(", ", RoleAccessLevel.Options)}.";

    private static bool ValidateCreateRoleDto(CreateRoleDto dto, out ErrorResponseDto? error)
    {
        error = null;

        if (!PascalCaseLettersOnlyRegex().IsMatch(dto.Tool))
        {
            error = new ErrorResponseDto(PascalCaseLettersOnlyRegexError, dto.Tool);
            return false;
        }

        if (!RoleAccessLevel.IsValid(dto.Access))
        {
            error = new ErrorResponseDto(AccessLevelError, dto.Access);
            return false;
        }

        return true;
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex PascalCaseBoundaryRegex();

    public static string ToKebabCase(string pascalCase)
    {
        return PascalCaseBoundaryRegex().Replace(pascalCase, "-$1").ToLowerInvariant();
    }

    public static bool TryCreateRole(
        CreateRoleDto dto,
        [NotNullWhen(true)] out string? roleName,
        [NotNullWhen(true)] out IdentityRole? role,
        [NotNullWhen(true)] out Claim[]? roleClaims,
        [NotNullWhen(true)] out string? descriptorClientId,
        [NotNullWhen(true)] out OpenIddictApplicationDescriptor? descriptor,
        out ErrorResponseDto? error
    )
    {
        roleName = null;
        role = null;
        descriptorClientId = null;
        roleClaims = null;
        descriptor = null;

        if (!ValidateCreateRoleDto(dto, out error))
            return false;

        roleName = $"{dto.Tool}.{dto.Access}";

        role = new IdentityRole(roleName);

        descriptorClientId = ToKebabCase(dto.Tool);

        roleClaims = PermissionClaimService.CreateRoleClaims(
            descriptorClientId,
            RoleAccessLevelPermissions.From(dto.Access)
        );

        descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = descriptorClientId,
            DisplayName = dto.Tool,
            ClientType = OpenIddictConstants.ClientTypes.Public,
        };

        return true;
    }

    public static bool TryDestructureRoleName(
        string roleName,
        [NotNullWhen(true)] out string? tool,
        [NotNullWhen(true)] out string? access,
        out ErrorResponseDto? error
    )
    {
        int dotIndex = roleName.IndexOf('.');
        tool = roleName[..dotIndex];
        access = roleName[(dotIndex + 1)..];

        if (!ValidateCreateRoleDto(new CreateRoleDto(tool, access), out error))
            return false;

        return true;
    }
}

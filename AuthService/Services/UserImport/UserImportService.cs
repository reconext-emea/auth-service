using System.Security.Claims;
using AuthService.Authorization;
using AuthService.Data;
using AuthService.Helpers.Roles;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Roles;
using AuthService.Models.Dto.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.Services.UserImport;

public sealed class UserImportService(
    AuthServiceDbContext dbContext,
    UserManager<AuthServiceUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<UserImportService> logger
) : IUserImportService
{
    private readonly AuthServiceDbContext _dbContext = dbContext;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly ILogger<UserImportService> _logger = logger;

    public async Task ImportSingleUserAsync(
        ImportUserDto import,
        ImportUsersResponseDto response,
        CancellationToken cancellationToken = default
    )
    {
        await using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            AuthServiceUser? user = await _userManager.FindByNameAsync(import.Username);

            if (user is not null)
            {
                response.AddSkipped();
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            user = AuthServiceUser.CreateFromImport(import.Username);

            IdentityResult createUserResult = await _userManager.CreateAsync(user);
            if (createUserResult.Succeeded == false)
            {
                response.AddError(import.Username, createUserResult.Errors);
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            foreach (CreateRoleDto role in import.Roles)
            {
                if (
                    !RoleNameValidator.IsValid(
                        role,
                        out string roleName,
                        out ErrorResponseDto error
                    )
                )
                {
                    response.AddError(import.Username, error);
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                IdentityRole? identityRole = await _roleManager.FindByNameAsync(roleName);

                if (identityRole is null)
                {
                    identityRole = new IdentityRole(roleName);
                    IdentityResult createRoleResult = await _roleManager.CreateAsync(identityRole);

                    if (createRoleResult.Succeeded == false)
                    {
                        response.AddError(import.Username, createRoleResult.Errors);
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    IEnumerable<string> permissions = PermissionMap.ResolvePermissions(
                        role.Tool,
                        role.Access
                    );

                    foreach (string permission in permissions)
                    {
                        await _roleManager.AddClaimAsync(
                            identityRole,
                            new Claim("permission", permission)
                        );
                    }
                }

                if (!await _userManager.IsInRoleAsync(user, roleName))
                {
                    IdentityResult assignResult = await _userManager.AddToRoleAsync(user, roleName);

                    if (!assignResult.Succeeded)
                    {
                        response.AddError(import.Username, assignResult.Errors);
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
            response.AddCreated();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed importing user {Username}", import.Username);
            response.AddException(import.Username, ex);
        }
    }
}

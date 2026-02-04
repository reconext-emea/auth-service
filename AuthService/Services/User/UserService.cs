using AuthService.Clients.LdapClient;
using AuthService.Constants;
using AuthService.Data;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Roles;
using AuthService.Models.Dto.Users;
using AuthService.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OpenIddict.Abstractions;

namespace AuthService.Services.User;

public sealed class UserService(
    AuthServiceDbContext dbContext,
    UserManager<AuthServiceUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager appManager,
    ILdapClient ldapClient,
    OfficeLocationToRegionAdapter adapter,
    ILogger<UserService> logger
) : IUserService
{
    private readonly AuthServiceDbContext _dbContext = dbContext;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly IOpenIddictApplicationManager _appManager = appManager;
    private readonly ILdapClient _ldapClient = ldapClient;
    private readonly OfficeLocationToRegionAdapter _adapter = adapter;
    private readonly ILogger<UserService> _logger = logger;

    /// <summary>
    /// Finds a user by identifier.
    /// </summary>
    /// <param name="userIdentifier">
    /// User identifier which can be one of the following:
    /// <list type="bullet">
    /// <item><description>User ID (GUID)</description></item>
    /// <item><description>Username (e.g. <c>maciej.zablocki</c>)</description></item>
    /// <item><description>Email address (e.g. <c>maciej.zablocki@reconext.com</c>)</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// The matching <see cref="AuthServiceUser"/> if found; otherwise <c>null</c>.
    /// </returns>
    public async Task<AuthServiceUser?> FindUserByIdentifierAsync(string userIdentifier)
    {
        if (Guid.TryParse(userIdentifier, out _))
        {
            var byId = await _userManager.FindByIdAsync(userIdentifier);
            if (byId != null)
                return byId;
        }

        var byName = await _userManager.FindByNameAsync(userIdentifier);
        if (byName != null)
            return byName;

        return await _userManager.FindByEmailAsync(userIdentifier);
    }

    public async Task<AuthServiceUser?> FindUserByIdentifierAsync(
        IQueryable<AuthServiceUser> query,
        string userIdentifier
    )
    {
        var normalizer = _userManager.KeyNormalizer;

        var normalized = normalizer.NormalizeName(userIdentifier);
        var normalizedEmail = normalizer.NormalizeEmail(userIdentifier);

        if (Guid.TryParse(userIdentifier, out _))
        {
            return await query.FirstOrDefaultAsync(u =>
                u.Id == userIdentifier
                || u.NormalizedUserName == normalized
                || u.NormalizedEmail == normalizedEmail
            );
        }

        return await query.FirstOrDefaultAsync(u =>
            u.NormalizedUserName == normalized || u.NormalizedEmail == normalizedEmail
        );
    }

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

            LdapUser? ldapUser = await _ldapClient.FindUserByUsernameAsync(
                import.Username,
                "reconext.com",
                cancellationToken
            );

            if (ldapUser is null)
            {
                response.AddError(
                    import.Username,
                    new ErrorResponseDto("User not found in LDAP or not allowed.")
                );
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            user = AuthServiceUser.CreateFromLdap(ldapUser, _adapter, import.CustomProperties);

            IdentityResult createUserResult = await _userManager.CreateAsync(user);
            if (createUserResult.Succeeded == false)
            {
                response.AddError(import.Username, createUserResult.Errors);
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            foreach (CreateRoleDto dto in import.Roles)
            {
                if (
                    !RoleService.TryCreateRole(
                        dto,
                        out var roleName,
                        out var role,
                        out var roleClaims,
                        out var descriptorClientId,
                        out var descriptor,
                        out var error
                    )
                )
                {
                    response.AddError(import.Username, error!);
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                IdentityRole? identityRole = await _roleManager.FindByNameAsync(roleName);

                if (identityRole is null)
                {
                    var roleResult = await _roleManager.CreateAsync(role);

                    if (!roleResult.Succeeded)
                    {
                        response.AddError(
                            import.Username,
                            new ErrorResponseDto
                            {
                                Error = "Role creation failed.",
                                Details = string.Join(
                                    ", ",
                                    roleResult.Errors.Select(e => e.Description)
                                ),
                            }
                        );
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    identityRole = await _roleManager.FindByNameAsync(roleName);

                    if (identityRole is null)
                    {
                        response.AddError(
                            import.Username,
                            new ErrorResponseDto { Error = "Failed to create role." }
                        );
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    foreach (var claim in roleClaims)
                    {
                        var claimResult = await _roleManager.AddClaimAsync(identityRole, claim);
                        if (!claimResult.Succeeded)
                        {
                            response.AddError(
                                import.Username,
                                new ErrorResponseDto
                                {
                                    Error = "Adding role claims failed.",
                                    Details = string.Join(
                                        ", ",
                                        claimResult.Errors.Select(e => e.Description)
                                    ),
                                }
                            );
                            await transaction.RollbackAsync(cancellationToken);
                            return;
                        }
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

                // 1) Find (by clientId)
                var app = await _appManager.FindByClientIdAsync(
                    descriptor.ClientId!,
                    cancellationToken
                );

                // 2) Create only if missing
                if (app is null)
                {
                    await _appManager.CreateAsync(descriptor, cancellationToken);

                    // 3) Re-load after create
                    app = await _appManager.FindByClientIdAsync(
                        descriptor.ClientId!,
                        cancellationToken
                    );

                    if (app is null)
                    {
                        response.AddError(
                            import.Username,
                            new ErrorResponseDto
                            {
                                Error =
                                    $"Failed to create application (clientId='{descriptor.ClientId}').",
                            }
                        );
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }
                }

                // 4) Get app id
                var appId = await _appManager.GetIdAsync(app, cancellationToken);
                if (string.IsNullOrWhiteSpace(appId))
                {
                    response.AddError(
                        import.Username,
                        new ErrorResponseDto
                        {
                            Error =
                                $"Application id not resolved (clientId='{descriptor.ClientId}').",
                        }
                    );
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                // 5) Create link only if missing
                var alreadyAssigned = await _dbContext
                    .Set<AuthServiceUserApplication>()
                    .AnyAsync(
                        x => x.UserId == user.Id && x.ApplicationId == appId,
                        cancellationToken
                    );

                if (!alreadyAssigned)
                {
                    _dbContext
                        .Set<AuthServiceUserApplication>()
                        .Add(
                            new AuthServiceUserApplication
                            {
                                UserId = user.Id,
                                ApplicationId = appId,
                            }
                        );

                    await _dbContext.SaveChangesAsync(cancellationToken);
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

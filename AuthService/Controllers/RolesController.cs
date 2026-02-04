using AuthService.Constants;
using AuthService.Data;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Roles;
using AuthService.Services.Identity;
using AuthService.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthService.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "roles")]
[Route("api/roles")]
public class RolesController(
    RoleManager<IdentityRole> roleManager,
    UserManager<AuthServiceUser> userManager,
    IOpenIddictApplicationManager appManager,
    IUserService userService,
    AuthServiceDbContext dbContext
) : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
    private readonly IOpenIddictApplicationManager _appManager = appManager;

    private readonly IUserService _userService = userService;
    private readonly AuthServiceDbContext _dbContext = dbContext;

    // --------------------------------------------------------------
    // Access Levels
    // --------------------------------------------------------------
    [HttpGet("access-levels")]
    public ActionResult<GetAccessLevelsResponseDto> GetAccessLevels()
    {
        return Ok(new GetAccessLevelsResponseDto([.. RoleAccessLevel.Options]));
    }

    // --------------------------------------------------------------
    // Permission Types
    // --------------------------------------------------------------
    [HttpGet("permissions")]
    public ActionResult<GetPermissionTypesResponseDto> GetPermissionTypes()
    {
        return Ok(new GetPermissionTypesResponseDto([.. Permission.Options]));
    }

    // --------------------------------------------------------------
    // Get All Roles
    // --------------------------------------------------------------
    [HttpGet]
    public ActionResult<GetRolesResponseDto> GetRoles()
    {
        List<string> roles = [.. _roleManager.Roles.Select(r => r.Name!)];

        return Ok(new GetRolesResponseDto(roles));
    }

    // --------------------------------------------------------------
    // Get Roles of a Specific User
    // --------------------------------------------------------------
    [HttpGet("of-user/{userIdentifier}")]
    public async Task<ActionResult<GetRolesOfUserResponseDto>> GetRolesOfUser(string userIdentifier)
    {
        // AuthServiceUser? user = await FindUser(userIdentifier);

        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(userIdentifier);

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        IList<string> roles = await _userManager.GetRolesAsync(user);

        return Ok(new GetRolesOfUserResponseDto([.. roles]));
    }

    // --------------------------------------------------------------
    // Create Role
    // --------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<CreateRoleResponseDto>> CreateRole([FromBody] CreateRoleDto dto)
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
            return BadRequest(error);
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // --- 1) Check role exists
            if (await _roleManager.RoleExistsAsync(roleName))
                return Conflict(new ErrorResponseDto { Error = "Role already exists." });

            // --- 2) Check application exists (avoid duplicate client id)
            if (await _appManager.FindByClientIdAsync(descriptorClientId) is not null)
                return Conflict(new ErrorResponseDto { Error = "Application already exists." });

            // --- 3) Create role
            var roleResult = await _roleManager.CreateAsync(role);

            if (!roleResult.Succeeded)
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ErrorResponseDto
                    {
                        Error = "Role creation failed.",
                        Details = string.Join(", ", roleResult.Errors.Select(e => e.Description)),
                    }
                );

            // --- 4) Add permission claims
            foreach (var claim in roleClaims)
            {
                var claimResult = await _roleManager.AddClaimAsync(role, claim);
                if (!claimResult.Succeeded)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new ErrorResponseDto
                        {
                            Error = "Adding role claims failed.",
                            Details = string.Join(
                                ", ",
                                claimResult.Errors.Select(e => e.Description)
                            ),
                        }
                    );
                }
            }

            // --- 5) Create OpenIddict application
            await _appManager.CreateAsync(descriptor);

            // --- 6) Commit transaction
            await tx.CommitAsync();

            return Ok(new CreateRoleResponseDto());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto { Error = "Transaction failed.", Details = ex.Message }
            );
        }
    }

    // --------------------------------------------------------------
    // Delete Role
    // --------------------------------------------------------------
    [HttpDelete("{roleName}")]
    public async Task<ActionResult<DeleteRoleResponseDto>> DeleteRole(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
            return NotFound(new ErrorResponseDto { Error = $"Role '{roleName}' not found." });

        if (
            !RoleService.TryDestructureRoleName(
                roleName,
                out var tool,
                out var access,
                out var error
            )
        )
        {
            return BadRequest(error);
        }

        var clientId = RoleService.ToKebabCase(tool);

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 1) Delete OpenIddict application (if it exists)
            var app = await _appManager.FindByClientIdAsync(clientId);

            if (app != null)
            {
                await _appManager.DeleteAsync(app);
            }

            // 2) Delete role
            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ErrorResponseDto
                    {
                        Error = "Role deletion failed.",
                        Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                    }
                );

            await tx.CommitAsync();

            return Ok(new DeleteRoleResponseDto());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto { Error = "Transaction failed.", Details = ex.Message }
            );
        }
    }

    // --------------------------------------------------------------
    // Assign Role
    // --------------------------------------------------------------
    [HttpPost("assign")]
    public async Task<ActionResult<AssignRoleResponseDto>> AssignRole([FromBody] AssignRoleDto dto)
    {
        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(dto.UserIdentifier);
        // var user = await FindUser(dto.UserIdentifier);
        if (user == null)
            return NotFound(
                new ErrorResponseDto { Error = $"User '{dto.UserIdentifier}' not found." }
            );

        var role = await _roleManager.FindByNameAsync(dto.RoleName);
        if (role == null)
            return NotFound(new ErrorResponseDto { Error = $"Role '{dto.RoleName}' not found." });

        if (
            !RoleService.TryDestructureRoleName(
                dto.RoleName,
                out var tool,
                out var access,
                out var error
            )
        )
        {
            return BadRequest(error);
        }

        var clientId = RoleService.ToKebabCase(tool);

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 1) Check if user already has the role
            if (await _userManager.IsInRoleAsync(user, dto.RoleName))
                return Conflict(
                    new ErrorResponseDto { Error = $"User already has role '{dto.RoleName}'." }
                );

            // 2) Assign role
            var roleResult = await _userManager.AddToRoleAsync(user, dto.RoleName);
            if (!roleResult.Succeeded)
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ErrorResponseDto
                    {
                        Error = "Role assignment failed.",
                        Details = string.Join(", ", roleResult.Errors.Select(e => e.Description)),
                    }
                );

            // 3) Find application by client id (derived from role/tool)
            var app = await _appManager.FindByClientIdAsync(clientId);
            if (app == null)
                return NotFound(
                    new ErrorResponseDto
                    {
                        Error = $"Application (clientId='{clientId}') not found.",
                    }
                );

            var appId = await _appManager.GetIdAsync(app);

            // 4) Check join table already exists
            var alreadyAssigned = await _dbContext
                .Set<AuthServiceUserApplication>()
                .AnyAsync(x => x.UserId == user.Id && x.ApplicationId == appId);

            if (alreadyAssigned)
                return Conflict(
                    new ErrorResponseDto { Error = "User already has this application assigned." }
                );

            // 5) Insert join row
            _dbContext
                .Set<AuthServiceUserApplication>()
                .Add(new AuthServiceUserApplication { UserId = user.Id, ApplicationId = appId! });

            await _dbContext.SaveChangesAsync();

            await tx.CommitAsync();

            return Ok(new AssignRoleResponseDto());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto { Error = "Transaction failed.", Details = ex.Message }
            );
        }
    }

    // --------------------------------------------------------------
    // Unassign Role
    // --------------------------------------------------------------
    [HttpPost("unassign")]
    public async Task<ActionResult<UnassignRoleResponseDto>> UnassignRole(
        [FromBody] UnassignRoleDto dto
    )
    {
        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(dto.UserIdentifier);
        // var user = await FindUser(dto.UserIdentifier);
        if (user == null)
            return NotFound(
                new ErrorResponseDto { Error = $"User '{dto.UserIdentifier}' not found." }
            );

        // Find role by Id / Name
        var role = await _roleManager.FindByNameAsync(dto.RoleName);
        if (role == null)
            return NotFound(new ErrorResponseDto { Error = $"Role '{dto.RoleName}' not found." });

        if (
            !RoleService.TryDestructureRoleName(
                dto.RoleName,
                out var tool,
                out var access,
                out var error
            )
        )
        {
            return BadRequest(error);
        }

        var clientId = RoleService.ToKebabCase(tool);

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 1) Check user actually has the role
            if (!await _userManager.IsInRoleAsync(user, dto.RoleName))
                return Conflict(
                    new ErrorResponseDto { Error = $"User does not have role '{dto.RoleName}'." }
                );

            // 2) Remove role
            var roleResult = await _userManager.RemoveFromRoleAsync(user, dto.RoleName);
            if (!roleResult.Succeeded)
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ErrorResponseDto
                    {
                        Error = "Role unassignment failed.",
                        Details = string.Join(", ", roleResult.Errors.Select(e => e.Description)),
                    }
                );

            // 3) Find corresponding application
            var app = await _appManager.FindByClientIdAsync(clientId);
            if (app == null)
                return NotFound(
                    new ErrorResponseDto
                    {
                        Error = $"Application (clientId='{clientId}') not found.",
                    }
                );

            var appId = await _appManager.GetIdAsync(app);

            // 4) Remove link from join table
            var link = await _dbContext
                .Set<AuthServiceUserApplication>()
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.ApplicationId == appId);

            if (link == null)
                return Conflict(
                    new ErrorResponseDto { Error = "User does not have this application assigned." }
                );

            _dbContext.Set<AuthServiceUserApplication>().Remove(link);
            await _dbContext.SaveChangesAsync();

            await tx.CommitAsync();

            return Ok(new UnassignRoleResponseDto());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto { Error = "Transaction failed.", Details = ex.Message }
            );
        }
    }
}

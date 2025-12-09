using System.Security.Claims;
using AuthService.Authorization;
using AuthService.Constants;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "roles")]
[Route("api/roles")]
public class RolesController(
    RoleManager<IdentityRole> roleManager,
    UserManager<AuthServiceUser> userManager
) : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;

    // --------------------------------------------------------------
    // Access Levels
    // --------------------------------------------------------------
    [HttpGet("access-levels")]
    public ActionResult<GetAccessLevelsResponseDto> GetAccessLevels()
    {
        return Ok(new GetAccessLevelsResponseDto { AccessLevels = [.. RoleAccessLevel.Options] });
    }

    // --------------------------------------------------------------
    // Permission Types
    // --------------------------------------------------------------
    [HttpGet("permissions")]
    public ActionResult<GetPermissionTypesResponseDto> GetPermissionTypes()
    {
        return Ok(new GetPermissionTypesResponseDto { Permissions = [.. Permission.Options] });
    }

    // --------------------------------------------------------------
    // Get All Roles
    // --------------------------------------------------------------
    [HttpGet]
    public ActionResult<GetRolesResponseDto> GetRoles()
    {
        List<string> roles = [.. _roleManager.Roles.Select(r => r.Name!)];

        return Ok(new GetRolesResponseDto { Roles = roles });
    }

    // --------------------------------------------------------------
    // Get Roles of a Specific User
    // --------------------------------------------------------------
    [HttpGet("of-user/{userIdentifier}")]
    public async Task<ActionResult<GetRolesOfUserResponseDto>> GetRolesOfUser(string userIdentifier)
    {
        AuthServiceUser? user = await FindUser(userIdentifier);
        if (user == null)
            return NotFound(
                new RolesErrorResponseDto { Error = $"User '{userIdentifier}' not found." }
            );

        IList<string> roles = await _userManager.GetRolesAsync(user);

        return Ok(new GetRolesOfUserResponseDto { Roles = [.. roles] });
    }

    // --------------------------------------------------------------
    // Create Role
    // --------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<CreateRoleResponseDto>> CreateRole([FromBody] CreateRoleDto dto)
    {
        // Validate format: Tool.Access
        if (!RoleNameValidator.IsValid(dto.RoleName))
            return BadRequest(
                new RolesErrorResponseDto
                {
                    Error = "Invalid role format. Expected Tool.AccessLevel",
                }
            );

        // Parse segments
        var (tool, access) = RoleNameParser.Parse(dto.RoleName);

        // Validate access level
        if (!RoleAccessLevel.IsValid(access))
            return BadRequest(
                new RolesErrorResponseDto { Error = $"Unknown access level: {access}" }
            );

        // Check if role exists
        if (await _roleManager.RoleExistsAsync(dto.RoleName))
            return Conflict(new RolesErrorResponseDto { Error = "Role already exists" });

        // Create role
        var role = new IdentityRole(dto.RoleName);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new RolesErrorResponseDto
                {
                    Error = "Role creation failed.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        // Resolve permissions
        var permissions = PermissionMap.ResolvePermissions(tool, access);

        // Add claims
        foreach (var perm in permissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim("permission", perm));
        }

        return Ok(new CreateRoleResponseDto { Role = role.Name!, RoleClaims = [.. permissions] });
    }

    // --------------------------------------------------------------
    // Delete Role
    // --------------------------------------------------------------
    [HttpDelete("{roleIdentifier}")]
    public async Task<ActionResult<DeleteRoleResponseDto>> DeleteRole(string roleIdentifier)
    {
        // Find role by Id / Name
        IdentityRole? role = await FindRole(roleIdentifier);
        if (role == null)
            return NotFound(
                new RolesErrorResponseDto { Error = $"Role '{roleIdentifier}' not found." }
            );

        IdentityResult? result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new RolesErrorResponseDto
                {
                    Error = "Role deletion failed.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        return Ok(
            new DeleteRoleResponseDto { Role = role.Name!, Message = "Role deleted successfully." }
        );
    }

    // --------------------------------------------------------------
    // Assign Role
    // --------------------------------------------------------------
    [HttpPost("assign")]
    public async Task<ActionResult<AssignRoleResponseDto>> AssignRole([FromBody] AssignRoleDto dto)
    {
        // Find user by Id / UserName / Email
        AuthServiceUser? user = await FindUser(dto.UserIdentifier);
        if (user == null)
            return NotFound(
                new RolesErrorResponseDto { Error = $"User '{dto.UserIdentifier}' not found." }
            );

        // Find role by Id / Name
        IdentityRole? role = await FindRole(dto.RoleIdentifier);
        if (role == null)
            return NotFound(
                new RolesErrorResponseDto { Error = $"Role '{dto.RoleIdentifier}' not found." }
            );

        string roleName = role.Name!;

        // Check if user already has the role
        if (await _userManager.IsInRoleAsync(user, roleName))
            return Conflict(
                new RolesErrorResponseDto { Error = $"User already has role '{roleName}'." }
            );

        // Assign role
        IdentityResult? result = await _userManager.AddToRoleAsync(user, roleName);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new RolesErrorResponseDto
                {
                    Error = "Role assignment failed.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        return Ok(
            new AssignRoleResponseDto
            {
                User = user.UserName!,
                Role = roleName,
                Message = "Role assigned successfully.",
            }
        );
    }

    // --------------------------------------------------------------
    // Unassign Role
    // --------------------------------------------------------------
    [HttpPost("unassign")]
    public async Task<ActionResult<UnassignRoleResponseDto>> UnassignRole(
        [FromBody] UnassignRoleDto dto
    )
    {
        // Find user by Id / UserName / Email
        AuthServiceUser? user = await FindUser(dto.UserIdentifier);
        if (user == null)
            return NotFound(
                new RolesErrorResponseDto { Error = $"User '{dto.UserIdentifier}' not found." }
            );

        // Find role by Id / Name
        IdentityRole? role = await FindRole(dto.RoleIdentifier);
        if (role == null)
            return NotFound(
                new RolesErrorResponseDto { Error = $"Role '{dto.RoleIdentifier}' not found." }
            );

        string roleName = role.Name!;

        // Check if the user actually has this role
        if (!await _userManager.IsInRoleAsync(user, roleName))
            return Conflict(
                new RolesErrorResponseDto { Error = $"User does not have role '{roleName}'." }
            );

        // Remove role
        IdentityResult? result = await _userManager.RemoveFromRoleAsync(user, roleName);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new RolesErrorResponseDto
                {
                    Error = "Role unassignment failed.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        return Ok(
            new UnassignRoleResponseDto
            {
                User = user.UserName!,
                Role = roleName,
                Message = "Role unassigned successfully.",
            }
        );
    }

    // --------------------------------------------------------------
    // Helper Methods
    // --------------------------------------------------------------
    private async Task<AuthServiceUser?> FindUser(string input)
    {
        // Try by Id
        var user = await _userManager.FindByIdAsync(input);
        if (user != null)
            return user;

        // Try by UserName
        user = await _userManager.FindByNameAsync(input);
        if (user != null)
            return user;

        // Try by Email
        return await _userManager.FindByEmailAsync(input);
    }

    private async Task<IdentityRole?> FindRole(string input)
    {
        // Try by Id
        var role = await _roleManager.FindByIdAsync(input);
        if (role != null)
            return role;

        // Try by Name
        return await _roleManager.FindByNameAsync(input);
    }
}

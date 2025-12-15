using System.Security.Claims;
using AuthService.Constants;
using AuthService.Data;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "users")]
[Route("api/users")]
public class UsersController(
    UserManager<AuthServiceUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AuthServiceDbContext db
) : ControllerBase
{
    private readonly UserManager<AuthServiceUser> _userManager = userManager;

    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly AuthServiceDbContext _db = db;

    // --------------------------------------------------------------
    // Get All Users
    // --------------------------------------------------------------
    [HttpGet("many/{includeSettings:bool}")]
    public async Task<ActionResult<GetUsersResponseDto>> GetUsers(bool includeSettings)
    {
        IQueryable<AuthServiceUser> query = _userManager.Users;
        if (includeSettings)
            query = query.Include(u => u.AppSettings);

        List<AuthServiceUser> users = await query.ToListAsync();
        List<AuthServiceUserDto> passports = [.. users.Select(u => new AuthServiceUserDto(u))];

        return Ok(new GetUsersResponseDto { Users = passports });
    }

    // --------------------------------------------------------------
    // Get Specific User by Id / Username / Email
    // --------------------------------------------------------------
    [HttpGet("one/{userIdentifier}/{includeSettings:bool}")]
    public async Task<ActionResult<AuthServiceUserDto>> GetUser(
        string userIdentifier,
        bool includeSettings
    )
    {
        IQueryable<AuthServiceUser> query = _userManager.Users;
        if (includeSettings)
            query = query.Include(u => u.AppSettings);

        AuthServiceUser? user = await query.FirstOrDefaultAsync(u =>
            u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        );

        if (user == null)
            return NotFound(
                new UsersErrorResponseDto { Error = $"User '{userIdentifier}' not found." }
            );

        AuthServiceUserDto passport = new(user);

        return Ok(new GetUserResponseDto { User = passport });
    }

    // --------------------------------------------------------------
    // Update User Settings
    // --------------------------------------------------------------
    [HttpPut("one/{userIdentifier}/settings")]
    public async Task<ActionResult<UpdateUserSettingsResponseDto>> UpdateUserSettings(
        string userIdentifier,
        [FromBody] UpdateUserSettingsDto dto
    )
    {
        if (!PreferredLanguage.IsValid(dto.PreferredLanguageCode))
        {
            return BadRequest(
                new UsersErrorResponseDto
                {
                    Error = $"Invalid preferred language '{dto.PreferredLanguageCode}'. ",
                    Details = $"Allowed values: {string.Join(", ", PreferredLanguage.Options)}",
                }
            );
        }

        if (!ColorTheme.IsValid(dto.ColorThemeCode))
        {
            return BadRequest(
                new UsersErrorResponseDto
                {
                    Error = $"Invalid color theme '{dto.ColorThemeCode}'. ",
                    Details = $"Allowed values: {string.Join(", ", ColorTheme.Options)}",
                }
            );
        }

        AuthServiceUser? user = await _userManager
            .Users.Include(u => u.AppSettings)
            .FirstOrDefaultAsync(u =>
                u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
            );

        if (user == null)
            return NotFound(
                new UsersErrorResponseDto { Error = $"User '{userIdentifier}' not found." }
            );

        if (user.AppSettings == null)
        {
            var settings = new AuthServiceUserAppSettings
            {
                Id = user.Id,
                PreferredLanguageCode = dto.PreferredLanguageCode,
                ColorThemeCode = dto.ColorThemeCode,
                User = user,
            };
            _db.AspNetUsersAppSettings.Add(settings);
        }
        else
        {
            user.AppSettings.PreferredLanguageCode = dto.PreferredLanguageCode;
            user.AppSettings.ColorThemeCode = dto.ColorThemeCode;
        }

        await _db.SaveChangesAsync();

        return Ok(new UpdateUserSettingsResponseDto { Message = "Settings updated successfully." });
    }

    // --------------------------------------------------------------
    // Get User Claims
    // --------------------------------------------------------------
    [HttpGet("one/{userIdentifier}/claims")]
    public async Task<ActionResult<GetUserClaimsResponseDto>> GetUserClaims(string userIdentifier)
    {
        AuthServiceUser? user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        );

        if (user == null)
            return NotFound(
                new UsersErrorResponseDto { Error = $"User '{userIdentifier}' not found." }
            );

        var userClaims = await _userManager.GetClaimsAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        var roleClaims = new List<Claim>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                continue;

            var claims = await _roleManager.GetClaimsAsync(role);
            roleClaims.AddRange(claims);
        }

        return Ok(
            new GetUserClaimsResponseDto
            {
                UserClaims = [.. userClaims.Select(c => c.Value)],
                RoleClaims = [.. roleClaims.Select(c => c.Value)],
            }
        );
    }

    // --------------------------------------------------------------
    // Delete Claim From User
    // --------------------------------------------------------------
    [HttpDelete("one/{userIdentifier}/claims/${userClaimValue}")]
    public async Task<ActionResult<DeleteClaimFromUserResponseDto>> DeleteClaimFromUser(
        string userIdentifier,
        string userClaimValue
    )
    {
        AuthServiceUser? user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        );

        if (user == null)
            return NotFound(
                new UsersErrorResponseDto { Error = $"User '{userIdentifier}' not found." }
            );

        // Find the claim by value
        var claims = await _userManager.GetClaimsAsync(user);
        var claimToRemove = claims.FirstOrDefault(c =>
            c.Type == "permission" && c.Value == userClaimValue
        );

        if (claimToRemove == null)
            return NotFound(
                new UsersErrorResponseDto
                {
                    Error = $"Claim '{userClaimValue}' not found for user '{userIdentifier}'.",
                }
            );

        var result = await _userManager.RemoveClaimAsync(user, claimToRemove);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new RolesErrorResponseDto
                {
                    Error = "Failed to remove claim.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        return Ok(
            new DeleteClaimFromUserResponseDto { Message = "User claim removed successfully." }
        );
    }

    // --------------------------------------------------------------
    // Add Claim to User
    // --------------------------------------------------------------
    [HttpPost("one/{userIdentifier}/claims")]
    public async Task<ActionResult<AddClaimToUserDtoResponseDto>> AddClaimToUser(
        string userIdentifier,
        [FromBody] AddClaimToUserDto dto
    )
    {
        AuthServiceUser? user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        );

        if (user == null)
            return NotFound(
                new UsersErrorResponseDto { Error = $"User '{userIdentifier}' not found." }
            );

        var userClaimValue = $"user.{dto.Tool.ToLower()}.{dto.Privilege.ToLower()}";

        var existingClaims = await _userManager.GetClaimsAsync(user);
        if (existingClaims.Any(c => c.Type == "permission" && c.Value == userClaimValue))
        {
            return BadRequest(
                new UsersErrorResponseDto
                {
                    Error = "User already has this claim.",
                    Details = userClaimValue,
                }
            );
        }

        var claim = new Claim("permission", userClaimValue);

        var result = await _userManager.AddClaimAsync(user, claim);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new RolesErrorResponseDto
                {
                    Error = "Failed to add claim.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        return Ok(new AddClaimToUserDtoResponseDto { Message = "User claim added successfully." });
    }
}

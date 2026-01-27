using System.Security.Claims;
using AuthService.Constants;
using AuthService.Data;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Users;
using AuthService.Services.UserImport;
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
    AuthServiceDbContext dbContext,
    IUserImportService userImportService
) : ControllerBase
{
    private readonly UserManager<AuthServiceUser> _userManager = userManager;

    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    private readonly AuthServiceDbContext _dbContext = dbContext;

    private readonly IUserImportService _userImportService = userImportService;

    // --------------------------------------------------------------
    // Import Users
    // --------------------------------------------------------------
    [HttpPost("import")]
    public async Task<ActionResult<ImportUsersResponseDto>> ImportUsers(
        [FromBody] ImportUsersRequestDto dto,
        CancellationToken cancellationToken
    )
    {
        var futureResponse = new ImportUsersResponseDto();

        foreach (ImportUserDto import in dto.Users)
        {
            await _userImportService.ImportSingleUserAsync(
                import,
                futureResponse,
                cancellationToken
            );
        }

        return Ok(futureResponse);
    }

    // --------------------------------------------------------------
    // Delete User
    // --------------------------------------------------------------
    [HttpDelete("one/{userIdentifier}")]
    public async Task<ActionResult<DeleteUserResponseDto>> DeleteUser(string userIdentifier)
    {
        AuthServiceUser? user = await _userManager
            .Users.Include(u => u.AppSettings)
            .Include(u => u.CustomProperties)
            .FirstOrDefaultAsync(u =>
                u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
            );

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto
                {
                    Error = "Failed to delete user.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );
        }

        return Ok(new DeleteUserResponseDto());
    }

    // --------------------------------------------------------------
    // Get All Users
    // --------------------------------------------------------------
    [HttpGet("many/{includeSettings:bool}/{includeProperties:bool}")]
    public async Task<ActionResult<GetUsersResponseDto>> GetUsers(
        bool includeSettings,
        bool includeProperties,
        [FromQuery] string? whereOfficeLocation
    )
    {
        IQueryable<AuthServiceUser> query = _userManager.Users;

        if (includeSettings)
            query = query.Include(u => u.AppSettings);

        if (includeProperties)
            query = query.Include(u => u.CustomProperties);

        if (!string.IsNullOrWhiteSpace(whereOfficeLocation))
            query = query.Where(u => u.OfficeLocation == whereOfficeLocation);

        List<AuthServiceUser> users = await query.ToListAsync();
        List<AuthServiceUserDto> passports = [.. users.Select(u => new AuthServiceUserDto(u))];

        return Ok(new GetUsersResponseDto { Users = passports });
    }

    // --------------------------------------------------------------
    // Get Specific User by Id / Username / Email
    // --------------------------------------------------------------
    [HttpGet("one/{userIdentifier}/{includeSettings:bool}/{includeProperties:bool}")]
    public async Task<ActionResult<AuthServiceUserDto>> GetUser(
        string userIdentifier,
        bool includeSettings,
        bool includeProperties
    )
    {
        IQueryable<AuthServiceUser> query = _userManager.Users;

        if (includeSettings)
            query = query.Include(u => u.AppSettings);

        if (includeProperties)
            query = query.Include(u => u.CustomProperties);

        AuthServiceUser? user = await query.FirstOrDefaultAsync(u =>
            u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        );

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

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
                new ErrorResponseDto
                {
                    Error = $"Invalid preferred language '{dto.PreferredLanguageCode}'. ",
                    Details = $"Allowed values: {string.Join(", ", PreferredLanguage.Options)}",
                }
            );
        }

        if (!ColorTheme.IsValid(dto.ColorThemeCode))
        {
            return BadRequest(
                new ErrorResponseDto
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
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        if (user.AppSettings == null)
        {
            var settings = new AuthServiceUserAppSettings
            {
                Id = user.Id,
                PreferredLanguageCode = dto.PreferredLanguageCode,
                ColorThemeCode = dto.ColorThemeCode,
                User = user,
            };
            _dbContext.AspNetUsersAppSettings.Add(settings);
        }
        else
        {
            user.AppSettings.PreferredLanguageCode = dto.PreferredLanguageCode;
            user.AppSettings.ColorThemeCode = dto.ColorThemeCode;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new UpdateUserSettingsResponseDto());
    }

    // --------------------------------------------------------------
    // Update User Properties
    // --------------------------------------------------------------
    [HttpPut("one/{userIdentifier}/properties")]
    public async Task<ActionResult<UpdateUserPropertiesResponseDto>> UpdateUserProperties(
        string userIdentifier,
        [FromBody] UpdateUserPropertiesDto dto
    )
    {
        if (!ConfidentialityClass.IsValid(dto.Confidentiality))
        {
            return BadRequest(
                new ErrorResponseDto
                {
                    Error = $"Invalid confidentiality class '{dto.Confidentiality}'. ",
                    Details = $"Allowed values: {string.Join(", ", ConfidentialityClass.Options)}",
                }
            );
        }

        AuthServiceUser? user = await _userManager
            .Users.Include(u => u.CustomProperties)
            .FirstOrDefaultAsync(u =>
                u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
            );

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        if (user.CustomProperties == null)
        {
            var properties = new AuthServiceUserCustomProperties(user, dto);
            // {
            //     Id = user.Id,
            //     Confidentiality = dto.Confidentiality,
            //     User = user,
            // };

            _dbContext.AspNetUsersCustomProperties.Add(properties);
        }
        else
        {
            user.CustomProperties.UpdateProperties(user, dto);
            // user.CustomProperties.Confidentiality = dto.Confidentiality;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new UpdateUserPropertiesResponseDto());
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
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

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
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        // Find the claim by value
        var claims = await _userManager.GetClaimsAsync(user);
        var claimToRemove = claims.FirstOrDefault(c =>
            c.Type == "permission" && c.Value == userClaimValue
        );

        if (claimToRemove == null)
            return NotFound(
                new ErrorResponseDto
                {
                    Error = $"Claim '{userClaimValue}' not found for user '{userIdentifier}'.",
                }
            );

        var result = await _userManager.RemoveClaimAsync(user, claimToRemove);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponseDto
                {
                    Error = "Failed to remove claim.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        return Ok(new DeleteClaimFromUserResponseDto());
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
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        if (dto.Tool.Contains('.'))
        {
            return BadRequest(
                new ErrorResponseDto
                {
                    Error = "Tool cannot contain '.' character.",
                    Details = dto.Privilege,
                }
            );
        }

        if (dto.Privilege.Contains('.'))
        {
            return BadRequest(
                new ErrorResponseDto
                {
                    Error = "Privilege cannot contain '.' character.",
                    Details = dto.Privilege,
                }
            );
        }

        var userClaimValue = $"user.{dto.Tool.ToLower()}.{dto.Privilege.ToLower()}";

        var existingClaims = await _userManager.GetClaimsAsync(user);
        if (existingClaims.Any(c => c.Type == "permission" && c.Value == userClaimValue))
        {
            return BadRequest(
                new ErrorResponseDto
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
                new ErrorResponseDto
                {
                    Error = "Failed to add claim.",
                    Details = string.Join(", ", result.Errors.Select(e => e.Description)),
                }
            );

        return Ok(new AddClaimToUserDtoResponseDto());
    }
}

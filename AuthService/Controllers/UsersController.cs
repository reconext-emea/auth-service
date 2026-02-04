using System.Security.Claims;
using AuthService.Constants;
using AuthService.Data;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using AuthService.Models.Dto.Users;
using AuthService.Services.Identity;
using AuthService.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace AuthService.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "users")]
[Route("api/users")]
[Authorize]
public class UsersController(
    UserManager<AuthServiceUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager appManager,
    AuthServiceDbContext dbContext,
    IUserService userService,
    OfficeLocationToRegionAdapter adapter
) : ControllerBase
{
    private readonly UserManager<AuthServiceUser> _userManager = userManager;

    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    private readonly IOpenIddictApplicationManager _appManager = appManager;

    private readonly AuthServiceDbContext _dbContext = dbContext;

    private readonly IUserService _userService = userService;
    private readonly OfficeLocationToRegionAdapter _adapter = adapter;

    // --------------------------------------------------------------
    // Get distinct departments of users
    // --------------------------------------------------------------
    [HttpGet("departments")]
    public async Task<ActionResult<GetDepartmentsResponseDto>> GetDepartments(
        [FromQuery] string? whereOfficeLocation
    )
    {
        IQueryable<AuthServiceUser> query = _userManager.Users;

        if (!string.IsNullOrWhiteSpace(whereOfficeLocation))
            query = query.Where(u => u.OfficeLocation == whereOfficeLocation);

        IQueryable<string> departmentsQuery = query
            .Where(u => !string.IsNullOrWhiteSpace(u.Department))
            .Select(u => u.Department)
            .Distinct();

        List<string> departments = await departmentsQuery.ToListAsync();

        return Ok(new GetDepartmentsResponseDto(departments));
    }

    // --------------------------------------------------------------
    // Import User
    // --------------------------------------------------------------
    // [HttpPost("one/import")]
    // public async Task<ActionResult<ImportUsersResponseDto>> ImportUser(
    //     [FromBody] ImportUserRequestDto dto,
    //     CancellationToken cancellationToken
    // )
    // {
    //     var futureResponse = new ImportUsersResponseDto();

    //     await _userService.ImportSingleUserAsync(dto.User, futureResponse, cancellationToken);

    //     return Ok(futureResponse);
    // }

    // --------------------------------------------------------------
    // Import Users
    // --------------------------------------------------------------
    [HttpPost("many/import")]
    public async Task<ActionResult<ImportUsersResponseDto>> ImportUsers(
        [FromBody] ImportUsersRequestDto dto,
        CancellationToken cancellationToken
    )
    {
        var futureResponse = new ImportUsersResponseDto();

        foreach (ImportUserDto import in dto.Users)
        {
            await _userService.ImportSingleUserAsync(import, futureResponse, cancellationToken);
        }

        return Ok(futureResponse);
    }

    // --------------------------------------------------------------
    // Delete User
    // --------------------------------------------------------------
    [HttpDelete("one/{userIdentifier}")]
    public async Task<ActionResult<DeleteUserResponseDto>> DeleteUser(string userIdentifier)
    {
        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(userIdentifier);

        // await _userManager
        //     .Users.Include(u => u.AppSettings)
        //     .Include(u => u.CustomProperties)
        //     .FirstOrDefaultAsync(u =>
        //         u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        //     );

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
    [HttpGet("many")]
    public async Task<ActionResult<GetUsersResponseDto>> GetUsers(
        [FromQuery] string? whereOfficeLocation,
        [FromQuery] string? whereDepartment,
        [FromQuery] string? whereJobTitle,
        [FromQuery] string? whereConfidentiality,
        [FromQuery] string? whereClientId
    )
    {
        IQueryable<AuthServiceUser> query = _userManager.Users;

        query = query
            .Include(u => u.AppSettings)
            .Include(u => u.CustomProperties)
            .Include(u => u.Applications)
                .ThenInclude(ua => ua.Application);

        if (!string.IsNullOrWhiteSpace(whereOfficeLocation))
            query = query.Where(u => u.OfficeLocation == whereOfficeLocation);

        if (!string.IsNullOrWhiteSpace(whereDepartment))
            query = query.Where(u => u.Department == whereDepartment);

        if (!string.IsNullOrWhiteSpace(whereJobTitle))
            query = query.Where(u => u.JobTitle == whereJobTitle);

        if (!string.IsNullOrWhiteSpace(whereConfidentiality))
            query = query.Where(u => u.CustomProperties.Confidentiality == whereConfidentiality);

        if (!string.IsNullOrWhiteSpace(whereClientId))
            query = query.Where(u =>
                u.Applications.Any(a => a.Application.ClientId == whereClientId)
            );

        List<AuthServiceUser> users = await query.ToListAsync();
        List<AuthServiceUserDto> passports = [.. users.Select(AuthServiceUserDto.From)];

        return Ok(new GetUsersResponseDto(passports));
    }

    // --------------------------------------------------------------
    // Get Specific User by Id / Username / Email
    // --------------------------------------------------------------
    [HttpGet("one/{userIdentifier}")]
    public async Task<ActionResult<AuthServiceUserDto>> GetUser(string userIdentifier)
    {
        IQueryable<AuthServiceUser> query = _userManager.Users;

        query = query
            .Include(u => u.AppSettings)
            .Include(u => u.CustomProperties)
            .Include(u => u.Applications)
                .ThenInclude(ua => ua.Application);

        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(query, userIdentifier);

        // await query.FirstOrDefaultAsync(u =>
        //     u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        // );

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        AuthServiceUserDto passport = AuthServiceUserDto.From(user);

        return Ok(new GetUserResponseDto(passport));
    }

    // --------------------------------------------------------------
    // Update User EmployeeId
    // --------------------------------------------------------------
    [HttpPut("one/{userIdentifier}/employee-id")]
    public async Task<ActionResult<UpdateEmployeeIdResponseDto>> UpdateEmployeeId(
        string userIdentifier,
        [FromBody] UpdateEmployeeIdDto dto
    )
    {
        if (string.IsNullOrWhiteSpace(dto.EmployeeId))
        {
            return BadRequest(new ErrorResponseDto { Error = "EmployeeId cannot be empty." });
        }

        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(userIdentifier);

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        user.SetEmployeeId(dto.EmployeeId);

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(
                new ErrorResponseDto
                {
                    Error = "Failed to update EmployeeId.",
                    Details = string.Join(
                        "; ",
                        result.Errors.Select(e => $"{e.Code}: {e.Description}")
                    ),
                }
            );
        }

        return Ok(new UpdateEmployeeIdResponseDto());
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

        if (!PreferredColorTheme.IsValid(dto.PreferredColorThemeCode))
        {
            return BadRequest(
                new ErrorResponseDto
                {
                    Error = $"Invalid color theme '{dto.PreferredColorThemeCode}'. ",
                    Details = $"Allowed values: {string.Join(", ", PreferredColorTheme.Options)}",
                }
            );
        }

        IQueryable<AuthServiceUser> query = _userManager.Users;

        query = query.Include(u => u.AppSettings);

        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(query, userIdentifier);

        // AuthServiceUser? user = await _userManager
        //     .Users.Include(u => u.AppSettings)
        //     .FirstOrDefaultAsync(u =>
        //         u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        //     );

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        if (user.AppSettings == null)
        {
            var settings = new AuthServiceUserAppSettings
            {
                Id = user.Id,
                PreferredLanguageCode = dto.PreferredLanguageCode,
                PreferredColorThemeCode = dto.PreferredColorThemeCode,
                User = user,
            };
            _dbContext.AspNetUsersAppSettings.Add(settings);
        }
        else
        {
            user.AppSettings.PreferredLanguageCode = dto.PreferredLanguageCode;
            user.AppSettings.PreferredColorThemeCode = dto.PreferredColorThemeCode;
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

        IQueryable<AuthServiceUser> query = _userManager.Users;

        query = query.Include(u => u.CustomProperties);

        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(query, userIdentifier);

        // AuthServiceUser? user = await _userManager
        //     .Users.Include(u => u.CustomProperties)
        //     .FirstOrDefaultAsync(u =>
        //         u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        //     );

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        if (user.CustomProperties == null)
        {
            var properties = new AuthServiceUserCustomProperties(user, _adapter, dto);

            _dbContext.AspNetUsersCustomProperties.Add(properties);
        }
        else
        {
            user.CustomProperties.UpdateProperties(user, _adapter, dto);
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
        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(userIdentifier);

        // AuthServiceUser? user = await _userManager.Users.FirstOrDefaultAsync(u =>
        //     u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        // );

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
        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(userIdentifier);

        // await _userManager.Users.FirstOrDefaultAsync(u =>
        //     u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        // );

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
        if (await _appManager.FindByClientIdAsync(dto.Tool) is null)
            return NotFound(
                new ErrorResponseDto
                {
                    Error =
                        $"Application (clientId='{dto.Tool}') not found. "
                        + "User permission claims are specialized role claims, so the Tool part of a claim must match the clientId of a registered application. "
                        + "Applications are registered when a role is created, where the Tool value is converted to kebab-case and used as the clientId.",
                }
            );

        if (!PermissionClaimService.TryCreateUserClaim(dto, out var claim, out var error))
        {
            return BadRequest(error);
        }

        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(userIdentifier);

        // AuthServiceUser? user = await _userManager.Users.FirstOrDefaultAsync(u =>
        //     u.Id == userIdentifier || u.UserName == userIdentifier || u.Email == userIdentifier
        // );

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        var existingClaims = await _userManager.GetClaimsAsync(user);

        bool hasClaim = existingClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value);

        if (hasClaim)
        {
            return BadRequest(
                new ErrorResponseDto
                {
                    Error = "User already has this claim.",
                    Details = claim.Value,
                }
            );
        }

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

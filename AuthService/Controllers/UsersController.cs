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
public class UsersController(UserManager<AuthServiceUser> userManager, AuthServiceDbContext db)
    : ControllerBase
{
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
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

        return Ok(
            new UpdateUserSettingsResponseDto
            {
                UserIdentifier = userIdentifier,
                Message = "Settings updated successfully.",
            }
        );
    }

    // --------------------------------------------------------------
    // Helper Methods
    // --------------------------------------------------------------
    // private async Task<AuthServiceUser?> FindUser(string input)
    // {
    //     // Try by Id
    //     var user = await _userManager.FindByIdAsync(input);
    //     if (user != null)
    //         return user;

    //     // Try by UserName
    //     user = await _userManager.FindByNameAsync(input);
    //     if (user != null)
    //         return user;

    //     // Try by Email
    //     return await _userManager.FindByEmailAsync(input);
    // }
}

using AuthService.Data;
using AuthService.Models;
using AuthService.Models.Dto.Applications;
using AuthService.Models.Dto.Errors;
using AuthService.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace AuthService.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "applications")]
[Route("api/applications")]
public class ApplicationsController(
    // UserManager<AuthServiceUser> userManager,
    // IOpenIddictApplicationManager appManager,
    IUserService userService,
    AuthServiceDbContext dbContext
) : ControllerBase
{
    // private readonly UserManager<AuthServiceUser> _userManager = userManager;

    // private readonly IOpenIddictApplicationManager _appManager = appManager;
    private readonly IUserService _userService = userService;

    private readonly AuthServiceDbContext _dbContext = dbContext;

    // --------------------------------------------------------------
    // Get All Applications (like Get All Roles)
    // --------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<GetApplicationsResponseDto>> GetApplications()
    {
        var apps = await _dbContext
            .Set<OpenIddictEntityFrameworkCoreApplication>()
            .AsNoTracking()
            .Select(app => new ApplicationDto(app.Id!, app.ClientId, app.DisplayName))
            .ToListAsync();

        return Ok(new GetApplicationsResponseDto(apps));
    }

    // --------------------------------------------------------------
    // Get Applications of a Specific User (like Get Roles of User)
    // --------------------------------------------------------------
    [HttpGet("of-user/{userIdentifier}")]
    public async Task<ActionResult<GetApplicationsOfUserResponseDto>> GetApplicationsOfUser(
        string userIdentifier
    )
    {
        // var user = await FindUser(userIdentifier);
        AuthServiceUser? user = await _userService.FindUserByIdentifierAsync(userIdentifier);

        if (user == null)
            return NotFound(new ErrorResponseDto { Error = $"User '{userIdentifier}' not found." });

        var appIds = await _dbContext
            .Set<AuthServiceUserApplication>()
            .AsNoTracking()
            .Where(userApp => userApp.UserId == user.Id)
            .Select(userApp => userApp.ApplicationId)
            .ToListAsync();

        var apps = await _dbContext
            .Set<OpenIddictEntityFrameworkCoreApplication>()
            .AsNoTracking()
            .Where(app => appIds.Contains(app.Id!))
            .Select(app => new ApplicationDto(app.Id!, app.ClientId, app.DisplayName))
            .ToListAsync();

        return Ok(new GetApplicationsOfUserResponseDto(apps));
    }

    // --------------------------------------------------------------
    // Create Application
    // --------------------------------------------------------------
    // [HttpPost]
    // public async Task<ActionResult<CreateApplicationResponseDto>> CreateApplication(
    //     [FromBody] CreateApplicationDto dto
    // )
    // {
    //     var descriptor = new OpenIddictApplicationDescriptor
    //     {
    //         ClientId = dto.ClientId,
    //         DisplayName = dto.DisplayName,
    //         ClientType = OpenIddictConstants.ClientTypes.Public,
    //     };

    //     await _appManager.CreateAsync(descriptor);

    //     return Ok(new CreateApplicationResponseDto());
    // }

    // --------------------------------------------------------------
    // Delete Application
    // --------------------------------------------------------------
    // [HttpDelete("{applicationIdentifier}")]
    // public async Task<IActionResult> DeleteApplication(string applicationIdentifier)
    // {
    //     var app = await FindApplication(applicationIdentifier);
    //     if (app == null)
    //         return NotFound(
    //             new ErrorResponseDto { Error = $"Application '{applicationIdentifier}' not found." }
    //         );

    //     await _appManager.DeleteAsync(app);

    //     return Ok(new DeleteApplicationResponseDto());
    // }

    // --------------------------------------------------------------
    // Assign Application (like Assign Role)
    // --------------------------------------------------------------
    // [HttpPost("assign")]
    // public async Task<ActionResult<AssignApplicationResponseDto>> AssignApplication(
    //     [FromBody] AssignApplicationDto dto
    // )
    // {
    //     var user = await FindUser(dto.UserIdentifier);
    //     if (user == null)
    //         return NotFound(
    //             new ErrorResponseDto { Error = $"User '{dto.UserIdentifier}' not found." }
    //         );

    //     var app = await FindApplication(dto.ApplicationIdentifier);
    //     if (app == null)
    //         return NotFound(
    //             new ErrorResponseDto
    //             {
    //                 Error = $"Application '{dto.ApplicationIdentifier}' not found.",
    //             }
    //         );

    //     var already = await _dbContext
    //         .Set<AuthServiceUserApplication>()
    //         .AnyAsync(x => x.UserId == user.Id && x.ApplicationId == app.Id);

    //     if (already)
    //         return Conflict(
    //             new ErrorResponseDto { Error = "User already has this application assigned." }
    //         );

    //     _dbContext
    //         .Set<AuthServiceUserApplication>()
    //         .Add(new AuthServiceUserApplication { UserId = user.Id, ApplicationId = app.Id! });

    //     await _dbContext.SaveChangesAsync();

    //     return Ok(new AssignApplicationResponseDto());
    // }

    // --------------------------------------------------------------
    // Unassign Application (like Unassign Role)
    // --------------------------------------------------------------
    // [HttpPost("unassign")]
    // public async Task<ActionResult<UnassignApplicationResponseDto>> UnassignApplication(
    //     [FromBody] UnassignApplicationDto dto
    // )
    // {
    //     var user = await FindUser(dto.UserIdentifier);
    //     if (user == null)
    //         return NotFound(
    //             new ErrorResponseDto { Error = $"User '{dto.UserIdentifier}' not found." }
    //         );

    //     var app = await FindApplication(dto.ApplicationIdentifier);
    //     if (app == null)
    //         return NotFound(
    //             new ErrorResponseDto
    //             {
    //                 Error = $"Application '{dto.ApplicationIdentifier}' not found.",
    //             }
    //         );

    //     var link = await _dbContext
    //         .Set<AuthServiceUserApplication>()
    //         .FirstOrDefaultAsync(x => x.UserId == user.Id && x.ApplicationId == app.Id);

    //     if (link == null)
    //         return Conflict(
    //             new ErrorResponseDto { Error = "User does not have this application assigned." }
    //         );

    //     _dbContext.Set<AuthServiceUserApplication>().Remove(link);
    //     await _dbContext.SaveChangesAsync();

    //     return Ok(new UnassignApplicationResponseDto());
    // }

    // --------------------------------------------------------------
    // Helper Methods
    // --------------------------------------------------------------

    /// <summary>
    /// Finds an OpenIddict application either by database Id or by ClientId.
    /// </summary>
    // private async Task<OpenIddictEntityFrameworkCoreApplication?> FindApplication(
    //     string applicationIdentifier
    // )
    // {
    //     // Try by Id
    //     var app = await _dbContext
    //         .Set<OpenIddictEntityFrameworkCoreApplication>()
    //         .FirstOrDefaultAsync(a => a.Id == applicationIdentifier);

    //     if (app != null)
    //         return app;

    //     // Try by ClientId
    //     return await _dbContext
    //         .Set<OpenIddictEntityFrameworkCoreApplication>()
    //         .FirstOrDefaultAsync(a => a.ClientId == applicationIdentifier);
    // }
}

using OpenIddict.EntityFrameworkCore.Models;

namespace AuthService.Models;

/// <summary>
/// Join table: AspNetUsersOpenIddictApplications
/// </summary>
public class AuthServiceUserApplication
{
    public string UserId { get; set; } = default!;
    public AuthServiceUser User { get; set; } = default!;

    // OpenIddict application primary key (string by default)
    public string ApplicationId { get; set; } = default!;

    public OpenIddictEntityFrameworkCoreApplication Application { get; set; } = default!;
}

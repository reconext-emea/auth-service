using Microsoft.AspNetCore.Identity;

namespace AuthService.Models;

public class ApplicationUser : IdentityUser
{
    public required string OfficeLocation { get; set; }
}

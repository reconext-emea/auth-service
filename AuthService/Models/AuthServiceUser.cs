using AuthService.Clients.LdapClient;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Models;

public class AuthServiceUser : IdentityUser
{
    public string DisplayName { get; private set; } = null!;
    public string OfficeLocation { get; private set; } = null!;

    private AuthServiceUser() { }

    public static AuthServiceUser CreateFromLdap(LdapUser ldapUser)
    {
        return new AuthServiceUser
        {
            UserName = ldapUser.Username,
            Email = BuildEmail(ldapUser.Username, ldapUser.Domain),
            DisplayName = BuildDisplayName(ldapUser.Username),
            OfficeLocation = ldapUser.OfficeLocation,
        };
    }

    public AuthServiceUser UpdateFromLdap(LdapUser ldapUser)
    {
        UserName = ldapUser.Username;
        Email = BuildEmail(ldapUser.Username, ldapUser.Domain);
        DisplayName = BuildDisplayName(ldapUser.Username);
        OfficeLocation = ldapUser.OfficeLocation;

        return this;
    }

    private static string BuildDisplayName(string username)
    {
        var usernameParts = username.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var uppercaseUsernameParts = usernameParts
            .Select(p => char.ToUpper(p[0]) + p.Substring(1))
            .ToArray();
        return string.Join(" ", uppercaseUsernameParts);
    }

    private static string BuildEmail(string username, string domain)
    {
        return $"{username}@{domain}";
    }
}

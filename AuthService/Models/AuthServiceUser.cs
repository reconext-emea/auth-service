using AuthService.Clients.GraphClient;
using AuthService.Clients.LdapClient;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Models;

public class AuthServiceUser : IdentityUser
{
    public string DisplayName { get; private set; } = null!;
    public string OfficeLocation { get; private set; } = null!;

    public AuthServiceUserAppSettings AppSettings { get; set; } = null!;

    private AuthServiceUser() { }

    public static AuthServiceUser CreateFromLdap(LdapUser ldapUser)
    {
        return new AuthServiceUser
        {
            UserName = NormalizeUserName(ldapUser.Username),
            Email = BuildEmail(ldapUser.Username, ldapUser.Domain),
            DisplayName = BuildDisplayName(ldapUser.Username),
            OfficeLocation = ldapUser.OfficeLocation,

            AppSettings = CreateDefaultSettings(),
        };
    }

    public AuthServiceUser UpdateFromLdap(LdapUser ldapUser)
    {
        UserName = NormalizeUserName(ldapUser.Username);
        Email = BuildEmail(ldapUser.Username, ldapUser.Domain);
        DisplayName = BuildDisplayName(ldapUser.Username);
        OfficeLocation = ldapUser.OfficeLocation;

        return this;
    }

    public static AuthServiceUser CreateFromGraph(GraphUser graphUser)
    {
        return new AuthServiceUser
        {
            UserName = NormalizeUserName(graphUser.Username),
            Email = NormalizeEmail(graphUser.Mail),
            DisplayName = BuildDisplayName(graphUser.Username),
            OfficeLocation = graphUser.OfficeLocation,

            AppSettings = CreateDefaultSettings(),
        };
    }

    public AuthServiceUser UpdateFromGraph(GraphUser graphUser)
    {
        UserName = NormalizeUserName(graphUser.Username);
        Email = NormalizeEmail(graphUser.Mail);
        DisplayName = BuildDisplayName(graphUser.Username);
        OfficeLocation = graphUser.OfficeLocation;

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

    private static string NormalizeUserName(string username)
    {
        return username.ToLower();
    }

    private static string NormalizeEmail(string mail)
    {
        return mail.ToLower();
    }

    private static string BuildEmail(string username, string domain)
    {
        return NormalizeEmail($"{username}@{domain}");
    }

    private static AuthServiceUserAppSettings CreateDefaultSettings() => new();
}

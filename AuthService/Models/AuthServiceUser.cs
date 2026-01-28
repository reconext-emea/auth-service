using AuthService.Clients.GraphClient;
using AuthService.Clients.LdapClient;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Models;

public class AuthServiceUser : IdentityUser
{
    public string DisplayName { get; private set; } = string.Empty;
    public string OfficeLocation { get; private set; } = string.Empty;
    public string EmployeeId { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string JobTitle { get; private set; } = string.Empty;

    public AuthServiceUserAppSettings AppSettings { get; set; } = new();
    public AuthServiceUserCustomProperties CustomProperties { get; set; } = new();

    private AuthServiceUser() { }

    private AuthServiceUser(string username)
    {
        UserName = NormalizeUserName(username);
        Email = BuildEmail(username, "reconext.com");
    }

    private AuthServiceUser(LdapUser ldapUser)
    {
        UserName = NormalizeUserName(ldapUser.Username);
        Email = BuildEmail(ldapUser.Username, ldapUser.Domain);
        DisplayName = ldapUser.Attributes.DisplayName;
        EmployeeId = ldapUser.Attributes.EmployeeId;
        Department = ldapUser.Attributes.Department;
        JobTitle = ldapUser.Attributes.JobTitle;
        OfficeLocation = ldapUser.Attributes.OfficeLocation;
    }

    private AuthServiceUser(GraphUser graphUser)
    {
        UserName = NormalizeUserName(graphUser.Username);
        Email = NormalizeEmail(graphUser.Mail);
        DisplayName = graphUser.Attributes.DisplayName;
        EmployeeId = graphUser.Attributes.EmployeeId;
        Department = graphUser.Attributes.Department;
        JobTitle = graphUser.Attributes.JobTitle;
        OfficeLocation = graphUser.Attributes.OfficeLocation;
    }

    public static AuthServiceUser CreateFromImport(string username) => new(username);

    public static AuthServiceUser CreateFromLdap(LdapUser ldapUser) => new(ldapUser);

    public AuthServiceUser UpdateFromLdap(LdapUser ldapUser)
    {
        UserName = NormalizeUserName(ldapUser.Username);
        Email = BuildEmail(ldapUser.Username, ldapUser.Domain);
        DisplayName = ldapUser.Attributes.DisplayName;
        EmployeeId = ldapUser.Attributes.EmployeeId;
        Department = ldapUser.Attributes.Department;
        JobTitle = ldapUser.Attributes.JobTitle;
        OfficeLocation = ldapUser.Attributes.OfficeLocation;

        CustomProperties.UpdateProperties(this, null);

        return this;
    }

    public static AuthServiceUser CreateFromGraph(GraphUser graphUser) => new(graphUser);

    public AuthServiceUser UpdateFromGraph(GraphUser graphUser)
    {
        UserName = NormalizeUserName(graphUser.Username);
        Email = NormalizeEmail(graphUser.Mail);
        DisplayName = graphUser.Attributes.DisplayName;
        EmployeeId = graphUser.Attributes.EmployeeId;
        Department = graphUser.Attributes.Department;
        JobTitle = graphUser.Attributes.JobTitle;
        OfficeLocation = graphUser.Attributes.OfficeLocation;

        CustomProperties.UpdateProperties(this, null);

        return this;
    }

    private static string NormalizeUserName(string username) => username.ToLower();

    private static string NormalizeEmail(string mail) => mail.ToLower();

    private static string BuildEmail(string username, string domain)
    {
        return NormalizeEmail($"{NormalizeUserName(username)}@{domain}");
    }
}

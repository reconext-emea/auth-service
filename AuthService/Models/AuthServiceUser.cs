using AuthService.Clients.GraphClient;
using AuthService.Clients.LdapClient;
using AuthService.Constants;
using AuthService.Models.Dto.Users;
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

    public ICollection<AuthServiceUserApplication> Applications { get; set; } = [];

    private AuthServiceUser() { }

    private AuthServiceUser(
        LdapUser ldapUser,
        OfficeLocationToRegionAdapter adapter,
        UpdateUserPropertiesDto? dto
    )
    {
        UserName = NormalizeUserName(ldapUser.Username);
        Email = BuildEmail(ldapUser.Username, ldapUser.Domain);
        DisplayName = ldapUser.Attributes.DisplayName;
        EmployeeId = ldapUser.Attributes.EmployeeId;
        Department = ldapUser.Attributes.Department;
        JobTitle = ldapUser.Attributes.JobTitle;
        OfficeLocation = ldapUser.Attributes.OfficeLocation;

        CustomProperties.UpdateProperties(this, adapter, dto);
    }

    private AuthServiceUser(
        GraphUser graphUser,
        OfficeLocationToRegionAdapter adapter,
        UpdateUserPropertiesDto? dto
    )
    {
        UserName = NormalizeUserName(graphUser.Username);
        Email = NormalizeEmail(graphUser.Mail);
        DisplayName = graphUser.Attributes.DisplayName;
        EmployeeId = graphUser.Attributes.EmployeeId;
        Department = graphUser.Attributes.Department;
        JobTitle = graphUser.Attributes.JobTitle;
        OfficeLocation = graphUser.Attributes.OfficeLocation;

        CustomProperties.UpdateProperties(this, adapter, dto);
    }

    public static AuthServiceUser CreateFromLdap(
        LdapUser ldapUser,
        OfficeLocationToRegionAdapter adapter,
        UpdateUserPropertiesDto? dto = null
    ) => new(ldapUser, adapter, dto);

    public AuthServiceUser UpdateFromLdap(
        LdapUser ldapUser,
        OfficeLocationToRegionAdapter adapter,
        UpdateUserPropertiesDto? dto = null
    )
    {
        UserName = NormalizeUserName(ldapUser.Username);
        Email = BuildEmail(ldapUser.Username, ldapUser.Domain);
        DisplayName = ldapUser.Attributes.DisplayName;
        EmployeeId = ldapUser.Attributes.EmployeeId;
        Department = ldapUser.Attributes.Department;
        JobTitle = ldapUser.Attributes.JobTitle;
        OfficeLocation = ldapUser.Attributes.OfficeLocation;

        CustomProperties.UpdateProperties(this, adapter, dto);

        return this;
    }

    public static AuthServiceUser CreateFromGraph(
        GraphUser graphUser,
        OfficeLocationToRegionAdapter adapter,
        UpdateUserPropertiesDto? dto = null
    ) => new(graphUser, adapter, dto);

    public AuthServiceUser UpdateFromGraph(
        GraphUser graphUser,
        OfficeLocationToRegionAdapter adapter,
        UpdateUserPropertiesDto? dto = null
    )
    {
        UserName = NormalizeUserName(graphUser.Username);
        Email = NormalizeEmail(graphUser.Mail);
        DisplayName = graphUser.Attributes.DisplayName;
        EmployeeId = graphUser.Attributes.EmployeeId;
        Department = graphUser.Attributes.Department;
        JobTitle = graphUser.Attributes.JobTitle;
        OfficeLocation = graphUser.Attributes.OfficeLocation;

        CustomProperties.UpdateProperties(this, adapter, dto);

        return this;
    }

    private static string NormalizeUserName(string username) => username.ToLowerInvariant();

    private static string NormalizeEmail(string mail) => mail.ToLowerInvariant();

    private static string BuildEmail(string username, string domain)
    {
        return NormalizeEmail($"{NormalizeUserName(username)}@{domain}");
    }

    public void SetEmployeeId(string employeeId)
    {
        EmployeeId = (employeeId ?? string.Empty).Trim();
    }
}

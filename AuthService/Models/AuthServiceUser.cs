using AuthService.Clients.GraphClient;
using AuthService.Clients.LdapClient;
using AuthService.Models.Dto.Users;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Models;

public class AuthServiceUser : IdentityUser
{
    public string DisplayName { get; private set; } = null!;
    public string OfficeLocation { get; private set; } = null!;
    public string EmployeeId { get; private set; } = null!;
    public string Department { get; private set; } = null!;
    public string JobTitle { get; private set; } = null!;

    public AuthServiceUserAppSettings AppSettings { get; set; } = null!;
    public AuthServiceUserCustomProperties CustomProperties { get; set; } = null!;

    private AuthServiceUser() { }

    public static AuthServiceUser CreateFromImport(
        string importUsername,
        UpdateUserSettingsDto? settingsDto = null
    )
    {
        return new AuthServiceUser
        {
            UserName = NormalizeUserName(importUsername),
            Email = string.Empty,
            // DisplayName = BuildDisplayName(importUsername),
            DisplayName = string.Empty,

            EmployeeId = string.Empty,
            Department = string.Empty,
            JobTitle = string.Empty,

            OfficeLocation = string.Empty,

            AppSettings = settingsDto is null
                ? CreateDefaultSettings()
                : CreateSettings(settingsDto),
            CustomProperties = CreateDefaultProperties(),
        };
    }

    public static AuthServiceUser CreateFromLdap(LdapUser ldapUser)
    {
        return new AuthServiceUser
        {
            UserName = NormalizeUserName(ldapUser.Username),
            Email = BuildEmail(ldapUser.Username, ldapUser.Domain),
            // DisplayName = BuildDisplayName(ldapUser.Username),
            DisplayName = ldapUser.Attributes.DisplayName,

            EmployeeId = ldapUser.Attributes.EmployeeId,
            Department = ldapUser.Attributes.Department,
            JobTitle = ldapUser.Attributes.JobTitle,

            OfficeLocation = ldapUser.Attributes.OfficeLocation,

            AppSettings = CreateDefaultSettings(),
            CustomProperties = CreateDefaultProperties(),
        };
    }

    public AuthServiceUser UpdateFromLdap(LdapUser ldapUser)
    {
        UserName = NormalizeUserName(ldapUser.Username);
        Email = BuildEmail(ldapUser.Username, ldapUser.Domain);
        // DisplayName = BuildDisplayName(ldapUser.Username),
        DisplayName = ldapUser.Attributes.DisplayName;

        EmployeeId = ldapUser.Attributes.EmployeeId;
        Department = ldapUser.Attributes.Department;
        JobTitle = ldapUser.Attributes.JobTitle;

        OfficeLocation = ldapUser.Attributes.OfficeLocation;

        AppSettings ??= CreateDefaultSettings();

        if (CustomProperties is not null)
            CustomProperties.UpdateProperties(this, null);
        else
            CustomProperties = CreateDefaultProperties();

        return this;
    }

    public static AuthServiceUser CreateFromGraph(GraphUser graphUser)
    {
        return new AuthServiceUser
        {
            UserName = NormalizeUserName(graphUser.Username),
            Email = NormalizeEmail(graphUser.Mail),
            // DisplayName = BuildDisplayName(graphUser.Username),
            DisplayName = graphUser.Attributes.DisplayName,

            EmployeeId = graphUser.Attributes.EmployeeId,
            Department = graphUser.Attributes.Department,
            JobTitle = graphUser.Attributes.JobTitle,

            OfficeLocation = graphUser.Attributes.OfficeLocation,

            AppSettings = CreateDefaultSettings(),
            CustomProperties = CreateDefaultProperties(),
        };
    }

    public AuthServiceUser UpdateFromGraph(GraphUser graphUser)
    {
        UserName = NormalizeUserName(graphUser.Username);
        Email = NormalizeEmail(graphUser.Mail);
        // DisplayName = BuildDisplayName(graphUser.Username),
        DisplayName = graphUser.Attributes.DisplayName;

        EmployeeId = graphUser.Attributes.EmployeeId;
        Department = graphUser.Attributes.Department;
        JobTitle = graphUser.Attributes.JobTitle;

        OfficeLocation = graphUser.Attributes.OfficeLocation;

        AppSettings ??= CreateDefaultSettings();

        if (CustomProperties is not null)
            CustomProperties.UpdateProperties(this, null);
        else
            CustomProperties = CreateDefaultProperties();

        return this;
    }

    // private static string BuildDisplayName(string username)
    // {
    //     var usernameParts = NormalizeUserName(username)
    //         .Split('.', StringSplitOptions.RemoveEmptyEntries);
    //     var uppercaseUsernameParts = usernameParts
    //         .Select(p => char.ToUpper(p[0]) + p.Substring(1))
    //         .ToArray();
    //     return string.Join(" ", uppercaseUsernameParts);
    // }

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
        return NormalizeEmail($"{NormalizeUserName(username)}@{domain}");
    }

    private static AuthServiceUserAppSettings CreateDefaultSettings() => new();

    private static AuthServiceUserAppSettings CreateSettings(UpdateUserSettingsDto dto) => new(dto);

    private static AuthServiceUserCustomProperties CreateDefaultProperties() => new();
}

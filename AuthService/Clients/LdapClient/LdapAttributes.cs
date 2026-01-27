namespace AuthService.Clients.LdapClient;

public sealed record LdapAttributes(
    string EmployeeId,
    string DisplayName,
    string Department,
    string JobTitle,
    string OfficeLocation
);

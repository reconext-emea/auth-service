namespace AuthService.Clients.GraphClient;

public sealed record GraphAttributes(
    string EmployeeId,
    string DisplayName,
    string Department,
    string JobTitle,
    string OfficeLocation
);

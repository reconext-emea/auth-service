namespace AuthService.Models.Dto.Users;

public sealed record UpdateEmployeeIdDto()
{
    public string EmployeeId { get; set; } = string.Empty;
}

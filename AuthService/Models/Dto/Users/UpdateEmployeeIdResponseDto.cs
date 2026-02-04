namespace AuthService.Models.Dto.Users;

public sealed record UpdateEmployeeIdResponseDto
{
    public string Message { get; } = "Employee Id (hrId) updated successfully.";
}

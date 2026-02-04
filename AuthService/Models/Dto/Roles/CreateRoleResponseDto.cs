namespace AuthService.Models.Dto.Roles;

public sealed record CreateRoleResponseDto
{
    public string Message { get; } = "Role (with claims and application) created successfully.";
}

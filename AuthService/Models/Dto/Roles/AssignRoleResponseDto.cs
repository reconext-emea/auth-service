namespace AuthService.Models.Dto.Roles;

public sealed record AssignRoleResponseDto
{
    public string Message { get; } = "Role (with claims and application) assigned successfully.";
}

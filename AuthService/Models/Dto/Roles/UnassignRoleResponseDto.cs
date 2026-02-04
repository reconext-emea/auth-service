namespace AuthService.Models.Dto.Roles;

public sealed record UnassignRoleResponseDto
{
    public string Message { get; } = "Role (with claims and application) unassigned successfully.";
}

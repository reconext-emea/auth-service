namespace AuthService.Models.Dto.Roles;

public sealed record DeleteRoleResponseDto
{
    public string Message { get; } = "Role (with claims and application) deleted successfully.";
}

namespace AuthService.Models.Dto.Roles;

public class UnassignRoleResponseDto
{
    public string User { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Message { get; set; } = "Role removed successfully.";
}

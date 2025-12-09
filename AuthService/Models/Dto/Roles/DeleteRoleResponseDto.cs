namespace AuthService.Models.Dto.Roles;

public class DeleteRoleResponseDto
{
    public string Role { get; set; } = null!;
    public string Message { get; set; } = "Role deleted successfully.";
}

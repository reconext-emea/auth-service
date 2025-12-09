namespace AuthService.Models.Dto.Roles;

public class CreateRoleResponseDto
{
    public string Role { get; set; } = null!;
    public List<string> RoleClaims { get; set; } = null!;
}

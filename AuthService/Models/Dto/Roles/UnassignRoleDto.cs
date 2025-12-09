namespace AuthService.Models.Dto.Roles;

public class UnassignRoleDto
{
    public string UserIdentifier { get; set; } = null!;
    public string RoleIdentifier { get; set; } = null!;
}

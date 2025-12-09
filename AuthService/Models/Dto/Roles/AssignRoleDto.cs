namespace AuthService.Models.Dto.Roles;

public class AssignRoleDto
{
    public string UserIdentifier { get; set; } = null!;
    public string RoleIdentifier { get; set; } = null!;
}

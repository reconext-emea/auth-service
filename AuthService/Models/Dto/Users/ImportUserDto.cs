using AuthService.Models.Dto.Roles;

namespace AuthService.Models.Dto.Users;

public sealed record ImportUserDto(string Username, IReadOnlyList<CreateRoleDto> Roles);

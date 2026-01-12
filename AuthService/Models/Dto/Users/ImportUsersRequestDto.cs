namespace AuthService.Models.Dto.Users;

public sealed record ImportUsersRequestDto(IReadOnlyList<ImportUserDto> Users);

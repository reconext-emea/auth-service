namespace AuthService.Models.Dto.Users;

public sealed record GetUsersResponseDto(List<AuthServiceUserDto> Users);

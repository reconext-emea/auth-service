namespace AuthService.Models.Dto.Users;

public class GetUsersResponseDto
{
    public List<AuthServiceUserDto> Users { get; set; } = null!;
}

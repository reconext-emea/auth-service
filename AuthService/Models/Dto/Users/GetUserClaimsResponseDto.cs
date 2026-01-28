namespace AuthService.Models.Dto.Users;

public class GetUserClaimsResponseDto
{
    public List<string> UserClaims { get; set; } = [];

    public List<string> RoleClaims { get; set; } = [];
}

namespace AuthService.Models.Dto.Users;

public class GetUserClaimsResponseDto
{
    public string UserIdentifier { get; set; } = null!;

    public List<string> UserClaims { get; set; } = null!;

    public List<string> RoleClaims { get; set; } = null!;
}

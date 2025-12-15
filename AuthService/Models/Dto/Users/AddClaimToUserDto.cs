namespace AuthService.Models.Dto.Users;

public class AddClaimToUserDto
{
    public string Tool { get; set; } = null!;
    public string Privilege { get; set; } = null!;
}

namespace AuthService.Models.Dto.Users;

public class DeleteClaimFromUserResponseDto
{
    public string UserIdentifier { get; set; } = null!;
    public string UserClaim { get; set; } = null!;
    public string Message { get; set; } = "Claim removed successfully.";
}

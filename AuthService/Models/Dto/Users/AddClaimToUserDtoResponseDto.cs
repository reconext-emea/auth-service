namespace AuthService.Models.Dto.Users;

public class AddClaimToUserDtoResponseDto
{
    public string UserIdentifier { get; set; } = null!;
    public string Claim { get; set; } = null!;
    public string Message { get; set; } = "Claim added successfully.";
}

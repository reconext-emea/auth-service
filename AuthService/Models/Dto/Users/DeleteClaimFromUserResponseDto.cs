namespace AuthService.Models.Dto.Users;

public sealed record DeleteClaimFromUserResponseDto
{
    public string Message { get; } = "User claim removed successfully.";
}

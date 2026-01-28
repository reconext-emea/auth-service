namespace AuthService.Models.Dto.Users;

public sealed record AddClaimToUserDtoResponseDto
{
    public string Message { get; } = "User claim added successfully.";
}

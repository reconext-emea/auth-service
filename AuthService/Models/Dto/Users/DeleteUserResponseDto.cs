namespace AuthService.Models.Dto.Users;

public sealed record DeleteUserResponseDto
{
    public string Message { get; } = "User deleted successfully.";
}

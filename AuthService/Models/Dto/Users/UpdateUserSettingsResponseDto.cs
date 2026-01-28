namespace AuthService.Models.Dto.Users;

public sealed record UpdateUserSettingsResponseDto
{
    public string Message { get; } = "Settings updated successfully.";
}

namespace AuthService.Models.Dto.Users;

public sealed record UpdateUserPropertiesResponseDto
{
    public string Message { get; } = "Properties updated successfully.";
}

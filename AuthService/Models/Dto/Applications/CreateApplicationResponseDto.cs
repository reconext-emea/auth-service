namespace AuthService.Models.Dto.Applications;

public sealed record CreateApplicationResponseDto
{
    public string Message { get; } = "Application created successfully.";
}

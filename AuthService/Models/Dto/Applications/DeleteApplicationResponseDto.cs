namespace AuthService.Models.Dto.Applications;

public sealed record DeleteApplicationResponseDto
{
    public string Message { get; } = "Application deleted successfully.";
}

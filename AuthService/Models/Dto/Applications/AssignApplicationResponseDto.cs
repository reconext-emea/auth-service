namespace AuthService.Models.Dto.Applications;

public sealed record AssignApplicationResponseDto
{
    public string Message { get; } = "Application assigned successfully.";
}

namespace AuthService.Models.Dto.Applications;

public sealed record UnassignApplicationResponseDto
{
    public string Message { get; } = "Application unassigned successfully.";
}

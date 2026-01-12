namespace AuthService.Models.Dto.Errors;

public class ErrorResponseDto
{
    public string Error { get; set; } = null!;
    public string? Details { get; set; }
}

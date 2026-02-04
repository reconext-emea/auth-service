namespace AuthService.Models.Dto.Errors;

public class ErrorResponseDto
{
    public string Error { get; set; } = string.Empty;
    public string? Details { get; set; } = null;

    public ErrorResponseDto() { }

    public ErrorResponseDto(string error, string? details = null)
    {
        Error = error;
        Details = details;
    }
}

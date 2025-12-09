namespace AuthService.Models.Dto.Errors;

public class AuthErrorRequestDto
{
    public string Reference { get; set; } = default!;
    public string ErrorDetails { get; set; } = default!;
}

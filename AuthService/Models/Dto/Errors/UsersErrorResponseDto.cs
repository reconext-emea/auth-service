namespace AuthService.Models.Dto.Errors;

public class UsersErrorResponseDto
{
    public string Error { get; set; } = null!;
    public string? Details { get; set; }
}

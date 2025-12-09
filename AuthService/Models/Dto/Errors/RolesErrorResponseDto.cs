namespace AuthService.Models.Dto.Errors;

public class RolesErrorResponseDto
{
    public string Error { get; set; } = null!;
    public string? Details { get; set; }
}

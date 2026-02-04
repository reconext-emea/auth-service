namespace AuthService.Models.Dto.Applications;

public sealed record CreateApplicationDto(string ClientId, string? DisplayName);

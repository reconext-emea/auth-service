namespace AuthService.Models.Dto.Applications;

public sealed record AssignApplicationDto(
    string UserIdentifier,
    string ApplicationIdentifier // Id OR ClientId
);

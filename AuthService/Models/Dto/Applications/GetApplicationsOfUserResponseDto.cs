namespace AuthService.Models.Dto.Applications;

public sealed record GetApplicationsOfUserResponseDto(IReadOnlyList<ApplicationDto> Applications);

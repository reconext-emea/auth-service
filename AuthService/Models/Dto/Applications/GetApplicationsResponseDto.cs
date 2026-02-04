namespace AuthService.Models.Dto.Applications;

public sealed record GetApplicationsResponseDto(IReadOnlyList<ApplicationDto> Applications);

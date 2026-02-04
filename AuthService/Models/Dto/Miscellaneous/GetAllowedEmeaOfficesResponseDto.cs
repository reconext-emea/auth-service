namespace AuthService.Models.Dto.Miscellaneous;

public sealed record GetAllowedEmeaOfficesResponseDto(IReadOnlyList<string> Offices);

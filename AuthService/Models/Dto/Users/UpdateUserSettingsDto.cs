using AuthService.Constants;

namespace AuthService.Models.Dto.Users;

public sealed record UpdateUserSettingsDto
{
    public string PreferredLanguageCode { get; init; } = PreferredLanguage.English;
    public string PreferredColorThemeCode { get; init; } = PreferredColorTheme.Light;
}

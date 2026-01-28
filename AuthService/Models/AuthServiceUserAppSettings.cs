using AuthService.Constants;
using AuthService.Models.Dto.Users;

namespace AuthService.Models;

public class AuthServiceUserAppSettings
{
    public string Id { get; set; } = null!;

    /// <summary>
    /// ISO 639-1 Language Code
    /// </summary>
    public string PreferredLanguageCode { get; set; } = PreferredLanguage.English;
    public string PreferredColorThemeCode { get; set; } = PreferredColorTheme.Light;

    public AuthServiceUser User { get; set; } = null!;

    public AuthServiceUserAppSettings() { }

    public AuthServiceUserAppSettings(UpdateUserSettingsDto dto)
    {
        PreferredLanguageCode = dto.PreferredLanguageCode;
        PreferredColorThemeCode = dto.PreferredColorThemeCode;
    }
}

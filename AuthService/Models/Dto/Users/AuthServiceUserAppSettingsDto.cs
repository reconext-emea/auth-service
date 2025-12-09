namespace AuthService.Models.Dto.Users;

public class AuthServiceUserSettingsDto(AuthServiceUserAppSettings appSettings)
{
    public string PreferredLanguageCode { get; set; } = appSettings.PreferredLanguageCode;
    public string ColorThemeCode { get; set; } = appSettings.ColorThemeCode;
}

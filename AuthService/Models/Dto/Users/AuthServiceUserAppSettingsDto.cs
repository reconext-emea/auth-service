namespace AuthService.Models.Dto.Users;

public sealed record AuthServiceUserSettingsDto(
    string PreferredLanguageCode,
    string PreferredColorThemeCode
)
{
    public static AuthServiceUserSettingsDto From(AuthServiceUserAppSettings s) =>
        new(s.PreferredLanguageCode, s.PreferredColorThemeCode);
}

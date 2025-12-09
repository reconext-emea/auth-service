namespace AuthService.Models.Dto.Users;

public class UpdateUserSettingsDto
{
    public string PreferredLanguageCode { get; set; } = null!;
    public string ColorThemeCode { get; set; } = null!;
}

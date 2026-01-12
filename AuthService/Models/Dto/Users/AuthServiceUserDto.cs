namespace AuthService.Models.Dto.Users;

public class AuthServiceUserDto
{
    public string Id { get; private set; }
    public string UserName { get; private set; }
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public string OfficeLocation { get; private set; }
    public AuthServiceUserSettingsDto? AppSettings { get; private set; }
    public AuthServiceUserCustomPropertiesDto? CustomProperties { get; private set; }

    public AuthServiceUserDto(AuthServiceUser user)
    {
        Id = user.Id;
        UserName = user.UserName!;
        Email = user.Email!;
        DisplayName = user.DisplayName;
        OfficeLocation = user.OfficeLocation;

        if (user.AppSettings != null)
        {
            AppSettings = new AuthServiceUserSettingsDto(user.AppSettings);
        }

        if (user.CustomProperties != null)
        {
            CustomProperties = new AuthServiceUserCustomPropertiesDto(user.CustomProperties);
        }
    }
}

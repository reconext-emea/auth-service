namespace AuthService.Models.Dto.Users;

public sealed record AuthServiceUserDto(
    string Id,
    string UserName,
    string Email,
    string DisplayName,
    string OfficeLocation,
    string EmployeeId,
    string Department,
    string JobTitle,
    AuthServiceUserSettingsDto AppSettings,
    AuthServiceUserCustomPropertiesDto CustomProperties
)
{
    public static AuthServiceUserDto From(AuthServiceUser user) =>
        new(
            user.Id,
            user.UserName!,
            user.Email!,
            user.DisplayName,
            user.OfficeLocation,
            user.EmployeeId,
            user.Department,
            user.JobTitle,
            AuthServiceUserSettingsDto.From(user.AppSettings),
            AuthServiceUserCustomPropertiesDto.From(user.CustomProperties)
        );
}

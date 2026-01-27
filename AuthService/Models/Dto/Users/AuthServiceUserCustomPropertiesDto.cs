namespace AuthService.Models.Dto.Users;

public class AuthServiceUserCustomPropertiesDto(AuthServiceUserCustomProperties customProperties)
{
    public string Confidentiality { get; set; } = customProperties.Confidentiality;
    public string Region { get; set; } = customProperties.Region;
}

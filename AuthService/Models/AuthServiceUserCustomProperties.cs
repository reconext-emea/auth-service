using AuthService.Constants;
using AuthService.Models.Dto.Users;

namespace AuthService.Models;

public class AuthServiceUserCustomProperties
{
    public string Id { get; set; } = null!;

    /// <summary>
    /// Determined by Petr Koukal
    /// </summary>
    public string Confidentiality { get; set; } = ConfidentialityClass.Class1;
    public string Region { get; set; } = string.Empty;

    public AuthServiceUser User { get; set; } = null!;

    public AuthServiceUserCustomProperties() { }

    public AuthServiceUserCustomProperties(AuthServiceUser user, UpdateUserPropertiesDto? dto)
    {
        Region = OfficeLocationToRegionAdapter.GetRegionOfOfficeLocation(user.OfficeLocation);

        if (!string.IsNullOrWhiteSpace(dto?.Confidentiality))
            Confidentiality = dto.Confidentiality;
    }

    public void UpdateProperties(AuthServiceUser user, UpdateUserPropertiesDto? dto)
    {
        Region = OfficeLocationToRegionAdapter.GetRegionOfOfficeLocation(user.OfficeLocation);

        if (!string.IsNullOrWhiteSpace(dto?.Confidentiality))
            Confidentiality = dto.Confidentiality;
    }
}

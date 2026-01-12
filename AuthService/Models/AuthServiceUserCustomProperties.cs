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

    public AuthServiceUser User { get; set; } = null!;

    public AuthServiceUserCustomProperties() { }

    public AuthServiceUserCustomProperties(UpdateUserPropertiesDto dto)
    {
        Confidentiality = dto.Confidentiality;
    }
}

using AuthService.Clients.LdapClient;

namespace AuthService.Constants;

public class OfficeLocationToRegionAdapter(LdapConfig ldapConfig)
{
    private readonly LdapConfig _ldapConfig = ldapConfig;

    public string GetRegionOfOfficeLocation(string officeLocation)
    {
        if (_ldapConfig.AllowedEmeaOfficeNames.Contains(officeLocation))
            return Region.Emea;

        return string.Empty;
    }
}

using AuthService.Clients.LdapClient;
using Microsoft.Extensions.Configuration;

namespace AuthService.Tests.Helpers.Ldap;

public static class FakeLdapConfig
{
    public static LdapConfig Create(
        string? allowedOffices =
            "Bydgoszcz Site (PL)::Havant Site (UK)::Prague Site (CZ)::REMOTE / HOME OFFICE::Tallinn Site (EE)::Zoetermeer Site (NL)",
        string techUser = "BYD-Intranet",
        string techDomain = "reconext.com",
        string techPassword = "secret"
    )
    {
        var values = new Dictionary<string, string?>
        {
            ["Ldap:AllowedEmeaOfficeNames"] = allowedOffices,
            ["Ldap:TechnicalUsername"] = techUser,
            ["Ldap:TechnicalDomain"] = techDomain,
            ["Ldap:TechnicalPassword"] = techPassword,
        };

        return new LdapConfig(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }
}

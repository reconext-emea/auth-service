using Novell.Directory.Ldap;

namespace AuthService.Clients.LdapClient;

public class LdapClient(LdapConfig config, ILogger<LdapClient> logger) : ILdapClient
{
    private readonly LdapConfig _config = config;
    private readonly ILogger<LdapClient> _logger = logger;

    public async Task<LdapAuthenticateAsyncResult> AuthenticateAsync(UserPassport passport)
    {
        if (!_config.AllowedDomains.TryGetValue(passport.Domain, out var baseDn))
            return new(false, LdapError.DomainNotAllowed);

        TechnicalUser tu = _config.TechnicalUser;

        string host = tu.GetHost();
        string bindDn = tu.GetBindDn();
        string bindPassword = tu.GetBindPassword();
        string searchFilter = tu.GetSearchFilter(passport.Username);

        try
        {
            using var connection = new LdapConnection { SecureSocketLayer = false };

            await connection.ConnectAsync(host, LdapConnection.DefaultPort);
            await connection.BindAsync(bindDn, bindPassword);

            ILdapSearchResults results = await connection.SearchAsync(
                baseDn,
                LdapConnection.ScopeSub,
                searchFilter,
                null,
                false
            );

            if (!await results.HasMoreAsync())
                return new(false, LdapError.UserNotFound);

            LdapEntry user = await results.NextAsync();

            string? officeName = user.GetStringValueOrDefault("physicalDeliveryOfficeName");

            if (
                string.IsNullOrWhiteSpace(officeName)
                || !_config.AllowedEmeaOfficeNames.Contains(officeName)
            )
            {
                return new(false, LdapError.OfficeNotAllowed);
            }

            await connection.BindAsync(user.Dn, passport.Password);

            var ldapUser = new LdapUser(passport, officeName);

            return new(true, null, ldapUser);
        }
        catch (LdapException ex)
        {
            if (ex.ResultCode == LdapException.InvalidCredentials)
                return new(false, LdapError.InvalidCredentials);

            return new(false, LdapError.ServerError);
        }
    }
}

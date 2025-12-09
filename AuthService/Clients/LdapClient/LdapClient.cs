using Novell.Directory.Ldap;

namespace AuthService.Clients.LdapClient;

public class LdapClient : ILdapClient
{
    private readonly LdapConfig _config;
    private readonly Func<ILdapConnection> _connectionFactory;

    public LdapClient(LdapConfig config)
    {
        _config = config;
        _connectionFactory = () => new LdapConnection { SecureSocketLayer = false };
    }

    public LdapClient(LdapConfig config, Func<ILdapConnection> connectionFactory)
    {
        _config = config;
        _connectionFactory = connectionFactory;
    }

    public async Task<LdapAuthenticateAsyncResult> AuthenticateAsync(UserPassport passport)
    {
        TechnicalUser tu = _config.TechnicalUser;

        string host = tu.GetHost();
        string bindDn = tu.GetBindDn();
        string bindPassword = tu.GetBindPassword();
        string searchFilter = tu.GetSearchFilter(passport.Username);

        try
        {
            using ILdapConnection connection = _connectionFactory();

            await connection.ConnectAsync(host, LdapConnection.DefaultPort);
            await connection.BindAsync(bindDn, bindPassword);

            ILdapSearchResults results = await connection.SearchAsync(
                string.Join(",", passport.Domain.Split('.').Select(p => $"dc={p}")),
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

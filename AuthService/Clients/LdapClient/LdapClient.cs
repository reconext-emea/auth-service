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

            var attributes = user.GetAttributeSet();

            var ldapAttributes = new LdapAttributes(
                EmployeeId: user.GetStringValueOrDefault("employeeID") ?? string.Empty,
                DisplayName: user.GetStringValueOrDefault("displayName") ?? string.Empty,
                Department: user.GetStringValueOrDefault("department") ?? string.Empty,
                JobTitle: user.GetStringValueOrDefault("title") ?? string.Empty,
                OfficeLocation: user.GetStringValueOrDefault("physicalDeliveryOfficeName")
                    ?? string.Empty
            );

            if (
                string.IsNullOrWhiteSpace(ldapAttributes.OfficeLocation)
                || !_config.AllowedEmeaOfficeNames.Contains(ldapAttributes.OfficeLocation)
            )
            {
                return new(false, LdapError.OfficeNotAllowed);
            }

            await connection.BindAsync(user.Dn, passport.Password);

            var ldapUser = new LdapUser(passport, ldapAttributes);

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

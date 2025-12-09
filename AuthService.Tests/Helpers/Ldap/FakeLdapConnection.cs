using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Sasl;

namespace AuthService.Tests.Helpers.Ldap;

public class FakeLdapConnection : ILdapConnection
{
    public string? UsedHost { get; private set; }
    public int UsedPort { get; private set; }
    public string? UsedBindDn { get; private set; }
    public string? UsedBindPassword { get; private set; }
    public string? UsedSearchFilter { get; private set; }

    public LdapEntry? SearchEntry { get; set; }
    public bool ThrowInvalidCredentialsOnUserBind { get; set; }

    public bool Bound => throw new NotImplementedException();

    public bool Connected => throw new NotImplementedException();

    public LdapSearchConstraints SearchConstraints => throw new NotImplementedException();

    public DebugId DebugId => throw new NotImplementedException();

    public void Dispose() { }

    public Task StartTlsAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task StopTlsAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(LdapEntry entry, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(LdapEntry entry, LdapConstraints cons, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task BindAsync(string dn, string passwd, CancellationToken ct = default)
    {
        if (ThrowInvalidCredentialsOnUserBind && UsedBindDn != null)
            throw new LdapException(
                messageOrKey: "Invalid credentials",
                resultCode: LdapException.InvalidCredentials,
                serverMsg: null
            );
        ;

        UsedBindDn = dn;
        UsedBindPassword = passwd;
        return Task.CompletedTask;
    }

    public Task BindAsync(int version, string dn, string passwd, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task BindAsync(
        string dn,
        string passwd,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task BindAsync(
        int version,
        string dn,
        string passwd,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task BindAsync(int version, string dn, byte[] passwd, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task BindAsync(
        int version,
        string dn,
        byte[] passwd,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task BindAsync(SaslRequest saslRequest, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<ISaslClientFactory> GetRegisteredSaslClientFactories()
    {
        throw new NotImplementedException();
    }

    public void RegisterSaslClientFactory(ISaslClientFactory saslClientFactory)
    {
        throw new NotImplementedException();
    }

    public bool IsSaslMechanismSupported(string mechanism)
    {
        throw new NotImplementedException();
    }

    public Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        UsedHost = host;
        UsedPort = port;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string dn, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string dn, LdapConstraints cons, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void Disconnect()
    {
        throw new NotImplementedException();
    }

    public Task<LdapExtendedResponse> ExtendedOperationAsync(
        LdapExtendedOperation op,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<LdapExtendedResponse> ExtendedOperationAsync(
        LdapExtendedOperation op,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task ModifyAsync(string dn, LdapModification mod, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task ModifyAsync(
        string dn,
        LdapModification mod,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task ModifyAsync(string dn, LdapModification[] mods, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task ModifyAsync(
        string dn,
        LdapModification[] mods,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<LdapEntry> ReadAsync(string dn, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<LdapEntry> ReadAsync(
        string dn,
        LdapSearchConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<LdapEntry> ReadAsync(string dn, string[] attrs, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<LdapEntry> ReadAsync(
        string dn,
        string[] attrs,
        LdapSearchConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task RenameAsync(
        string dn,
        string newRdn,
        bool deleteOldRdn,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task RenameAsync(
        string dn,
        string newRdn,
        bool deleteOldRdn,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task RenameAsync(
        string dn,
        string newRdn,
        string newParentdn,
        bool deleteOldRdn,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task RenameAsync(
        string dn,
        string newRdn,
        string newParentdn,
        bool deleteOldRdn,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<ILdapSearchResults> SearchAsync(
        string @base,
        int scope,
        string filter,
        string[] attrs,
        bool typesOnly,
        CancellationToken ct = default
    )
    {
        UsedSearchFilter = filter;

        // Return empty results to avoid further logic
        return Task.FromResult<ILdapSearchResults>(new FakeLdapSearchResults(SearchEntry));
    }

    public Task<ILdapSearchResults> SearchAsync(
        string @base,
        int scope,
        string filter,
        string[] attrs,
        bool typesOnly,
        LdapSearchConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<bool> CompareAsync(string dn, LdapAttribute attr, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CompareAsync(
        string dn,
        LdapAttribute attr,
        LdapConstraints cons,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }
}

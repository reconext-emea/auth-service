using Novell.Directory.Ldap;

namespace AuthService.Tests.Helpers.Ldap;

public class FakeLdapSearchResults : ILdapSearchResults
{
    private readonly LdapEntry? _entry;

    public FakeLdapSearchResults()
    {
        _entry = null;
    }

    public FakeLdapSearchResults(LdapEntry? entry)
    {
        _entry = entry;
    }

    public LdapControl[] ResponseControls => throw new NotImplementedException();

    public Task<LdapEntry> NextAsync() => throw new NotImplementedException();

    public void Dispose() { }

    public Task<bool> HasMoreAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_entry != null);
    }

    public Task<LdapEntry> NextAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_entry!);
    }

    public IAsyncEnumerator<LdapEntry> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }
}

using Microsoft.Kiota.Abstractions.Authentication;

namespace AuthService.Clients.GraphClient;

public class StaticTokenProvider(string token) : IAccessTokenProvider
{
    private readonly string _token = token;

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(_token);
    }
}

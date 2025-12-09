using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AuthService.Clients.GraphClient;

public class GraphClientFactory
{
    public GraphServiceClient InitializeFromAcquiredGraphToken(string graphToken)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new StaticTokenProvider(graphToken)
        );

        var graphClient = new GraphServiceClient(authProvider);

        return graphClient;
    }
}

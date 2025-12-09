using Microsoft.Graph;

namespace AuthService.Clients.GraphClient;

public interface IGraphClientFactory
{
    public GraphServiceClient InitializeGraphClient();
}

using System.Security.Claims;

namespace AuthService.Clients.EntraIdClient;

public record EntraIdAuthenticateAsyncResult(
    bool Success,
    EntraIdError? ErrorCode = null,
    string? ErrorDescription = null,
    ClaimsPrincipal? Principal = null
);

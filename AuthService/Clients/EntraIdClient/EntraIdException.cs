namespace AuthService.Clients.EntraIdClient;

public class EntraIdException(EntraIdError error) : Exception()
{
    public EntraIdError Error { get; } = error;
    public string Description { get; } = GetDescription(error);

    private static string GetDescription(EntraIdError error) =>
        error switch
        {
            EntraIdError.MissingToken => "Access token was not provided.",
            EntraIdError.InvalidToken => "The access token is invalid.",
            EntraIdError.NotAccessToken => "The token is not an access token.",
            EntraIdError.SigningKeyFetchFailed => "Unable to retrieve signing keys.",
            EntraIdError.MissingPrincipal =>
                "Unable to extract claims principal from access token.",
            EntraIdError.GraphRequestFailed =>
                "Unable to communicate with Microsoft Graph. The access token may not include Graph permissions or the service is unreachable.",
            EntraIdError.MissingGraphUser =>
                "Unable to retrieve user information from Microsoft Graph.",
            EntraIdError.MissingGraphUserMail =>
                "Microsoft Graph did not provide a mail or user principal name for the user.",
            EntraIdError.InvalidGraphUserMailFormat =>
                "The user's email address format is not compatible with the local identity system.",
            EntraIdError.MissingGraphUserOfficeLocation =>
                "Microsoft Graph did not provide an office location for the user.",
            EntraIdError.UserCreationFailed => "Failed to create local user account.",
            _ => "An Entra ID authentication error occurred.",
        };
}

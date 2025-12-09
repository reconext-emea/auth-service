namespace AuthService.Clients.EntraIdClient;

public enum EntraIdError
{
    MissingToken,
    InvalidToken,
    NotAccessToken,
    SigningKeyFetchFailed,
    MissingPrincipal,
    GraphRequestFailed,
    MissingGraphUser,
    MissingGraphUserMail,
    InvalidGraphUserMailFormat,
    MissingGraphUserOfficeLocation,
    UserCreationFailed,
}

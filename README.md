# auth-service

Authentication • Identity Management • Authorization • Token Issuing

The Auth Service acts as the central identity provider for Byd and Prg Intranet applications.
It supports multiple authentication methods, manages user accounts and roles, and issues
standards-compliant security tokens used across the platform.

It provides:

- LDAP (Active Directory) authentication

- Microsoft Entra ID (Azure AD) authentication

- User and identity management (ASP.NET Core Identity)

- Role- and permission-based authorization

- Token issuing and refreshing (OpenIddict)

## Frontend Types

To install the TypeScript types exposed by this service, run:

```bash
npm i github:reconext-emea/auth-service
```

After installation, a scoped package will appear under: `node_modules/@reconext/`.

You can then import AuthService namespace in your frontend code:

```ts
import { AuthService } from "@reconext/auth-service-frontend-types";
```

> **Client**  
> The namespace exposes several important TypeScript types and utilities.
> Most importantly, it provides the BydIntranetClient class - the primary client used by frontend applications to communicate with this Auth Service.

## Authorization Endpoints

> **Environment**  
> When the environment variable  
> `ASPNETCORE_ENVIRONMENT=Production`  
> is set, the service **does not allow HTTP traffic** — only **HTTPS requests are accepted**.  
> HTTP requests will be rejected.  
> In `Development`, both HTTP is permitted.

All authentication methods are handled behind `/connect/token` endpoint.

Regardless of which authentication provider is used, the endpoint always returns the same set of issued tokens:

- **Access Token**
- **ID Token**
- **Refresh Token**

### Token Types Explained

**Access Token** (access_token) is used to call your backend APIs.

- **Can be decoded**;
- **Short-lived**;
- **Contains claims required by your APIs**, with more fields planned for future expansion (sub_roles);
- **Must be sent as** `Authorization: Bearer <token>` when calling APIs.

**ID Token** (id_token) is used by frontend only to identify the logged-in user.

- **Can be decoded**;
- **Short-lived**;
- **Contains profile information**, with more fields planned for future expansion;
- **Never sent to backend APIs**, meant for frontend use only.

**Refresh Token** (refresh_token) is used to obtain new access/id tokens.

- **Cannot be decoded** (opaque by design);
- **Long-lived** compared to access/id tokens;
- **Sent only to** `/connect/token` using `grant_type=refresh_token`.
- **Allows silent re-authentication** without asking the user to log in again

### Architecture Overview (LDAP)

Even when using LDAP authentication, the client must still be initialized with MSAL configuration.

More details about MSAL configuration requirements can be found in the
**Architecture Overview (Microsoft Entra ID)** section below.

#### Example usage:

```ts
import { AuthService } from "@reconext/auth-service-frontend-types";

async function loginWithLdap(): Promise<void> {
  //
  // Reset any previous error UI state
  //
  errorShallowRef.value = null;
  errorShallowRefReference.value = null;

  //
  // Initialize the AuthService client
  //
  // For development (localhost):
  // const client = AuthService.BydIntranetClient
  //   .create<AuthService.Environment.Development>(null);
  //
  // For production:
  const client = AuthService.BydIntranetClient.create<AuthService.Environment.Production>({
    redirectUri: "https://bydintranet.reconext.com/",
    postLogoutRedirectUri: "https://bydintranet.reconext.com/",
  });

  try {
    //
    // Perform login
    //
    const response: AuthService.IConnectTokenResponse = await client.ldapLogin(
      username.value,
      password.value
    );

    //
    // Handle AuthError, e.g. rejected credentials
    //
    if (client.isAuthError(response)) {
      // Save the error in the AuthService database
      errorShallowRefReference.value = await client.saveErrorAsync(response);

      // Display the reason to the user
      errorShallowRef.value = response;
      return; // Stop here; login failed
    }

    //
    // Login succeeded — redirect the user
    //
    // e.g. router.push("/dashboard")
  } catch (error: unknown) {
    //
    // Handle unexpected system errors
    //
    errorShallowRefReference.value = await client.saveErrorAsync(error);
  }
}
```

#### 400 Bad Responses

This call never throws authentication errors.

All authentication + validation failures are returned as JSON error body.

The response body always follows the OAuth 2.0 error format:

```json
{
  "error": "error_code",
  "error_description": "Explanation of the error."
}
```

Possible OAuth2 error bodies:

```json
{
  "error": "invalid_request",
  "error_description": "Username is required."
}
```

```json
{
  "error": "invalid_request",
  "error_description": "Password is required."
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Invalid username or password."
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Your office location is not authorized to access the system."
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "User not found in LDAP directory."
}
```

```json
{
  "error": "server_error",
  "error_description": "Unexpected LDAP error occurred."
}
```

### Architecture Overview (Microsoft Entra ID)

To use the Byd Intranet app with Entra ID, a small configuration step is required. In the Azure portal under “Byd Intranet → Authentication (Preview)”, any new URL that initiates the Microsoft login flow must be added as a Redirect URI.

When the redirectUri is set during the MSAL login process, Azure validates whether it matches one of the registered Redirect URIs. If it does, Azure will redirect back to that URI and allow the application to acquire the authentication response.

Additional details about valid MSAL configuration values can be found in
AuthService.MsalAuthConfig<T extends AuthService.Environment.Development | AuthService.Environment.Production>
(see the next Architecture section).

After the frontend completes Microsoft Entra ID login and obtains a Microsoft access token
(e.g., through a popup flow), it can exchange that token for service-issued tokens by
calling the same `/connect/token` endpoint:

#### Example usage:

```ts
import { AuthService } from "@reconext/auth-service-frontend-types";

async function loginWithMicrosoft(): Promise<void> {
  //
  // Reset any previous error UI state
  //
  errorShallowRef.value = null;
  errorShallowRefReference.value = null;

  //
  // Initialize the AuthService client
  //
  // For development (localhost):
  // const client = AuthService.BydIntranetClient
  //   .create<AuthService.Environment.Development>(null);
  //
  // For production:
  const client = AuthService.BydIntranetClient.create<AuthService.Environment.Production>({
    redirectUri: "https://prgintranet.reconext.com/",
    postLogoutRedirectUri: "https://prgintranet.reconext.com/",
  });

  try {
    //
    // Perform login
    //
    const response: AuthService.IConnectTokenResponse = await client.msalLogin();
    //
    // Handle AuthError, e.g. rejected credentials
    //
    if (client.isAuthError(response)) {
      // Save the error in the AuthService database
      errorShallowRefReference.value = await client.saveErrorAsync(response);

      // Display the reason to the user
      errorShallowRef.value = response;
      return; // Stop here; login failed
    }

    //
    // Login succeeded — redirect the user
    //
    // e.g. router.push("/dashboard")
  } catch (error: unknown) {
    //
    // Handle unexpected system errors
    //
    errorShallowRefReference.value = await client.saveErrorAsync(error);
  }
}
```

#### 400 Bad Responses

This call never throws authentication errors.

All authentication + validation failures are returned as JSON error body.

The response body always follows the OAuth 2.0 error format:

```json
{
  "error": "error_code",
  "error_description": "Explanation of the error."
}
```

Possible OAuth2 error bodies:

```json
{
  "error": "invalid_grant",
  "error_description": "Access token was not provided. (MissingToken)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "The access token is invalid. (InvalidToken)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "The token is not an access token. (NotAccessToken)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Unable to retrieve signing keys. (SigningKeyFetchFailed)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Unable to extract claims principal from access token. (MissingPrincipal)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Unable to communicate with Microsoft Graph. The access token may not include Graph permissions or the service is unreachable. (GraphRequestFailed)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Unable to retrieve user information from Microsoft Graph. (MissingGraphUser)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Microsoft Graph did not provide a mail or user principal name for the user. (MissingGraphUserMail)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "The user's email address format is not compatible with the local identity system. (InvalidGraphUserMailFormat)"
}
```

```json
{
  "error": "invalid_grant",
  "error_description": "Microsoft Graph did not provide an office location for the user. (MissingGraphUserOfficeLocation)"
}
```

```json
{
  "error": "server_error",
  "error_description": "Failed to create local user account. (UserCreationFailed)"
}
```

### Architecture Overview (Refresh Token)

Access tokens are short-lived by design.
When an access token expires, the frontend can request new tokens without requiring the user to log in again.

This is done by sending the existing refresh_token to the same `/connect/token` endpoint using the refresh_token grant.

#### Example usage:

```ts
import { AuthService } from "@reconext/auth-service-frontend-types";

try {
  //
  // Perform refresh
  //
  const response: AuthService.IConnectTokenResponse = await client.refreshTokenAsync(refreshToken);

  //
  // Handle AuthError, e.g. refresh token may be expired or invalid
  //
  if (client.isAuthError(response)) {
    // Save the error in the AuthService database
    await client.saveErrorAsync(response);

    // Refresh failed — redirect the user
    // e.g. router.push("/login")
    return;
  }

  //
  // Refresh succeeded — update stored tokens
  //
} catch (error) {
  //
  // Handle unexpected system errors
  //
  await client.saveErrorAsync(error);
}
```

#### 400 Bad Responses

> Refresh token error cases were not tested.
>
> The following error scenarios are based on OAuth 2.0 / OpenIddict specifications
> and expected behavior of the refresh token grant.

This call never throws authentication errors.

All authentication + validation failures are returned as JSON error body.

The response body always follows the OAuth 2.0 error format:

```json
{
  "error": "error_code",
  "error_description": "Explanation of the error."
}
```

Possible OAuth2 error bodies:

- invalid_grant

Returned when:

- refresh token is expired

- refresh token is revoked

- refresh token does not exist

- refresh token is malformed

- refresh token does not belong to a valid session

- the token was issued for a different client

### Success 200 OK Responses

When authentication succeeds, the `/connect/token` endpoint always returns:

**Interface:** `AuthService.IConnectTokenResponse`

```json
{
  "access_token": "XXXXX",
  "id_token": "XXXXX",
  "refresh_token": "XXXXX",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

Both access_token and id_token can be decoded client-side.

```ts
// const response = ...

const decodedAccessToken = AuthService.BydIntranetClient.decodeJwt(response.access_token);
const decodedIdToken = AuthService.BydIntranetClient.decodeJwt(response.id_token);

// console.log(decodedAccessToken.sub);
// console.log(decodedIdToken.email);
```

## Best Practices

To ensure secure and seamless authentication, the frontend should follow a set of recommended patterns when working with access tokens, refresh tokens, and API calls.

### Use a Shared Axios Instance with Request Interceptors

Create a single Axios instance and configure it to automatically:

- attach the `Authorization: Bearer <access_token>` header
- refresh the token when the access token expires
- retry the original request after refreshing

Example:

```ts
import axios from "axios";
import { AuthService } from "@reconext/auth-service-frontend-types";

const jwtAxios = axios.create();

// Attach access token before each request
jwtAxios.interceptors.request.use(
  (config) => {
    const authorizationStore = useAuthorizationStore();
    const accessToken = authorizationStore.getAccessToken();

    if (accessToken) {
      config.headers = config.headers || {};
      config.headers.Authorization = `Bearer ${accessToken}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Automatically refresh on 401 responses
jwtAxios.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status !== 401) return Promise.reject(error);

    try {
      const authorizationStore = useAuthorizationStore();
      const client = authorizationStore.getClient();
      const refreshToken = authorizationStore.getRefreshToken();

      // No refresh token → go to login
      if (!refreshToken) {
        router.push("/login");
        return Promise.reject(error);
      }

      // Attempt refresh
      const response = await client.refreshTokenAsync(refreshToken);

      if (client.isAuthError(response)) {
        // Refresh token invalid → user must log in again
        await client.saveErrorAsync(response);
        authorizationStore.clear();
        router.push("/login");
        return Promise.reject(error);
      }

      // Update stored tokens
      authorizationStore.newConnectTokenResponse(response);

      // Retry original request with updated access token
      const accessToken = authorizationStore.getAccessToken();
      error.config.headers.Authorization = `Bearer ${accessToken}`;
      return jwtAxios.request(error.config);
    } catch (ex) {
      // Unable to refresh → force login
      useAuthorizationStore().clear();
      router.push("/login");
      return Promise.reject(ex);
    }
  }
);

export default jwtAxios;
```

## Tests

Before building the project with:

```bash
docker compose up -d --build
```

you must execute the test suite and confirm the project builds successfully.

To run the tests, execute:

```bash
dotnet test ./AuthService.Tests
```

## Entity Framework Core

This project uses **EF Core 8** with **PostgreSQL** for managing Identity and OpenIddict data.
Below are the **essential EF Core commands** used during development.

### CLI

Install EF Core (required once)

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

Update EF Core

```bash
dotnet tool update --global dotnet-ef
```

### Create a new migration

```bash
dotnet ef migrations add <Name>
```

### Apply migrations to the database

```bash
dotnet ef database update
```

### List all migrations

```bash
dotnet ef migrations list
```

# auth-service

Authentication • Identity • Token Issuing

This service provides two authentication providers:

- **LDAP (Active Directory)**;
- **Microsoft Entra ID (Azure AD)**.

User identities are stored and managed through **ASP.NET Core Identity**, while **OpenIddict** issues standards-compliant tokens.

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

## Frontend Types

To install the TypeScript types exposed by this service, run:

```bash
npm i github:reconext-emea/auth-service
```

After installation, a scoped package will appear under: `node_modules/@reconext/`.

You can then import types in your frontend code:

```ts
import type { IConnectTokenResponse } from "@reconext/auth-service-frontend-types";
```

## Authorization Endpoints

> **Environment Note:**  
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

### Architecture Overview (LDAP)

#### Example usage:

```ts
import axios from "axios";
import type { IConnectTokenResponse } from "@reconext/auth-service-frontend-types";

const response = await axios
  .create({
    //  baseURL: `https://10.41.0.85:5081`,
    baseURL: `http://localhost:5081`,
  })
  .post<IConnectTokenResponse>(
    `connect/token`,
    new URLSearchParams({
      grant_type: "password",
      username: username.value,
      password: password.value,
      domain: domain.value,
      scope: "openid profile email api offline_access",
    }),
    {
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
    }
  );
```

#### Error 400 Bad Responses

OpenIddict wraps all authentication/validation failures in an HTTP `400 Bad Request` response.
The response body always follows the OAuth 2.0 error format:

```json
{
  "error": "error_code",
  "error_description": "Explanation of the error."
}
```

Exhaustive List of Error Responses:

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

To use the Byd Intranet app with Entra ID, a small configuration step is required. Under “Byd Intranet | Authentication (Preview)”, every time a new URL is introduced as the source of the Microsoft login, it must be added as a Redirect URI.

When the redirectUri is set during the MSAL login process, Azure validates whether it matches one of the registered Redirect URIs. If it does, Azure will redirect back to that URI and allow the application to acquire the authentication response.

After the frontend completes Microsoft Entra ID login and obtains a Microsoft access token
(e.g., through a popup flow), it can exchange that token for service-issued tokens by
calling the same `/connect/token` endpoint:

#### Example usage:

```ts
import axios from "axios";
import type { IConnectTokenResponse } from "@reconext/auth-service-frontend-types";

const response = await axios
  .create({
    //  baseURL: `https://10.41.0.85:5081`,
    baseURL: `http://localhost:5081`,
  })
  .post<IConnectTokenResponse>(
    `connect/token`,
    new URLSearchParams({
      grant_type: "urn:entra:access_token",
      access_token: microsoftAccessToken,
      scope: "openid profile email api offline_access",
    }),
    {
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
    }
  );
```

### Success 200 OK Responses

When authentication succeeds, the `/connect/token` endpoint always returns:

```json
{
  "access_token": "XXXXX",
  "id_token": "XXXXX",
  "refresh_token": "XXXXX",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

Both access_token and id_token can be decoded client-side if needed.

```ts
import { jwtDecode } from "jwt-decode";

// const response = ...

const decodedAccessToken = jwtDecode(response.data.access_token);
const decodedIdToken = jwtDecode(response.data.id_token);

// console.log(decodedAccessToken.sub);
// console.log(decodedIdToken.email);
```

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

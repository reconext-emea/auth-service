# auth-service

Authentication • Identity • Token Issuing

This service provides two authentication providers:

- **LDAP (Active Directory)**;
- **Microsoft Entra ID (Azure AD)**.

User identities are stored and managed through **ASP.NET Core Identity**, while **OpenIddict** issues standards-compliant tokens.

## Architecture Overview (LDAP)

This service issues JWT tokens after successful LDAP authentication.

A single request to `/connect/token` returns three important tokens:

- **Access Token**
- **ID Token**
- **Refresh Token**

These tokens contain user identity information and allow secure access to backend APIs.

### Endpoint

Example axios request:

```ts
const response = await axios
  .create({
    baseURL: "https://bydintranet.reconext.com:5081",
  })
  .post(
    "connect/token",
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

Expected JSON response:

```json
{
  "access_token": "XXXXX",
  "id_token": "XXXXX",
  "refresh_token": "XXXXX",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

Rejected response:

OpenIddict always wraps errors inside a `400 Bad Request` HTTP response

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
  "error": "invalid_request",
  "error_description": "Domain is required."
}
```

```json
{
  "error": "invalid_request",
  "error_description": "The specified domain is not allowed."
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

#### Token Types Explained

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

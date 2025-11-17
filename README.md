# auth-service

Identity & Authentication Microservice.

This project implements a modern authentication architecture using **ASP.NET Core**, **OpenIddict**, and **PostgreSQL**, with support for external identity providers such as **LDAP** and **Microsoft Entra ID (Azure AD)**.

## Architecture Overview

                        +-------------------------------+
                        |       External Providers      |
                        |      (Microsoft, LDAP/AD)     |
                        +---------------+---------------+
                                        |
                                        |  OIDC / OAuth2
                                        v
                        +---------------+---------------+
                        |     Identity Service (C#)     |
                        |-------------------------------|
                        | - ASP.NET Core Web API        |
                        | - OpenIddict (OIDC Server)    |
                        | - ASP.NET Identity            |
                        | - Local accounts              |
                        | - LDAP, Microsoft Entra ID    |
                        | - Issues JWT + Refresh tokens |
                        +---------------+---------------+
                                        ^
                                        |
                        +---------------+---------------+
                        |            Frontend           |
                        | - SPA                         |
                        | - Web                         |
                        +-------------------------------+

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

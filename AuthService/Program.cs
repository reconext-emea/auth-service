using AuthService.Clients.LdapClient;
using AuthService.Data;
using AuthService.Models;
using AuthService.Services.Token;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

var services = builder.Services;

services.AddSingleton<LdapConfig>();
services.AddScoped<LdapClient>();
services.AddScoped<ITokenService, TokenService>();

// ---------- DB ----------
services.AddDbContext<AuthServiceDbContext>(options =>
{
    string? connStr = config.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connStr))
    {
        throw new Exception("DefaultConnection connection string is missing.");
    }

    options.UseNpgsql(connStr);
    options.UseOpenIddict();
});

// ---------- Identity ----------
services
    .AddIdentity<AuthServiceUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true; //  Gets or sets a flag indicating whether the application requires unique emails for its users. Defaults to false.
        options.Lockout.AllowedForNewUsers = false; // Gets or sets a flag indicating whether a new user can be locked out. Defaults to true.
    })
    .AddEntityFrameworkStores<AuthServiceDbContext>()
    .AddDefaultTokenProviders();

// ---------- OpenIddict (token server) ----------
services
    .AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<AuthServiceDbContext>();
    })
    .AddServer(options =>
    {
        // Enable the flows (OAuth2/OpenID Connect flows) you want to support.
        // Authorization Code Flow is the standard browser-based login flow.
        // PKCE (Proof Key for Code Exchange) secures public clients like SPAs and mobile apps.
        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

        // Allows issuing refresh tokens, enabling clients to stay logged in
        // without re-entering credentials.
        options.AllowRefreshTokenFlow();

        // Registers the authorization endpoint URI.
        // This endpoint is used for redirect-based user login flows.
        options.SetAuthorizationEndpointUris("connect/authorize");

        // Registers the token endpoint URI.
        // This endpoint is used to exchange authorization codes or refresh tokens
        // for access tokens and ID tokens.
        options.SetTokenEndpointUris("connect/token");

        // Declares the list of supported scopes.
        // openid  -> enables OpenID Connect and ID tokens
        // email   -> allows sending email claim in tokens
        // profile -> allows sending profile claims (name, etc.)
        // api     -> your custom API access scope
        options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile, "api");

        if (builder.Environment.IsDevelopment())
        {
            // Adds development certificates for token signing and encryption.
            // These are auto-generated per environment and only for local dev.
            options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        }
        else
        {
            // PRODUCTION: use real certificates
            // options.AddEncryptionCertificate(...);
            // options.AddSigningCertificate(...);
        }

        // Integrates OpenIddict with ASP.NET Core.
        // Enables passthrough so your controllers can customize responses
        // instead of OpenIddict always handling them automatically.
        // - EnableAuthorizationEndpointPassthrough():
        //      Lets your own code run during /connect/authorize requests.
        // - EnableTokenEndpointPassthrough():
        //      Allows issuing tokens via SignIn(principal, scheme) inside controllers.
        options
            .UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// ---------- Auth / API ----------
services.AddAuthentication();
services.AddAuthorization();

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthServiceDbContext>();
    db.Database.Migrate();
}

// ---------- Middleware ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

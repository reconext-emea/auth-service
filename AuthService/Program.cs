using AuthService.Clients.LdapClient;
using AuthService.Data;
using AuthService.Models;
using AuthService.Services.OpenIddict;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

var services = builder.Services;

services.AddSingleton<LdapConfig>();
services.AddScoped<LdapClient>();
services.AddScoped<PasswordGrantHandler>();

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
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();

        options.SetTokenEndpointUris("/connect/token");

        options.AcceptAnonymousClients();

        options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile, "api");

        options.DisableAccessTokenEncryption();

        if (builder.Environment.IsDevelopment())
        {
            options.UseAspNetCore().DisableTransportSecurityRequirement(); // for HTTP localhost
            // .EnableTokenEndpointPassthrough();

            options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        }
        else
        {
            options.UseAspNetCore(); // .EnableTokenEndpointPassthrough();

            // options.AddEncryptionCertificate(...);
            // options.AddSigningCertificate(...);
        }

        options.AddEventHandler<OpenIddictServerEvents.HandleTokenRequestContext>(builder =>
        {
            builder.UseScopedHandler<PasswordGrantHandler>();
        });
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

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

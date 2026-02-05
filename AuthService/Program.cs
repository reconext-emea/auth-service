using System.Security.Cryptography.X509Certificates;
using AuthService.Clients.EntraIdClient;
using AuthService.Clients.LdapClient;
using AuthService.Constants;
using AuthService.Data;
using AuthService.Models;
using AuthService.Services.OpenIddict;
using AuthService.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

var services = builder.Services;

services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return new LdapConfig(cfg);
});

services.AddSingleton<OfficeLocationToRegionAdapter>();

services.AddScoped<ILdapClient>(sp =>
{
    var ldapConfig = sp.GetRequiredService<LdapConfig>();
    return new LdapClient(ldapConfig);
});

services.AddScoped<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
services.AddScoped<PasswordGrantHandler>();

services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return new EntraIdConfig(cfg);
});

services.AddScoped<IEntraIdClient>(sp =>
{
    var cfg = sp.GetRequiredService<EntraIdConfig>();
    var logger = sp.GetRequiredService<ILogger<EntraIdClient>>();
    return new EntraIdClient(cfg, logger);
});

services.AddScoped<EntraTokenGrantHandler>();

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

services.AddScoped<IUserService, UserService>();

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
        options.AllowCustomFlow("urn:entra:access_token");

        options.SetTokenEndpointUris("/connect/token");

        options.AcceptAnonymousClients();

        options.RegisterScopes(Scopes.OpenId, Scopes.OfflineAccess);

        options.RegisterClaims([
            "username",
            "email",
            "office_location",
            "display_username",
            Claims.Role,
            "permission",
            "app_settings",
        ]);

        options.DisableAccessTokenEncryption();

        if (builder.Environment.IsDevelopment())
        {
            options.UseAspNetCore().DisableTransportSecurityRequirement();

            options.AddDevelopmentEncryptionCertificate();
            options.AddDevelopmentSigningCertificate();
        }
        else
        {
            options.UseAspNetCore();

            var signingBase64 = config.GetSection("SigningCert")["Base64"];
            var signingPassword = config.GetSection("SigningCert")["Password"];

            var encryptBase64 = config.GetSection("EncryptCert")["Base64"];
            var encryptPassword = config.GetSection("EncryptCert")["Password"];

            if (string.IsNullOrWhiteSpace(signingBase64))
                throw new InvalidOperationException("Signing certificate Base64 is missing.");

            if (string.IsNullOrWhiteSpace(signingPassword))
                throw new InvalidOperationException("Signing certificate password is missing.");

            if (string.IsNullOrWhiteSpace(encryptBase64))
                throw new InvalidOperationException("Encryption certificate Base64 is missing.");

            if (string.IsNullOrWhiteSpace(encryptPassword))
                throw new InvalidOperationException("Encryption certificate password is missing.");

            options.AddSigningCertificate(
                new X509Certificate2(Convert.FromBase64String(signingBase64), signingPassword)
            );

            options.AddEncryptionCertificate(
                new X509Certificate2(Convert.FromBase64String(encryptBase64), encryptPassword)
            );
        }

        options.AddEventHandler<OpenIddictServerEvents.HandleTokenRequestContext>(builder =>
        {
            builder.UseScopedHandler<PasswordGrantHandler>();
        });

        options.AddEventHandler<OpenIddictServerEvents.HandleTokenRequestContext>(builder =>
        {
            builder.UseScopedHandler<EntraTokenGrantHandler>();
        });
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// ---------- Auth / API ----------
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddict
        .Validation
        .AspNetCore
        .OpenIddictValidationAspNetCoreDefaults
        .AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddict
        .Validation
        .AspNetCore
        .OpenIddictValidationAspNetCoreDefaults
        .AuthenticationScheme;
});

services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // In dev: allow everything
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
    }
    else
    {
        // In non-dev: require auth everywhere by default
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
});

services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();

    options.SwaggerDoc("roles", new OpenApiInfo { Title = "Roles API", Version = "v1" });
    options.SwaggerDoc("users", new OpenApiInfo { Title = "Users API", Version = "v1" });
    options.SwaggerDoc(
        "applications",
        new OpenApiInfo { Title = "Applications API", Version = "v1" }
    );
    options.SwaggerDoc(
        "miscellaneous",
        new OpenApiInfo { Title = "Miscellaneous API", Version = "v1" }
    );
});

services.AddCors(options =>
{
    string[] allowedOrigins =
        config
            .GetSection("Cors")["AllowedOrigins"]
            ?.Split("::", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? [];

    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// app.Use(
//     async (context, next) =>
//     {
//         var logger = context
//             .RequestServices.GetRequiredService<ILoggerFactory>()
//             .CreateLogger("SwaggerAuth");

//         if (context.Request.Path.StartsWithSegments("/swagger"))
//         {
//             string? authHeader = context.Request.Headers.Authorization;

//             if (
//                 authHeader is null
//                 || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
//             )
//             {
//                 context.Response.StatusCode = 401;
//                 context.Response.Headers.WWWAuthenticate =
//                     $"Basic realm=\"Swagger-{Guid.NewGuid()}\"";
//                 context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
//                 context.Response.Headers.Pragma = "no-cache";
//                 context.Response.Headers.Expires = "0";
//                 return;
//             }

//             var encoded = authHeader["Basic ".Length..].Trim();
//             var bytes = Convert.FromBase64String(encoded);
//             var credentials = System.Text.Encoding.UTF8.GetString(bytes).Split(':', 2);

//             var username = credentials[0];
//             var password = credentials[1];

//             string[] allowedUsernames =
//                 config
//                     .GetSection("Swagger")["AllowedUsernames"]
//                     ?.Split(
//                         "::",
//                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
//                     )
//                 ?? [];

//             if (!allowedUsernames.Contains(username))
//             {
//                 logger.LogWarning(
//                     $"Swagger access denied: user '{username}' is not allowed.",
//                     username
//                 );
//                 context.Response.StatusCode = 401;

//                 return;
//             }

//             var ldap = context.RequestServices.GetRequiredService<ILdapClient>();
//             var passport = new UserPassport(username, "reconext.com", password);
//             var result = await ldap.AuthenticateAsync(passport);

//             if (!result.Success)
//             {
//                 logger.LogWarning("Swagger LDAP auth failed for user.");
//                 context.Response.StatusCode = 401;

//                 return;
//             }
//         }

//         await next();
//     }
// );

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/roles/swagger.json", "Roles API");
    options.SwaggerEndpoint("/swagger/users/swagger.json", "Users API");
    options.SwaggerEndpoint("/swagger/applications/swagger.json", "Applications API");
    options.SwaggerEndpoint("/swagger/miscellaneous/swagger.json", "Miscellaneous API");
});

app.MapControllers();

app.Run();

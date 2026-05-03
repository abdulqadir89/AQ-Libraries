using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Handlers;
using AQ.Identity.OpenIddict.Health;
using AQ.Identity.OpenIddict.KeyManagement;
using AQ.Identity.OpenIddict.Middleware;
using AQ.Identity.OpenIddict.Seeding;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AQ.Identity.OpenIddict.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAqIdentity<TContext>(
        this IServiceCollection services,
        AqIdentityOptions options,
        IReadOnlyList<IdentityClientConfig> clients)
        where TContext : DbContext, IIdentityDbContext
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(o => o.LoginPath = "/auth/login");

        services.Configure<IdentityOptions>(identityOptions =>
        {
            identityOptions.Password.RequiredLength = options.Password.MinLength;
            identityOptions.Password.RequireDigit = options.Password.RequireDigit;
            identityOptions.Password.RequireUppercase = options.Password.RequireUppercase;
            identityOptions.Password.RequireNonAlphanumeric = options.Password.RequireNonAlphanumeric;

            identityOptions.Lockout.MaxFailedAccessAttempts = options.Lockout.MaxFailedAttempts;
            identityOptions.Lockout.DefaultLockoutTimeSpan = options.Lockout.LockoutDuration;
        });

        services.AddOpenIddict()
            .AddCore(coreOptions =>
            {
                coreOptions
                    .UseEntityFrameworkCore()
                    .UseDbContext<TContext>();
            })
            .AddServer(serverOptions =>
            {
                serverOptions.SetIssuer(new Uri(options.Issuer, UriKind.Absolute));
                serverOptions.AllowAuthorizationCodeFlow();
                serverOptions.AllowRefreshTokenFlow();
                serverOptions.SetAccessTokenLifetime(options.Tokens.AccessToken);
                serverOptions.SetRefreshTokenLifetime(options.Tokens.RefreshToken);

                serverOptions.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess, "manage_api", "els_api");

                serverOptions.AcceptAnonymousClients();
                serverOptions.DisableAccessTokenEncryption();

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    serverOptions.AddDevelopmentEncryptionCertificate();
                    serverOptions.AddDevelopmentSigningCertificate();
                }
                else
                {
                    serverOptions.AddDevelopmentEncryptionCertificate();
                    serverOptions.AddDevelopmentSigningCertificate();
                }

                serverOptions.SetAuthorizationEndpointUris("/connect/authorize");
                serverOptions.SetTokenEndpointUris("/connect/token");
                serverOptions.SetUserInfoEndpointUris("/connect/userinfo");
                serverOptions.SetEndSessionEndpointUris("/connect/logout");

                serverOptions.UseAspNetCore()
                    .DisableTransportSecurityRequirement()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            })
            .AddValidation(validationOptions =>
            {
                validationOptions.UseLocalServer();
                validationOptions.UseAspNetCore();

                if (options.Google != null)
                {
                    services.AddAuthentication()
                        .AddGoogle(o =>
                        {
                            o.ClientId = options.Google.ClientId;
                            o.ClientSecret = options.Google.ClientSecret;
                        });
                }

                // Reject tokens for inactive users or invalidated SecurityStamp
                validationOptions.AddEventHandler<OpenIddictValidationEvents.ValidateTokenContext>(builder =>
                    builder.UseInlineHandler(async context =>
                    {
                        var sub = context.Principal?.FindFirst(Claims.Subject)?.Value;
                        if (sub == null || !Guid.TryParse(sub, out var userId)) return;

                        var serviceProvider = context.Transaction.Properties["service_provider"] as IServiceProvider
                            ?? services.BuildServiceProvider();
                        using var scope = serviceProvider.CreateScope();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await userManager.FindByIdAsync(userId.ToString());

                        if (user is { IsActive: false })
                        {
                            context.Reject(Errors.InvalidToken, "Account is disabled.");
                            return;
                        }

                        // Reject if SecurityStamp has changed (forced re-auth after security events)
                        var tokenStamp = context.Principal?.FindFirst("stamp")?.Value;
                        if (tokenStamp != null && user?.SecurityStamp != null && tokenStamp != user.SecurityStamp)
                        {
                            context.Reject(Errors.InvalidToken, "Token has been invalidated.");
                        }
                    })
                    .SetOrder(int.MaxValue - 100));
            });

        services.AddScoped<IOpenIddictServerHandler<OpenIddictServerEvents.ProcessSignInContext>, ClaimsEnrichmentHandler>();

        // Ensure the handler's IIdentityDbContext dependency is resolvable as TContext
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<TContext>());

        // Register ManageApi policy
        services.AddAuthorization(opts =>
        {
            opts.AddPolicy("ManageApi", policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(Claims.Private.Scope, "manage_api"));
        });

        services.AddSingleton<IReadOnlyList<IdentityClientConfig>>(clients);
        services.AddSingleton<SigningKeyManager>();
        services.AddHostedService<ClientSeeder>(sp => new ClientSeeder(
            sp.GetRequiredService<IOpenIddictApplicationManager>(),
            sp.GetRequiredService<IOpenIddictScopeManager>(),
            sp.GetRequiredService<ILogger<ClientSeeder>>(),
            clients));

        if (options.Google != null)
        {
            services.AddAuthentication()
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = options.Google.ClientId;
                    googleOptions.ClientSecret = options.Google.ClientSecret;
                });
        }

        services.AddHealthChecks()
            .AddCheck<IdentityDbHealthCheck<TContext>>("identity_db", tags: ["live"])
            .AddCheck<IdentityMigrationHealthCheck<TContext>>("identity_migrations", tags: ["ready"]);

        return services;
    }

    public static IApplicationBuilder UseAqIdentity(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static IEndpointRouteBuilder MapAqIdentityHealthChecks(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        });

        return endpoints;
    }
}

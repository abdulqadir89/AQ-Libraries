using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Handlers;
using AQ.Identity.OpenIddict.KeyManagement;
using AQ.Identity.OpenIddict.Seeding;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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

                serverOptions.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess, "manage_api");

                serverOptions.AcceptAnonymousClients();
                serverOptions.DisableAccessTokenEncryption();

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    serverOptions.AddDevelopmentEncryptionCertificate();
                    serverOptions.AddDevelopmentSigningCertificate();
                }
                else
                {
                    var signingKeyManager = services.BuildServiceProvider().GetRequiredService<SigningKeyManager>();
                    var activeKey = signingKeyManager.GetActiveSigningKey();
                    serverOptions.AddSigningKey(activeKey.ToSecurityKey());

                    foreach (var validationKey in signingKeyManager.GetValidationKeys())
                    {
                        if (validationKey.Id != activeKey.Id)
                        {
                            serverOptions.AddSigningKey(validationKey.ToSecurityKey());
                        }
                    }
                }

                serverOptions.UseAspNetCore();
            })
            .AddValidation(validationOptions =>
            {
                validationOptions.UseLocalServer();
                validationOptions.UseAspNetCore();

                // Register inline handler to reject tokens for inactive users
                validationOptions.AddEventHandler<OpenIddictValidationEvents.ValidateTokenContext>(builder =>
                    builder.UseInlineHandler(async context =>
                    {
                        var sub = context.Principal?.FindFirst(Claims.Subject)?.Value;
                        if (sub == null || !Guid.TryParse(sub, out var userId)) return;

                        // Resolve scoped services from the DI container
                        var serviceProvider = context.Transaction.Properties["service_provider"] as IServiceProvider
                            ?? services.BuildServiceProvider();
                        using var scope = serviceProvider.CreateScope();
                        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await userManager.FindByIdAsync(userId.ToString());
                        if (user is { IsActive: false })
                        {
                            context.Reject(Errors.InvalidToken, "Account is disabled.");
                        }
                    })
                    .SetOrder(int.MaxValue - 100));
            });

        services.AddScoped<IOpenIddictServerHandler<OpenIddictServerEvents.ProcessSignInContext>, ClaimsEnrichmentHandler>();

        // Register ManageApi policy
        services.AddAuthorization(opts =>
        {
            opts.AddPolicy("ManageApi", policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(Claims.Private.Scope, "manage_api"));
        });

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

        return services;
    }

    public static IApplicationBuilder UseAqIdentity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}

using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using AQ.Identity.Core.Configuration.Validation;
using AQ.Identity.Core.Entities;
using AQ.Identity.OpenIddict.Extensions.Claims;
using AQ.Identity.OpenIddict.Extensions.Middleware;
using AQ.Identity.OpenIddict.Extensions.Seeding;
using AQ.Identity.OpenIddict.Handlers;
using AQ.Identity.OpenIddict.Health;
using AQ.Identity.OpenIddict.KeyManagement;
using AQ.Identity.OpenIddict.Middleware;
using AQ.Identity.OpenIddict.Seeding;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation;
using System.Threading.RateLimiting;
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
        // The UI project's Razor Pages bind several genuinely-optional fields (e.g.
        // PostLogoutUrisRaw, ServiceAccountClaimsRaw) to non-nullable `string` properties
        // under the host's <Nullable>enable</Nullable> setting. Without this, ASP.NET Core's
        // MVC model binding applies an implicit [Required] to every non-nullable
        // reference-typed bound property, and rejects an empty string as "missing" — even
        // though string.Empty is a valid, non-null value — causing those forms to silently
        // fail ModelState validation with no rendered error. This is the officially
        // documented opt-out (see the MvcOptions.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes
        // docs) rather than a workaround.
        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(o =>
            o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<AqIdentityOptions>, AqIdentityOptionsValidator>();
        services.AddOptions<AqIdentityOptions>()
            .Configure(o =>
            {
                o.Issuer = options.Issuer;
                o.AppName = options.AppName;
                o.Tokens = options.Tokens;
                o.Password = options.Password;
                o.Lockout = options.Lockout;
                o.Keys = options.Keys;
                o.Hsts = options.Hsts;
                o.Email = options.Email;
                o.Google = options.Google;
                o.AdminUser = options.AdminUser;
            })
            .ValidateOnStart();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<StoredClaimsPrincipalFactory<TContext>>();

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
                serverOptions.AllowClientCredentialsFlow();
                serverOptions.SetAccessTokenLifetime(options.Tokens.AccessToken);
                serverOptions.SetRefreshTokenLifetime(options.Tokens.RefreshToken);

                serverOptions.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess);

                serverOptions.AcceptAnonymousClients();
                serverOptions.DisableAccessTokenEncryption();

                // "plain" PKCE provides no real protection over no PKCE at all — only
                // advertise/accept S256.
                serverOptions.Configure(o =>
                {
                    o.CodeChallengeMethods.Clear();
                    o.CodeChallengeMethods.Add(CodeChallengeMethods.Sha256);
                });

                // Signing/encryption credentials are supplied by SigningCredentialsConfigurator,
                // which sources them from the persisted, rotating keys in SigningKeyManager
                // (see below) instead of ephemeral per-process dev certificates.

                serverOptions.SetAuthorizationEndpointUris("/connect/authorize");
                serverOptions.SetTokenEndpointUris("/connect/token");
                serverOptions.SetUserInfoEndpointUris("/connect/userinfo");
                serverOptions.SetEndSessionEndpointUris("/connect/logout");

                serverOptions.UseAspNetCore()
                    .DisableTransportSecurityRequirement()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
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
                        var sub = context.Principal?.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
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

        services.AddOpenIddict()
            .AddServer(serverOptions =>
            {
                serverOptions.AddEventHandler(
                    OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ProcessSignInContext>()
                        .UseScopedHandler<ClaimsEnrichmentHandler>()
                        .SetOrder(10_000 - 1)
                        .Build());

                // Must run before the built-in ValidateTokenEntry handler so the redeemed
                // refresh token's status can still be inspected before that handler rejects
                // the grant (see RefreshTokenReuseHandler for the full rationale).
                serverOptions.AddEventHandler(
                    OpenIddictServerHandlerDescriptor.CreateBuilder<OpenIddictServerEvents.ProcessAuthenticationContext>()
                        .UseScopedHandler<Handlers.RefreshTokenReuseHandler>()
                        .SetOrder(OpenIddictServerHandlers.Protection.ValidateTokenEntry.Descriptor.Order - 1)
                        .Build());
            });

        // Ensure the handler's IIdentityDbContext dependency is resolvable as TContext
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<TContext>());

        // Register ManageApi policy — checks plain claim so it works for both
        // cookie-authenticated Razor Pages and OpenIddict bearer token endpoints
        services.AddAuthorization(opts =>
        {
            opts.AddPolicy("ManageApi", policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireClaim("manage_api"));
        });

        services.AddSingleton<IReadOnlyList<IdentityClientConfig>>(clients);
        services.AddSingleton(options.Keys);
        services.AddScoped<SigningKeyManager>();
        services.AddScoped<ISigningKeyManager>(sp => sp.GetRequiredService<SigningKeyManager>());
        services.AddScoped<ISetupStateService, SetupStateService>();
        services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<OpenIddictServerOptions>, KeyManagement.SigningCredentialsConfigurator>();
        services.AddHostedService<KeyManagement.KeyRotationWorker>();

        if (options.Google != null)
        {
            services.AddAuthentication()
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = options.Google.ClientId;
                    googleOptions.ClientSecret = options.Google.ClientSecret;
                });
        }

        services.AddHsts(hstsOptions =>
        {
            hstsOptions.MaxAge = TimeSpan.FromDays(options.Hsts.MaxAgeDays);
            hstsOptions.IncludeSubDomains = options.Hsts.IncludeSubDomains;
            hstsOptions.Preload = options.Hsts.Preload;
        });

        services.AddHealthChecks()
            .AddCheck<IdentityDbHealthCheck<TContext>>("identity_db", tags: ["live"])
            .AddCheck<IdentityMigrationHealthCheck<TContext>>("identity_migrations", tags: ["ready"]);

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global limiter so it also covers /connect/token, which OpenIddict handles
            // via its own middleware passthrough rather than a mapped ASP.NET endpoint —
            // per-endpoint RequireRateLimiting() policies can't reach it.
            // Throttles credential-submitting paths (token exchange, login, MFA verify,
            // password reset) per client IP; everything else passes through unthrottled.
            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var path = httpContext.Request.Path;
                var isThrottledPath = httpContext.Request.Method == HttpMethods.Post &&
                    (path.StartsWithSegments("/connect/token") ||
                     path.StartsWithSegments("/auth/login") ||
                     path.StartsWithSegments("/auth/register") ||
                     path.StartsWithSegments("/auth/forgot-password") ||
                     path.StartsWithSegments("/auth/mfa"));

                if (!isThrottledPath)
                {
                    return RateLimitPartition.GetNoLimiter("unthrottled");
                }

                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseAqIdentity(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment())
        {
            // Standard ASP.NET Core template guidance: HSTS is skipped in Development since it
            // forces HTTPS for the configured max-age, which breaks plain-HTTP local dev.
            app.UseHsts();
        }

        app.UseMiddleware<RequestIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<SetupGuardMiddleware>();
        app.UseRateLimiter();
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

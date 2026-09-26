using System.Globalization;
using AQ.Identity.UI.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace AQ.Identity.UI.Localization;

public static class ServiceCollectionExtensions
{
    private static readonly CultureInfo[] SupportedCultures =
    [
        CultureInfo.GetCultureInfo("en"),
        CultureInfo.GetCultureInfo("zh-CN"),
        CultureInfo.GetCultureInfo("zh-TW"),
        CultureInfo.GetCultureInfo("zh-HK"),
    ];

    /// <summary>
    /// Wires up culture resolution and page/DataAnnotations localization for the Identity
    /// UI Razor Pages (login, register, MFA, account, apps) — see plan §8.a. Also registers
    /// <see cref="LocalizedIdentityErrorDescriber"/> so ASP.NET Core Identity's own error
    /// messages come back localized.
    /// </summary>
    public static IServiceCollection AddAqIdentityLocalization(this IServiceCollection services)
    {
        services.AddLocalization();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = SupportedCultures;
            options.SupportedUICultures = SupportedCultures;
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;

            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new UiLocalesRequestCultureProvider());
            options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
            options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
        });

        // Calling AddRazorPages() again on top of the host's own call is safe/idempotent —
        // ASP.NET Core just adds the localization pieces to the existing MVC builder.
        services.AddRazorPages()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(o =>
                o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(IdentityUIResource)));

        // The IdentityBuilder returned by AddIdentity/AddIdentityCore (in
        // AQ.Identity.OpenIddict.ServiceCollectionExtensions.AddAqIdentity) isn't reachable
        // here — that call lives in a different project and already ran by the time the
        // ELS Identity host calls this method. AddErrorDescriber<T>() on that builder is
        // sugar for exactly this registration, so it's replicated directly here instead.
        services.AddScoped<Microsoft.AspNetCore.Identity.IdentityErrorDescriber, LocalizedIdentityErrorDescriber>();

        return services;
    }
}

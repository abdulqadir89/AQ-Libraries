using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace AQ.Identity.UI.Localization;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies request localization, then persists the resolved culture to the
    /// <see cref="CookieRequestCultureProvider"/> cookie whenever it was resolved from
    /// <c>ui_locales</c> — so the language chosen when the login redirect started (e.g. from
    /// the ELS web app's locale) sticks across the whole Identity session, including
    /// register/forgot-password navigations that don't carry <c>ui_locales</c> themselves.
    /// </summary>
    public static IApplicationBuilder UseAqIdentityLocalization(this IApplicationBuilder app)
    {
        app.UseRequestLocalization();

        app.Use(async (context, next) =>
        {
            var feature = context.Features.Get<IRequestCultureFeature>();
            if (feature?.Provider is UiLocalesRequestCultureProvider)
            {
                var culture = feature.RequestCulture.UICulture;
                context.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions
                    {
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        SameSite = SameSiteMode.Lax,
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                    });
            }

            await next(context);
        });

        return app;
    }
}

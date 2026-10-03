using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AQ.Utilities.Email;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers email sending capability, binding <see cref="EmailOptions"/> via
    /// <see cref="IOptionsMonitor{TOptions}"/> against the given configuration section — so
    /// values sourced from a DB-backed configuration provider (e.g. an AppSettings table) are
    /// picked up live, without a restart.
    /// <para>
    /// Mail goes over SMTP whenever a <c>Host</c> is configured (so Development can deliver to a
    /// local catcher such as Mailpit). <see cref="ConsoleEmailService"/> is only used in
    /// Development when no host is configured.
    /// </para>
    /// </summary>
    public static IServiceCollection AddAqEmail(
        this IServiceCollection services,
        IConfiguration emailConfigSection,
        IHostEnvironment env)
    {
        services.Configure<EmailOptions>(emailConfigSection);

        var smtpConfigured = !string.IsNullOrWhiteSpace(emailConfigSection["Host"]);

        if (env.IsDevelopment() && !smtpConfigured)
        {
            services.AddTransient<IEmailService, ConsoleEmailService>();
        }
        else
        {
            services.AddTransient<IEmailService, SmtpEmailService>();
        }

        services.AddTransient<IEmailTemplateService, DefaultEmailTemplateService>();

        return services;
    }
}

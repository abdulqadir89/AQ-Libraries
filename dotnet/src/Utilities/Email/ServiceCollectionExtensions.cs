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
    /// </summary>
    public static IServiceCollection AddAqEmail(
        this IServiceCollection services,
        IConfiguration emailConfigSection,
        IHostEnvironment env)
    {
        services.Configure<EmailOptions>(emailConfigSection);

        if (env.IsDevelopment())
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

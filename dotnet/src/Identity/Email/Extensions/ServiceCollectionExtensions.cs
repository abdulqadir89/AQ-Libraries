using AQ.Identity.Core.Abstractions;
using AQ.Identity.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AQ.Identity.Email.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAqIdentityEmail(
        this IServiceCollection services,
        EmailOptions options,
        IHostEnvironment env)
    {
        services.AddSingleton(Options.Create(options));

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

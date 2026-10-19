using CoachPulse.Application.Interfaces;
using CoachPulse.Application.Settings;
using CoachPulse.Infrastructure.Payments;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CoachPulse.Infrastructure
{

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<ICurrentTenantService, CurrentTenantService>();
            services.AddScoped<IPasswordHasher, PasswordHasherService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IClientAccessService, ClientAccessService>();
            services.AddScoped<ISubscriptionActivationService, SubscriptionActivationService>();
            services.AddScoped<ISubscriptionStateMachine>(sp => (ISubscriptionStateMachine)sp.GetRequiredService<ISubscriptionActivationService>());
            var emailProvider = configuration["Email:Provider"] ?? "Dev";
            services.AddSingleton<IEmailDeliveryTransport>(provider => emailProvider.ToLowerInvariant() switch
            {
                "dev" => new DevelopmentEmailSender(provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DevelopmentEmailSender>>(), provider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()),
                "smtp" => new SmtpEmailSender(configuration, provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmtpEmailSender>>()),
                _ => throw new InvalidOperationException("Email:Provider must be either Dev or Smtp.")
            });
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddHttpClient<IPaymobClient, PaymobClient>();
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.AddDbContext<CoachPulseDbContext>(options =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"));
            });

            return services;
        }
    }
}

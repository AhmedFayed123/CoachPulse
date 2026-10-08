using CoachPulse.Application.Interfaces;
using CoachPulse.Application.Settings;
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

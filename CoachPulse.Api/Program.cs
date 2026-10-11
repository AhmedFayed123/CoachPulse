using CoachPulse.Api.Hubs;
using CoachPulse.Api.Middleware;
using CoachPulse.Application.Interfaces;
using CoachPulse.Infrastructure;
using CoachPulse.Infrastructure.BackgroundJobs;
using CoachPulse.Infrastructure.Data.Seed;
using CoachPulse.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

namespace CoachPulse.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            builder.Host.UseSerilog();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSignalR();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "CoachPulse API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "أدخل الـ JWT Token هنا مباشرة دون الحاجة لكتابة كلمة Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddInfrastructure(builder.Configuration);

            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing.");

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer is missing.");

            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT Audience is missing.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                        ClockSkew = TimeSpan.Zero
                    };

                    // دعم تمرير الـ Token عبر الـ Query String لمسار الـ ChatHub
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/hubs/chat"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // إعداد Hangfire Services
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

            builder.Services.AddHangfireServer();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SuperAdminOnly", policy =>
                {
                    policy.RequireRole("SuperAdmin");
                });

                options.AddPolicy("OwnerOnly", policy =>
                {
                    policy.RequireRole("Owner");
                });

                options.AddPolicy("CoachOnly", policy =>
                {
                    policy.RequireRole("Owner", "Staff");
                });

                options.AddPolicy("ClientOnly", policy =>
                {
                    policy.RequireRole("Client");
                });
            });

            var app = builder.Build();

            // إعداد Seeding والـ Recurring Jobs بعد بناء التطبيق
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var context = services.GetRequiredService<CoachPulseDbContext>();
                var passwordHasher = services.GetRequiredService<IPasswordHasher>();

                await DbSeeder.SeedAsync(context, passwordHasher);

                // جدولة المهمة الخلفية عبر Hangfire
                var recurringJobManager = services.GetRequiredService<IRecurringJobManager>();
                recurringJobManager.AddOrUpdate<SubscriptionCheckJob>(
                    "check-expiring-subscriptions",
                    job => job.CheckExpiringSubscriptionsAsync(),
                    Cron.Daily);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // تفعيل Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire");

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseMiddleware<TenantResolutionMiddleware>();

            app.UseAuthorization();

            app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}


//"email": "coach1@test.com",
//  "password": "Coach@12345"
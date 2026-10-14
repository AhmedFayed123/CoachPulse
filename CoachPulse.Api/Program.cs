using CoachPulse.Api.Hubs;
using CoachPulse.Api.BackgroundJobs;
using CoachPulse.Api.Middleware;
using CoachPulse.Api.Authorization;
using CoachPulse.Api.Infrastructure;
using CoachPulse.Application.Interfaces;
using CoachPulse.Infrastructure;
using CoachPulse.Infrastructure.BackgroundJobs;
using CoachPulse.Infrastructure.Data.Seed;
using CoachPulse.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;

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
            builder.Services.AddHttpClient("python-ai", client =>
                client.Timeout = TimeSpan.FromSeconds(3));
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter("paymob-webhook", limiter =>
                {
                    limiter.PermitLimit = 100;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                    limiter.AutoReplenishment = true;
                });
                options.AddPolicy("files-upload", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? context.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
                options.AddPolicy("auth-login", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        $"{context.Connection.RemoteIpAddress}|{context.Request.Headers["X-Login-Email"]}",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
                options.AddPolicy("auth-register", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1), QueueLimit = 0, AutoReplenishment = true }));
                options.AddPolicy("auth-forgot", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromHours(1), QueueLimit = 0, AutoReplenishment = true }));
            });

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
                                (path.StartsWithSegments("/hubs/chat") ||
                                 path.StartsWithSegments("/hubs/notifications")))
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
                recurringJobManager.AddOrUpdate<ExpireSubscriptionsJob>("expire-subscriptions-daily", job => job.RunAsync(CancellationToken.None), Cron.Daily);
                recurringJobManager.AddOrUpdate<SubscriptionExpiryReminderJob>("subscription-expiry-reminders-daily", job => job.RunAsync(CancellationToken.None), Cron.Daily);
                recurringJobManager.AddOrUpdate<PendingPaymentReminderJob>("pending-payment-reminders-six-hourly", job => job.RunAsync(CancellationToken.None), Cron.HourInterval(6));
                recurringJobManager.AddOrUpdate<PaymobReconciliationJob>("paymob-reconciliation-hourly", job => job.RunAsync(CancellationToken.None), Cron.Hourly);
                recurringJobManager.AddOrUpdate<CleanupOrphanFilesJob>(
                    "cleanup-orphan-files-weekly",
                    job => job.CleanupAsync(CancellationToken.None),
                    Cron.Weekly);
                recurringJobManager.AddOrUpdate<CleanupExpiredInvitationsJob>(
                    "cleanup-expired-invitations-daily",
                    job => job.CleanupAsync(CancellationToken.None),
                    Cron.Daily);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // تفعيل Hangfire Dashboard
            app.UseHttpsRedirection();

            app.UseMiddleware<RouteDeprecationMiddleware>();

            app.UseAuthentication();

            app.UseRateLimiter();

            app.UseMiddleware<TenantResolutionMiddleware>();

            app.UseAuthorization();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new SuperAdminHangfireAuthorizationFilter()]
            });

            app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }))
                .AllowAnonymous();

            app.MapGet("/readyz", async (
                CoachPulseDbContext db,
                IHttpClientFactory httpClientFactory,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var databaseReady = await db.Database.CanConnectAsync(cancellationToken);
                var pythonReady = false;
                var pythonBaseUrl = configuration["PythonAi:BaseUrl"];
                if (Uri.TryCreate(pythonBaseUrl, UriKind.Absolute, out var baseUri))
                {
                    try
                    {
                        using var response = await httpClientFactory.CreateClient("python-ai")
                            .GetAsync(new Uri(baseUri, "ready"), cancellationToken);
                        pythonReady = response.IsSuccessStatusCode;
                    }
                    catch (HttpRequestException) { pythonReady = false; }
                    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { pythonReady = false; }
                }

                var ready = databaseReady && pythonReady;
                return Results.Json(new
                {
                    status = ready ? "ready" : "not_ready",
                    checks = new { database = databaseReady, pythonAi = pythonReady }
                }, statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
            }).AllowAnonymous();


            app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
            app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}


//"email": "coach1@test.com",
//  "password": "Coach@12345"

using CoachPulse.Api.Hubs;
using CoachPulse.Api.BackgroundJobs;
using CoachPulse.Api.Middleware;
using CoachPulse.Api.Authorization;
using CoachPulse.Api.Infrastructure;
using CoachPulse.Api.Services;
using CoachPulse.Application.Interfaces;
using CoachPulse.Infrastructure;
using CoachPulse.Infrastructure.BackgroundJobs;
using CoachPulse.Infrastructure.Data.Seed;
using CoachPulse.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using Serilog;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using CoachPulse.Application.DTOs;

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

            builder.Services.AddControllers(options => options.Filters.Add<CoachPulse.Api.Infrastructure.ProblemDetailsResultFilter>())
                .AddJsonOptions(options => ApiJsonSerialization.AddStringEnums(options.JsonSerializerOptions));
            builder.Services.AddMemoryCache();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 64 * 1024)
                .AddJsonProtocol(options => ApiJsonSerialization.AddStringEnums(options.PayloadSerializerOptions));
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (!builder.Environment.IsDevelopment() && allowedOrigins.Any(origin => origin == "*" || origin.Contains('*')))
                throw new InvalidOperationException("Production CORS origins must be explicit origins; wildcard origins are not allowed.");
            if (!builder.Environment.IsDevelopment() && allowedOrigins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
                throw new InvalidOperationException("Each configured production CORS origin must be an absolute HTTP or HTTPS origin.");
            builder.Services.AddCors(options => options.AddPolicy("ConfiguredOrigins", policy =>
            {
                if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }));
            builder.Services.AddHttpClient("python-ai", client => client.Timeout = TimeSpan.FromSeconds(10));
            builder.Services.AddHttpClient("ai-generation", client => { client.BaseAddress = new Uri(builder.Configuration["Ai:BaseUrl"] ?? builder.Configuration["PythonAi:BaseUrl"] ?? "http://127.0.0.1:8000/"); client.Timeout = TimeSpan.FromSeconds(60); });
            builder.Services.AddHttpClient("ai-churn", client => { client.BaseAddress = new Uri(builder.Configuration["Ai:BaseUrl"] ?? builder.Configuration["PythonAi:BaseUrl"] ?? "http://127.0.0.1:8000/"); client.Timeout = TimeSpan.FromSeconds(10); });
            builder.Services.AddScoped<IAiClient, PythonAiClient>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
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
                        $"{context.Connection.RemoteIpAddress}|{context.Items["LoginRateLimitEmail"]}",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
                options.AddPolicy("auth-register", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1), QueueLimit = 0, AutoReplenishment = true }));
                options.AddPolicy("auth-forgot", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromHours(1), QueueLimit = 0, AutoReplenishment = true }));
                options.AddPolicy("public-slug", context =>
                    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
            });

            builder.Services.AddSwaggerGen(options =>
            {
                options.SupportNonNullableReferenceTypes();
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
            var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
            {
                Directory.CreateDirectory(dataProtectionKeysPath);
                builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
            }
            builder.Services.AddSingleton<DatabaseStartupState>();
            builder.Services.AddSingleton<DatabaseStartupInitializer>();
            builder.Services.AddScoped<IEmailSender, QueuedEmailSender>();
            builder.Services.AddScoped<CoachPulse.Api.Services.ChatMessageService>();
            builder.Services.AddScoped<ChurnScoringService>();
            builder.Services.AddScoped<IChurnFeatureBuilder, ChurnFeatureBuilder>();

            var emailProvider = builder.Configuration["Email:Provider"] ?? "Dev";
            if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Host"]) ||
                    !int.TryParse(builder.Configuration["Email:Smtp:Port"], out var smtpPort) || smtpPort is < 1 or > 65535 ||
                    string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:FromAddress"]))
                    throw new InvalidOperationException("Email:Provider=Smtp requires Email__Smtp__Host, Email__Smtp__Port, and Email__Smtp__FromAddress.");
                if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Username"]) && string.IsNullOrWhiteSpace(builder.Configuration["Email:Smtp:Password"]))
                    throw new InvalidOperationException("SMTP authentication requires Email__Smtp__Password when Email__Smtp__Username is set.");
            }
            else if (!string.Equals(emailProvider, "Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Email:Provider must be either Dev or Smtp.");

            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing.");
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
                throw new InvalidOperationException("JWT Key must contain at least 32 UTF-8 bytes and be supplied through a secret store or environment variable.");
            var accessTokenMinutes = builder.Configuration.GetValue("Jwt:AccessTokenMinutes", 15);
            if (accessTokenMinutes is < 5 or > 30)
                throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 5 and 30 minutes.");

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer is missing.");

            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT Audience is missing.");

            if (!builder.Environment.IsDevelopment())
            {
                var internalApiKey = builder.Configuration["Ai:InternalApiKey"] ?? builder.Configuration["PythonAi:InternalApiKey"];
                if (string.IsNullOrWhiteSpace(internalApiKey) || Encoding.UTF8.GetByteCount(internalApiKey) < 32)
                    throw new InvalidOperationException("Production requires Ai__InternalApiKey with at least 32 UTF-8 bytes.");
                var superAdminEmail = builder.Configuration["SUPERADMIN_EMAIL"] ?? builder.Configuration["SuperAdmin:Email"];
                var superAdminPassword = builder.Configuration["SUPERADMIN_PASSWORD"] ?? builder.Configuration["SuperAdmin:Password"];
                if (string.IsNullOrWhiteSpace(superAdminEmail) || string.IsNullOrWhiteSpace(superAdminPassword))
                    throw new InvalidOperationException("Production requires SUPERADMIN_EMAIL and SUPERADMIN_PASSWORD from the environment or a secret store.");
            }

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

            // Hangfire's server must not hold local liveness hostage when PostgreSQL is stopped.
            // Enable it in development explicitly once PostgreSQL is available; production runs it by default.
            var runHangfireServer = !builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Hangfire:RunServer");
            if (runHangfireServer) builder.Services.AddHangfireServer();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SuperAdminOnly", policy =>
                {
                    policy.RequireRole("SuperAdmin");
                });

                options.AddPolicy("OwnerOnly", policy =>
                {
                    policy.RequireRole("TenantOwner");
                });

                options.AddPolicy("CoachOnly", policy =>
                {
                    policy.RequireRole("TenantOwner", "CoachStaff");
                });

                options.AddPolicy("ClientOnly", policy =>
                {
                    policy.RequireRole("Client");
                });
            });

            var app = builder.Build();

            if (args.Length == 1 && string.Equals(args[0], "--migrate", StringComparison.OrdinalIgnoreCase))
            {
                await using var migrationScope = app.Services.CreateAsyncScope();
                var migrationDb = migrationScope.ServiceProvider.GetRequiredService<CoachPulseDbContext>();
                await migrationDb.Database.MigrateAsync();
                await app.DisposeAsync();
                return;
            }

            if (args.Length == 2 && string.Equals(args[0], "--export-swagger", StringComparison.OrdinalIgnoreCase))
            {
                var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
                var outputPath = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await using var output = File.CreateText(outputPath);
                document.SerializeAsV3(new OpenApiJsonWriter(output));
                await app.DisposeAsync();
                return;
            }

            if (args.Length >= 2 && string.Equals(args[0], "--seed-exercises", StringComparison.OrdinalIgnoreCase))
            {
                using var seedScope = app.Services.CreateScope();
                var seedDb = seedScope.ServiceProvider.GetRequiredService<CoachPulseDbContext>();
                var added = await PublicExerciseSeeder.SeedCsvAsync(seedDb, args[1]);
                Console.WriteLine($"Public exercise seed complete. Added {added} new exercises.");
                await app.DisposeAsync();
                return;
            }

            // إعداد Seeding والـ Recurring Jobs بعد بناء التطبيق

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // تفعيل Hangfire Dashboard
            app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = 500,
                    Title = "An unexpected error occurred.",
                    Extensions = { ["traceId"] = context.TraceIdentifier }
                });
            }));
            app.UseStatusCodePages(async statusContext =>
            {
                var context = statusContext.HttpContext;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = context.Response.StatusCode,
                    Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(context.Response.StatusCode),
                    Extensions = { ["traceId"] = context.TraceIdentifier }
                });
            });
            var forwardedHeaders = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost };
            foreach (var proxy in app.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                if (System.Net.IPAddress.TryParse(proxy, out var proxyAddress)) forwardedHeaders.KnownProxies.Add(proxyAddress);
            app.UseForwardedHeaders(forwardedHeaders);
            if (app.Environment.IsProduction()) app.UseHsts();
            app.UseHttpsRedirection();
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    return Task.CompletedTask;
                });
                await next();
            });
            app.UseCors("ConfiguredOrigins");

            app.UseMiddleware<RouteDeprecationMiddleware>();

            app.UseAuthentication();

            app.UseMiddleware<FirstLoginPasswordChangeMiddleware>();

            app.UseMiddleware<LoginRateLimitKeyMiddleware>();

            app.UseRateLimiter();

            app.UseMiddleware<TenantResolutionMiddleware>();

            app.UseAuthorization();

            if (!app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Hangfire:EnableDashboard"))
            {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = [new SuperAdminHangfireAuthorizationFilter()]
                });
            }

            app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }))
                .AllowAnonymous();

            app.MapGet("/readyz", async (
                DatabaseStartupState startupState,
                IHttpClientFactory httpClientFactory,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var databaseReady = startupState.DatabaseReachable;
                var pythonReady = false;
                var pythonBaseUrl = configuration["PythonAi:BaseUrl"];
                if (Uri.TryCreate(pythonBaseUrl, UriKind.Absolute, out var baseUri))
                {
                    try
                    {
                        var readinessClient = httpClientFactory.CreateClient("python-ai");
                        var internalKey = configuration["Ai:InternalApiKey"] ?? configuration["PythonAi:InternalApiKey"];
                        if (!string.IsNullOrWhiteSpace(internalKey)) readinessClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Internal-Api-Key", internalKey);
                        using var response = await readinessClient.GetAsync(new Uri(baseUri, "ready"), cancellationToken);
                        pythonReady = response.IsSuccessStatusCode;
                    }
                    catch (HttpRequestException) { pythonReady = false; }
                    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { pythonReady = false; }
                }

                var requirePython = configuration.GetValue<bool>("Readiness:RequirePythonService");
                var ready = databaseReady && startupState.IsInitialized && (!requirePython || pythonReady);
                return Results.Json(new
                {
                    status = ready ? "ready" : "not_ready",
                    checks = new { database = databaseReady, startup = startupState.IsInitialized, startupState.Failure, pythonAi = requirePython ? pythonReady : (bool?)null }
                }, statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
            }).AllowAnonymous();


            app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
            app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();

            app.MapControllers();

            var databaseInitializer = app.Services.GetRequiredService<DatabaseStartupInitializer>();
            app.Lifetime.ApplicationStarted.Register(databaseInitializer.Start);
            app.Lifetime.ApplicationStopping.Register(databaseInitializer.Stop);

            await app.RunAsync();
        }
    }
}

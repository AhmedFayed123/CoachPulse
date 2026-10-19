using CoachPulse.Api.BackgroundJobs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Infrastructure.BackgroundJobs;
using CoachPulse.Infrastructure.Data.Seed;
using CoachPulse.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace CoachPulse.Api.Infrastructure;

public sealed class DatabaseStartupInitializer(
    IServiceScopeFactory scopeFactory,
    DatabaseStartupState state,
    ILogger<DatabaseStartupInitializer> logger) : IDisposable
{
    public void Start() => _ = Task.Run(() => ExecuteAsync(_stopping.Token));

    public void Stop() => _stopping.Cancel();
    public void Dispose() => _stopping.Dispose();

    private readonly CancellationTokenSource _stopping = new();

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // BackgroundService.StartAsync executes synchronously until the first incomplete await.
        // Yield before probing PostgreSQL so Kestrel can start liveness/readiness endpoints.
        await Task.Yield();
        var waitingLogged = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<CoachPulseDbContext>();

                if (!await db.Database.CanConnectAsync(stoppingToken))
                {
                    state.MarkDatabaseUnavailable("PostgreSQL is unavailable.");
                    if (!waitingLogged)
                    {
                        logger.LogWarning("Database initialization is waiting for PostgreSQL. Configure ConnectionStrings__DefaultConnection and start PostgreSQL; readiness remains unavailable until initialization completes.");
                        waitingLogged = true;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                state.MarkDatabaseReachable();
                var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(stoppingToken)).ToArray();
                if (pendingMigrations.Length > 0)
                {
                    var message = $"Database has {pendingMigrations.Length} pending EF Core migration(s). Apply them with 'dotnet ef database update --project CoachPulse.Infrastructure --startup-project CoachPulse.Api', then restart the API.";
                    state.MarkFailure(message);
                    logger.LogError("{StartupFailure}", message);
                    return;
                }

                var passwordHasher = services.GetRequiredService<IPasswordHasher>();
                var configuration = services.GetRequiredService<IConfiguration>();
                await DbSeeder.SeedAsync(db, passwordHasher, configuration, services.GetRequiredService<IHostEnvironment>().IsProduction());
                await DemoDataSeeder.SeedAsync(db, passwordHasher, configuration, services.GetRequiredService<IHostEnvironment>().IsDevelopment(), services.GetRequiredService<IHostEnvironment>().IsProduction());
                RegisterRecurringJobs(services.GetRequiredService<IRecurringJobManager>());

                state.MarkInitialized();
                logger.LogInformation("Database startup initialization and recurring job registration completed.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (DbException exception)
            {
                state.MarkDatabaseUnavailable("PostgreSQL initialization failed; see API logs for the database error.");
                logger.LogWarning(exception, "PostgreSQL initialization failed; retrying in 5 seconds. Verify the database is running, migrations are applied, and the configured credentials are valid.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception exception)
            {
                state.MarkFailure("Database startup initialization failed; see API logs for details.");
                logger.LogCritical(exception, "Database startup initialization failed. API liveness remains available, readiness stays unavailable, and initialization will not be reported as successful.");
                return;
            }
        }
    }

    private static void RegisterRecurringJobs(IRecurringJobManager jobs)
    {
        jobs.AddOrUpdate<ExpireSubscriptionsJob>("expire-subscriptions-daily", job => job.RunAsync(CancellationToken.None), Cron.Daily);
        jobs.AddOrUpdate<SubscriptionExpiryReminderJob>("subscription-expiry-reminders-daily", job => job.RunAsync(CancellationToken.None), Cron.Daily);
        jobs.AddOrUpdate<PendingPaymentReminderJob>("pending-payment-reminders-six-hourly", job => job.RunAsync(CancellationToken.None), Cron.HourInterval(6));
        jobs.AddOrUpdate<PaymobReconciliationJob>("paymob-reconciliation-hourly", job => job.RunAsync(CancellationToken.None), Cron.Hourly);
        jobs.AddOrUpdate<CleanupOrphanFilesJob>("cleanup-orphan-files-weekly", job => job.CleanupAsync(CancellationToken.None), Cron.Weekly);
        jobs.AddOrUpdate<CleanupExpiredInvitationsJob>("cleanup-expired-invitations-daily", job => job.CleanupAsync(CancellationToken.None), Cron.Daily);
        jobs.AddOrUpdate<ChurnScoringJob>("churn-scoring-nightly", job => job.RunAsync(CancellationToken.None), Cron.Daily);
        jobs.AddOrUpdate<CheckInReminderJob>("check-in-reminders-daily", job => job.RunAsync(CancellationToken.None), Cron.Daily);
        jobs.AddOrUpdate<CleanupReadNotificationsJob>("cleanup-read-notifications-weekly", job => job.CleanupAsync(CancellationToken.None), Cron.Weekly);
        jobs.AddOrUpdate<GenerateUserDataExportJob>("cleanup-data-exports-daily", job => job.CleanupAsync(CancellationToken.None), Cron.Daily);
    }
}

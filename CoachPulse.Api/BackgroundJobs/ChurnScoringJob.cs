using CoachPulse.Api.Services;
using Hangfire;

namespace CoachPulse.Api.BackgroundJobs;

public sealed class ChurnScoringJob(ChurnScoringService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task RunAsync(CancellationToken cancellationToken) => await service.ScoreActiveTenantsAsync(cancellationToken);
}

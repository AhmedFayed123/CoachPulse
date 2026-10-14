using CoachPulse.Application.DTOs;

namespace CoachPulse.Application.Interfaces;

public interface IAiClient
{
    Task<AiWorkoutContent> GenerateProgramAsync(AiGenerateInput request, CancellationToken cancellationToken);
    Task<AiWorkoutContent> RegenerateProgramAsync(AiRegenerateInput request, CancellationToken cancellationToken);
    Task<AiChurnResponse> PredictChurnAsync(AiChurnRequest request, CancellationToken cancellationToken);
    Task<AiChurnBatchResponse> PredictChurnBatchAsync(IReadOnlyList<AiChurnRequest> requests, CancellationToken cancellationToken);
    Task<AiModelInfo> GetModelInfoAsync(CancellationToken cancellationToken);
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}

public interface IChurnFeatureBuilder
{
    Task<CoachPulse.Application.DTOs.AiChurnFeatures> BuildAsync(Guid tenantId, CoachPulse.Domain.Entities.Client client, CancellationToken cancellationToken);
}

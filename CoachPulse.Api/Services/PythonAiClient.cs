using System.Net.Http.Json;
using System.Text.Json;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;

namespace CoachPulse.Api.Services;

public sealed class PythonAiClient(IHttpClientFactory factory, IConfiguration configuration) : IAiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int ConsecutiveFailures;
    private static DateTime CircuitOpenUntilUtc;
    private string Key => configuration["Ai:InternalApiKey"] ?? configuration["PythonAi:InternalApiKey"] ?? "";
    private HttpClient Client(string name)
    {
        var client = factory.CreateClient(name);
        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Internal-Api-Key", Key);
        return client;
    }
    private async Task<T> Send<T>(string clientName, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        if (CircuitOpenUntilUtc > DateTime.UtcNow) throw new HttpRequestException("AI service circuit is open after repeated failures.");
        Exception? error = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);
                if (body is not null) request.Content = JsonContent.Create(body, options: Json);
                using var response = await Client(clientName).SendAsync(request, ct);
                if ((int)response.StatusCode >= 500 && attempt == 0) { error = new HttpRequestException("AI service unavailable"); continue; }
                if (!response.IsSuccessStatusCode) throw new HttpRequestException($"AI service returned {(int)response.StatusCode}.");
                var result = (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
                Interlocked.Exchange(ref ConsecutiveFailures, 0);
                CircuitOpenUntilUtc = default;
                return result;
            }
            catch (HttpRequestException ex) when (attempt == 0) { error = ex; }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested && attempt == 0) { error = ex; }
            catch (HttpRequestException ex) { RecordFailure(); throw new HttpRequestException("AI service request failed after retry.", ex); }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested) { RecordFailure(); throw new HttpRequestException("AI service timed out after retry.", ex); }
        }
        throw new HttpRequestException("Python AI service is unavailable after a bounded retry.", error);
    }
    private static void RecordFailure() { if (Interlocked.Increment(ref ConsecutiveFailures) >= 3) CircuitOpenUntilUtc = DateTime.UtcNow.AddSeconds(30); }
    public Task<AiWorkoutContent> GenerateProgramAsync(AiGenerateInput request, CancellationToken ct) => Send<AiWorkoutContent>("ai-generation", HttpMethod.Post, "generate-program", request, ct);
    public Task<AiWorkoutContent> RegenerateProgramAsync(AiRegenerateInput request, CancellationToken ct)
    {
        var payload = new { request.Program.ClientId, request.Program.Goal, request.Program.Level, request.Program.DaysPerWeek, request.Program.SessionMinutes, request.Program.DurationWeeks, request.Program.Equipment, request.Program.Injuries, request.Program.DietaryRestrictions, request.Program.CaloriesTarget, request.Program.Language, request.Program.ExtraNotes, availableExercises = request.Program.AvailableExercises.Select(x => new { id = x.Id, x.Name, x.MuscleGroup, x.Equipment }), request.OriginalProgramId, request.CoachNotes };
        return Send<AiWorkoutContent>("ai-generation", HttpMethod.Post, "regenerate-program", payload, ct);
    }
    public Task<AiChurnResponse> PredictChurnAsync(AiChurnRequest request, CancellationToken ct) => Send<AiChurnResponse>("ai-churn", HttpMethod.Post, "predict-churn", request, ct);
    public Task<AiChurnBatchResponse> PredictChurnBatchAsync(IReadOnlyList<AiChurnRequest> requests, CancellationToken ct) => Send<AiChurnBatchResponse>("ai-churn", HttpMethod.Post, "predict-churn/batch", new { items = requests }, ct);
    public Task<AiModelInfo> GetModelInfoAsync(CancellationToken ct) => Send<AiModelInfo>("ai-churn", HttpMethod.Get, "models/churn/info", null, ct);
    public Task<AiProgressSummary> CreateProgressSummaryAsync(AiProgressSummaryInput request, CancellationToken ct) => Send<AiProgressSummary>("ai-generation", HttpMethod.Post, "progress-summary", request, ct);
    public async Task<bool> IsReadyAsync(CancellationToken ct) { try { await Send<JsonElement>("ai-churn", HttpMethod.Get, "ready", null, ct); return true; } catch { return false; } }
}

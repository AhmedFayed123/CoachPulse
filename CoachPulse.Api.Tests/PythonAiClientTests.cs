using System.Net;
using System.Text;
using CoachPulse.Api.Services;
using CoachPulse.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class PythonAiClientTests
{
    [Fact]
    public async Task Generate_SendsInternalKeyAndReturnsValidatedProviderPayload()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("{\"workoutPlan\":{\"weeks\":[]},\"mealPlan\":{},\"unmatchedExercises\":[]}", Encoding.UTF8, "application/json") });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:InternalApiKey"] = "test-only-key" }).Build();
        var client = new PythonAiClient(new FakeFactory(handler), config);
        var input = new AiGenerateInput(Guid.NewGuid(), "FatLoss", "Beginner", 3, 45, 4, [], [], [], null, "en", "", []);
        var result = await client.GenerateProgramAsync(input, CancellationToken.None);
        Assert.Empty(result.UnmatchedExercises);
        Assert.Equal("test-only-key", handler.ObservedKey);
    }

    [Fact]
    public async Task ProviderFailure_IsSurfacedForGateway503Mapping()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var client = new PythonAiClient(new FakeFactory(handler), config);
        var input = new AiGenerateInput(Guid.NewGuid(), "FatLoss", "Beginner", 3, 45, 4, [], [], [], null, "en", "", []);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GenerateProgramAsync(input, CancellationToken.None));
    }

    private sealed class FakeFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("http://ai.test/") };
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public string? ObservedKey { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ObservedKey = request.Headers.GetValues("X-Internal-Api-Key").FirstOrDefault();
            return Task.FromResult(response(request));
        }
    }
}

using CoachPulse.Api.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class SecurityConfigurationTests
{
    [Theory]
    [InlineData(typeof(AuthController), "Login", "auth-login")]
    [InlineData(typeof(AuthController), "Register", "auth-register")]
    [InlineData(typeof(AuthController), "RegisterClient", "auth-register")]
    [InlineData(typeof(AuthController), "ForgotPassword", "auth-forgot")]
    [InlineData(typeof(FilesController), "Upload", "files-upload")]
    [InlineData(typeof(PublicController), "SlugAvailable", "public-slug")]
    [InlineData(typeof(WebhooksController), "HandlePaymobWebhook", "paymob-webhook")]
    public void SensitiveRoutesDeclareTheirSpecificLimit(Type controller, string action, string policy)
    {
        var method = controller.GetMethod(action, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.Equal(policy, method!.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
    }

    [Fact]
    public void ProductionSecurityAndInternalNetworkConfigurationRemainExplicit()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "CoachPulse.Api", "Program.cs"))) root = root.Parent;
        Assert.NotNull(root);
        var program = File.ReadAllText(Path.Combine(root!.FullName, "CoachPulse.Api", "Program.cs"));
        Assert.Contains("options.GlobalLimiter", program);
        Assert.Contains("app.UseCors(\"ConfiguredOrigins\")", program);
        Assert.Contains("app.UseHsts()", program);
        Assert.Contains("app.UseExceptionHandler", program);
        Assert.Contains("X-Content-Type-Options", program);
        Assert.Contains("X-Frame-Options", program);
        Assert.Contains("Referrer-Policy", program);
        Assert.Contains("options.MaximumReceiveMessageSize = 64 * 1024", program);
        var production = File.ReadAllText(Path.Combine(root.FullName, "CoachPulse.Api", "appsettings.Production.json"));
        Assert.DoesNotContain("Key", production, StringComparison.OrdinalIgnoreCase);
        var compose = File.ReadAllText(Path.Combine(root.FullName, "docker-compose.yml"));
        Assert.Contains("internal: true", compose);
        Assert.Contains("expose: [\"8000\"]", compose);
        var pythonDockerfile = File.ReadAllText(Path.Combine(root.FullName, "CoachPulse.AI", "Dockerfile"));
        Assert.Contains("USER coachpulse", pythonDockerfile);
        var apiDockerfile = File.ReadAllText(Path.Combine(root.FullName, "CoachPulse.Api", "Dockerfile"));
        Assert.Contains("USER $APP_UID", apiDockerfile);
    }
}

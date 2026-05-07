using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventService.Mcp.Tests.Integration;

public class GracefulShutdownTests
{
    [Fact]
    public async Task StopApplication_fires_ApplicationStopping_then_ApplicationStopped()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EventService:BaseUrl"] = "http://localhost:9999",
                    ["Mcp:EndpointPath"] = "/mcp"
                });
            });
        });

        // Force the host to materialise.
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/health");
        Assert.True(resp.IsSuccessStatusCode);

        var lifetime = factory.Services.GetRequiredService<IHostApplicationLifetime>();
        var stoppingFired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stoppedFired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        lifetime.ApplicationStopping.Register(() => stoppingFired.TrySetResult(true));
        lifetime.ApplicationStopped.Register(() => stoppedFired.TrySetResult(true));

        // Simulate Ctrl+C — Generic Host wires SIGINT/Ctrl+C to call StopApplication() on the lifetime.
        lifetime.StopApplication();

        var stopping = await Task.WhenAny(stoppingFired.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(stoppingFired.Task, stopping);

        var stopped = await Task.WhenAny(stoppedFired.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(stoppedFired.Task, stopped);
    }
}

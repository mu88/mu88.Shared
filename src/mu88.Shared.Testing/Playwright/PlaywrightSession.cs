using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Networks;
using Microsoft.Playwright;
using Testcontainers.Playwright;

namespace mu88.Shared.Testing.Playwright;

/// <summary>
/// Starts a Testcontainers-based Playwright browser server and connects a Chromium <see cref="IBrowser"/> to it,
/// bundling the container/connection lifecycle so callers don't need to wire up
/// Microsoft.Playwright + Testcontainers.Playwright manually.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires a live Docker daemon and the Playwright Node.js driver; already exercised end-to-end by consumers' System tests.")]
public sealed class PlaywrightSession : IAsyncDisposable
{
    private readonly PlaywrightContainer _container;
    private readonly IPlaywright _playwright;

    private PlaywrightSession(PlaywrightContainer container, IPlaywright playwright, IBrowser browser)
    {
        _container = container;
        _playwright = playwright;
        Browser = browser;
    }

    /// <summary>The Chromium browser instance connected to the started Playwright container.</summary>
    public IBrowser Browser { get; }

    /// <summary>
    /// Starts a Playwright container on the given network and connects a Chromium browser to it.
    /// </summary>
    /// <param name="image">The Playwright image reference, e.g. from <see cref="Testcontainers.TestcontainerImages.GetImageFromDockerfile"/>.</param>
    /// <param name="network">The Testcontainers network the Playwright container should join.</param>
    /// <param name="cancellationToken">Token used to cancel the container startup.</param>
    public static async Task<PlaywrightSession> StartAsync(string image, INetwork network, CancellationToken cancellationToken)
    {
        var container = new PlaywrightBuilder(image)
            .WithNetwork(network)
            .Build();

        await container.StartAsync(cancellationToken);

        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await playwright.Chromium.ConnectAsync(container.GetConnectionString());

        return new PlaywrightSession(container, playwright, browser);
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.CloseAsync();
        _playwright.Dispose();
        await _container.DisposeAsync();
    }
}

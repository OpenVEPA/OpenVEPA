using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenVEPA.Server;
using Spectre.Console;

namespace OpenVEPA.Cli.Setup;

/// <summary>
/// Browser-based setup wizard that starts a temporary Kestrel server,
/// serves an HTML setup page, and collects configuration via a REST API.
/// </summary>
internal static class SetupWizardWebUi
{
    private static readonly TimeSpan WizardTimeout = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Runs the web-based setup wizard bound to localhost on an auto-detected port.
    /// Starts a temporary server, opens the browser,
    /// waits for the user to submit configuration, then shuts down.
    /// </summary>
    /// <param name="ct">A cancellation token to abort the wizard.</param>
    /// <returns>The configuration from the user, or <c>null</c> if cancelled or timed out.</returns>
    public static async Task<SetupConfiguration?> RunAsync(CancellationToken ct = default)
    {
        return await RunAsync("localhost", 0, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the web-based setup wizard bound to the specified address.
    /// </summary>
    /// <param name="bindAddress">The IP address to bind to (e.g., "localhost" or "0.0.0.0").</param>
    /// <param name="port">The port to bind to. If 0 or negative, a free port is selected.</param>
    /// <param name="ct">A cancellation token to abort the wizard.</param>
    /// <returns>The configuration from the user, or <c>null</c> if cancelled or timed out.</returns>
    public static async Task<SetupConfiguration?> RunAsync(
        string bindAddress,
        int port,
        CancellationToken ct = default)
    {
        if (port <= 0)
        {
            port = FindFreePort();
        }

        var displayUrl = bindAddress == "0.0.0.0"
            ? $"http://localhost:{port}"
            : $"http://{bindAddress}:{port}";

        var tcs = new TaskCompletionSource<SetupConfiguration?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = CreateWebApplication(bindAddress, port, tcs);
        await app.StartAsync(ct).ConfigureAwait(false);

        var isDocker = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!isDocker)
        {
            TryOpenBrowser(displayUrl);
        }

        AnsiConsole.MarkupLine($"[bold green]Setup wizard:[/] [link={displayUrl}]{displayUrl}[/]");

        var config = await WaitForResultAsync(tcs, ct).ConfigureAwait(false);

        await app.StopAsync(CancellationToken.None).ConfigureAwait(false);

        return config;
    }

    /// <summary>Finds an available TCP port on the loopback interface.</summary>
    /// <returns>A free port number.</returns>
    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    /// <summary>Creates a temporary web application for the setup wizard.</summary>
    /// <param name="bindAddress">The IP address or hostname to bind to.</param>
    /// <param name="port">The port to listen on.</param>
    /// <param name="tcs">The completion source to signal when setup finishes.</param>
    /// <returns>A configured but not yet started web application.</returns>
    private static WebApplication CreateWebApplication(
        string bindAddress,
        int port,
        TaskCompletionSource<SetupConfiguration?> tcs)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://{bindAddress}:{port}");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        MapEndpoints(app, tcs);

        return app;
    }

    /// <summary>Maps the setup wizard HTTP endpoints.</summary>
    /// <param name="app">The web application to configure.</param>
    /// <param name="tcs">The completion source to signal on submit or cancel.</param>
    private static void MapEndpoints(
        WebApplication app,
        TaskCompletionSource<SetupConfiguration?> tcs)
    {
        app.MapGet("/", () => Results.Content(SetupPageHtml.Content, "text/html"));

        app.MapPost("/api/setup", async (HttpContext context) =>
        {
            SetupConfiguration? config;

            try
            {
                config = await JsonSerializer.DeserializeAsync<SetupConfiguration>(
                    context.Request.Body, JsonOptions, context.RequestAborted)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return Results.BadRequest("Invalid configuration JSON.");
            }

            if (config is null)
            {
                return Results.BadRequest("Empty configuration body.");
            }

            var resolved = config with { HomePath = ExpandHomePath(config.HomePath) };
            tcs.TrySetResult(resolved);

            return Results.Ok();
        });

        app.MapPost("/api/cancel", () =>
        {
            tcs.TrySetResult(null);
            return Results.Ok();
        });

        app.MapGet("/api/models", async (HttpContext context) =>
        {
            var provider = context.Request.Query["provider"].ToString();
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Results.BadRequest(new { error = "Missing required parameter: provider" });
            }

            if (!ModelListProxy.ProviderSupportsListing(provider))
            {
                return Results.Ok(new
                {
                    success = false,
                    models = Array.Empty<string>(),
                    provider,
                    endpoint = string.Empty,
                    error = "Provider does not support dynamic model listing.",
                    diagnostics = $"The provider '{provider}' does not expose a model list API.",
                });
            }

            var apiKey = context.Request.Query["apiKey"].ToString();
            var endpoint = context.Request.Query["endpoint"].ToString();

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = ModelListProxy.GetDefaultEndpoint(provider);
            }

            endpoint = endpoint.TrimEnd('/');

            var result = await ModelListProxy.FetchModelsWithStatusAsync(
                provider, apiKey, endpoint, context.RequestAborted).ConfigureAwait(false);

            return Results.Ok(new
            {
                success = result.Success,
                models = result.Models,
                provider = result.Provider,
                endpoint = result.Endpoint,
                error = result.Error,
                diagnostics = result.Diagnostics,
            });
        });
    }

    /// <summary>Attempts to open the default browser to the given URL.</summary>
    /// <param name="url">The URL to open.</param>
    private static void TryOpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (SystemException)
        {
            // Browser failed to open. The URL is printed to the terminal instead.
        }
    }

    /// <summary>
    /// Waits for the user to complete or cancel setup, respecting a timeout.
    /// </summary>
    /// <param name="tcs">The completion source to await.</param>
    /// <param name="ct">External cancellation token.</param>
    /// <returns>The submitted configuration, or <c>null</c> on cancel or timeout.</returns>
    private static async Task<SetupConfiguration?> WaitForResultAsync(
        TaskCompletionSource<SetupConfiguration?> tcs,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(WizardTimeout);

        using var registration = timeoutCts.Token.Register(
            static state => ((TaskCompletionSource<SetupConfiguration?>)state!).TrySetResult(null),
            tcs);

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "Waiting for setup to complete in your browser...",
                async _ => await tcs.Task.ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    /// <summary>Expands a leading tilde to the user home directory.</summary>
    /// <param name="path">The raw path that may start with <c>~</c>.</param>
    /// <returns>The expanded absolute path.</returns>
    private static string ExpandHomePath(string path)
    {
        if (!path.StartsWith('~'))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(
            home,
            path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
}

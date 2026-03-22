using Microsoft.Extensions.Logging;

namespace OpenVEPA.Providers;

/// <summary>
/// A <see cref="DelegatingHandler"/> that logs every HTTP request/response
/// so we can see the exact URLs the OpenAI SDK sends.
/// </summary>
internal sealed class LoggingHttpHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public LoggingHttpHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Url}",
            request.Method, request.RequestUri?.AbsoluteUri);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("HTTP {Method} {Url} → {StatusCode} ({ContentType})",
            request.Method,
            request.RequestUri?.AbsoluteUri,
            (int)response.StatusCode,
            response.Content.Headers.ContentType?.ToString() ?? "unknown");

        if (!response.IsSuccessStatusCode)
        {
            // Buffer the content so the SDK can still read it after we log.
            await response.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (body.Length > 500) body = body[..500];
            _logger.LogWarning("HTTP error response body: {Body}", body);
        }

        return response;
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OpenVEPA.Server.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a structured JSON error response
/// instead of a raw 500 with empty body.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for recording unhandled exceptions.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Processes the HTTP request, catching any unhandled exceptions.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started, cannot write error body");
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var errorMessage = BuildErrorMessage(ex);
            await context.Response.WriteAsJsonAsync(new
            {
                error = errorMessage,
                type = ex.GetType().Name,
            }).ConfigureAwait(false);
        }
    }

    private static string BuildErrorMessage(Exception ex)
    {
        // Provide actionable messages for common failure types
        return ex switch
        {
            InvalidOperationException ioe when ioe.Message.Contains("provider", StringComparison.OrdinalIgnoreCase)
                => $"LLM provider configuration error: {ioe.Message}",
            InvalidOperationException ioe when ioe.Message.Contains("service", StringComparison.OrdinalIgnoreCase)
                => $"Service initialization error: {ioe.Message}. The application may need to be restarted.",
            ArgumentNullException ane
                => $"A required component is missing: {ane.ParamName}. Please check your configuration.",
            HttpRequestException hre
                => $"Network error communicating with LLM provider: {hre.Message}",
            _ => $"An internal error occurred: {ex.Message}",
        };
    }
}

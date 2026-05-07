using System.Net.Http.Json;
using System.Text.Json;

namespace EventService.Mcp.Errors;

public static class ProblemDetailsExceptionMapper
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;

        ProblemDetails? pd = null;
        var mediaType = resp.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null &&
            (mediaType.Contains("problem+json", StringComparison.OrdinalIgnoreCase) ||
             mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(JsonOpts, ct);
            }
            catch
            {
                // Body wasn't a parseable ProblemDetails; fall back to status text.
            }
        }

        var correlationId = pd?.CorrelationId
            ?? (resp.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.FirstOrDefault() : null);

        throw new UpstreamException(
            statusCode: (int)resp.StatusCode,
            title: pd?.Title ?? resp.ReasonPhrase ?? "Upstream error",
            detail: pd?.Detail,
            correlationId: correlationId);
    }
}

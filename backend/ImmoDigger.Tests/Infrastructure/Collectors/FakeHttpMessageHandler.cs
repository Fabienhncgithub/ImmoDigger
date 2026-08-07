using System.Net;

namespace ImmoDigger.Tests.Infrastructure.Collectors;

/// <summary>
/// A test double standing in for the real network: maps a request path
/// (matched by substring) to a canned response, so collectors can be
/// tested against local fixture data without ever calling the real site.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string PathContains, Func<HttpResponseMessage> Respond)> _routes = [];
    public List<string> RequestedPaths { get; } = [];

    public FakeHttpMessageHandler AddTextResponse(string pathContains, string content, string contentType = "text/plain") =>
        AddResponse(pathContains, () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, contentType),
        });

    public FakeHttpMessageHandler AddJsonResponse(string pathContains, string json) =>
        AddTextResponse(pathContains, json, "application/json");

    public FakeHttpMessageHandler AddStatusResponse(string pathContains, HttpStatusCode statusCode) =>
        AddResponse(pathContains, () => new HttpResponseMessage(statusCode));

    private FakeHttpMessageHandler AddResponse(string pathContains, Func<HttpResponseMessage> respond)
    {
        _routes.Add((pathContains, respond));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;
        RequestedPaths.Add(path);

        var route = _routes.FirstOrDefault(r => path.Contains(r.PathContains, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(route.Respond?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

/// <summary>Wraps a single fake handler behind <see cref="IHttpClientFactory"/> so collectors under test see the real interface they depend on.</summary>
internal sealed class FakeHttpClientFactory(string baseAddress, FakeHttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler) { BaseAddress = new Uri(baseAddress) };
}

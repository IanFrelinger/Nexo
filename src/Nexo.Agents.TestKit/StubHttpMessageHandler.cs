using System.Net;
using System.Text;

namespace Nexo.Agents.TestKit;

/// <summary>One recorded outbound HTTP request, fully materialized.</summary>
/// <param name="Method">Request method.</param>
/// <param name="RequestUri">Request URI.</param>
/// <param name="Body">Request body, read before the handler returned (empty when there was none).</param>
public sealed record RecordedHttpRequest(HttpMethod Method, Uri? RequestUri, string Body);

/// <summary>
/// A func-driven <see cref="HttpMessageHandler"/> that records what was sent.
///
/// This replaces the fleet of one-off handler classes that had accumulated across
/// the test projects — each re-deriving from HttpMessageHandler to do the same two
/// things: answer with a canned response, and remember the request so the test can
/// assert on it.
///
/// The body is read INSIDE SendAsync rather than kept as a live
/// <see cref="HttpRequestMessage"/>: request content is routinely disposed once the
/// client finishes, so a handler that merely stashes the message hands the test a
/// body it can no longer read.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

    /// <summary>Creates a handler from an async responder.</summary>
    /// <param name="respond">Produces the response for each request.</param>
    public StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        _respond = respond ?? throw new ArgumentNullException(nameof(respond));

    /// <summary>Creates a handler from a synchronous responder.</summary>
    /// <param name="respond">Produces the response for each request.</param>
    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : this((request, _) => Task.FromResult(
            (respond ?? throw new ArgumentNullException(nameof(respond)))(request)))
    {
    }

    /// <summary>Requests seen, in order, with their bodies already read.</summary>
    public List<RecordedHttpRequest> Requests { get; } = new();

    /// <summary>The most recent request, or null when none was sent.</summary>
    public RecordedHttpRequest? LastRequest => Requests.Count == 0 ? null : Requests[^1];

    /// <summary>Method of the most recent request.</summary>
    public HttpMethod? LastMethod => LastRequest?.Method;

    /// <summary>URI of the most recent request.</summary>
    public Uri? LastRequestUri => LastRequest?.RequestUri;

    /// <summary>True when the handler was never called.</summary>
    public bool WasNeverCalled => Requests.Count == 0;

    /// <summary>Answers every request with the same status and body.</summary>
    /// <param name="status">Status code to return.</param>
    /// <param name="content">Response body, if any.</param>
    /// <param name="mediaType">Body media type.</param>
    public static StubHttpMessageHandler Always(
        HttpStatusCode status,
        string? content = null,
        string mediaType = "application/json") =>
        new(_ => Respond(status, content, mediaType));

    /// <summary>Answers each request with the next scripted response.</summary>
    /// <param name="responses">Responses in order; the last repeats once exhausted.</param>
    public static StubHttpMessageHandler Sequence(params HttpResponseMessage[] responses)
    {
        var script = new Scripted<HttpResponseMessage>(responses);
        return new StubHttpMessageHandler(_ => script.Next());
    }

    /// <summary>Fails every request with the given exception — the transient-failure case.</summary>
    /// <param name="error">Exception to throw.</param>
    public static StubHttpMessageHandler Throws(Exception error) =>
        new((_, _) => Task.FromException<HttpResponseMessage>(error));

    /// <summary>Builds a response message without the usual boilerplate.</summary>
    /// <param name="status">Status code.</param>
    /// <param name="content">Body, if any.</param>
    /// <param name="mediaType">Body media type.</param>
    public static HttpResponseMessage Respond(
        HttpStatusCode status,
        string? content = null,
        string mediaType = "application/json") =>
        new(status)
        {
            Content = content is null
                ? new StringContent(string.Empty)
                : new StringContent(content, Encoding.UTF8, mediaType)
        };

    /// <summary>Wraps this handler in a client, optionally with a base address.</summary>
    /// <param name="baseAddress">Base address for the client.</param>
    public HttpClient CreateClient(string? baseAddress = null)
    {
        var client = new HttpClient(this, disposeHandler: false);
        if (baseAddress is not null)
            client.BaseAddress = new Uri(baseAddress);
        return client;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Requests.Add(new RecordedHttpRequest(request.Method, request.RequestUri, body));

        return await _respond(request, cancellationToken).ConfigureAwait(false);
    }
}

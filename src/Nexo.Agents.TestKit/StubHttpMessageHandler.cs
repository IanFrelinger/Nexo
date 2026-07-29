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
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

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

    /// <summary>
    /// Creates a handler from a synchronous responder that also sees the token.
    ///
    /// This is a named factory rather than a constructor overload on purpose:
    /// a two-parameter lambda cannot be resolved between
    /// <c>Func&lt;…, HttpResponseMessage&gt;</c> and
    /// <c>Func&lt;…, Task&lt;HttpResponseMessage&gt;&gt;</c>, so overloading on the
    /// return type would make every <c>(req, ct) =&gt; …</c> call site ambiguous.
    /// </summary>
    /// <param name="respond">Produces the response for each request.</param>
    public static StubHttpMessageHandler FromSync(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
    {
        if (respond is null) throw new ArgumentNullException(nameof(respond));
        return new StubHttpMessageHandler((request, ct) => Task.FromResult(respond(request, ct)));
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

    /// <summary>
    /// Replaces the responder mid-test, keeping the recorded history.
    ///
    /// Needed for subjects that are driven through more than one server state on a
    /// single client — e.g. "model absent, then present after a pull" — where a new
    /// handler would also mean a new HttpClient and lose the request log.
    /// </summary>
    /// <param name="respond">New responder.</param>
    public void SetResponder(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        _respond = respond ?? throw new ArgumentNullException(nameof(respond));

    /// <summary>Replaces the responder mid-test with a synchronous one.</summary>
    /// <param name="respond">New responder.</param>
    public void SetResponder(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        if (respond is null) throw new ArgumentNullException(nameof(respond));
        _respond = (request, _) => Task.FromResult(respond(request));
    }

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

    /// <summary>
    /// Answers requests whose path matches <paramref name="absolutePath"/> with
    /// <paramref name="content"/>, and everything else with 404.
    ///
    /// This is the shape several Ollama-facing suites hand-rolled: the subject is
    /// only correct if it calls the RIGHT endpoint, so a wrong path must 404 rather
    /// than be quietly served the happy-path body.
    /// </summary>
    /// <param name="absolutePath">Path to match, compared case-insensitively ignoring a trailing slash.</param>
    /// <param name="content">Body returned on a match.</param>
    /// <param name="mediaType">Body media type.</param>
    public static StubHttpMessageHandler ForPath(
        string absolutePath,
        string content,
        string mediaType = "application/json")
    {
        var wanted = absolutePath.TrimEnd('/');
        return new StubHttpMessageHandler(request =>
            request.RequestUri is not null
            && string.Equals(
                request.RequestUri.AbsolutePath.TrimEnd('/'),
                wanted,
                StringComparison.OrdinalIgnoreCase)
                ? Respond(HttpStatusCode.OK, content, mediaType)
                : Respond(HttpStatusCode.NotFound));
    }

    /// <summary>Answers every request with a binary body.</summary>
    /// <param name="content">Body bytes.</param>
    /// <param name="mediaType">Body media type.</param>
    /// <param name="status">Status code.</param>
    public static StubHttpMessageHandler AlwaysBytes(
        byte[] content,
        string mediaType,
        HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new ByteArrayContent(content)
            };
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            return response;
        });

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

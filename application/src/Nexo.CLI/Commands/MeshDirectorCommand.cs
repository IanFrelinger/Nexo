using System.CommandLine;
using System.Text;
using System.Text.Json;

namespace Nexo.CLI.Commands;

/// <summary>
/// HTTP client for calling a remote Nexo.API from headless hosts (optional mesh director routes when present on the hub).
/// </summary>
public sealed class MeshDirectorCommand : Command
{
    internal const string DirectorBaseUrlEnv = "NEXO_MESH_DIRECTOR_BASE_URL";
    internal const string MeshApiKeyEnv = "NEXO_MESH_API_KEY";

    private const string DefaultApiKeyHeader = "X-Nexo-Api-Key";
    private const string DefaultMeshTokenHeader = "X-Nexo-Mesh-Token";

    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    public MeshDirectorCommand() : base("director", "Call a remote Nexo.API over HTTP (GET/POST/PATCH).")
    {
        var baseUrlOpt = new Option<string?>(
            "--base-url",
            () => null,
            $"Director base URL. Defaults to {DirectorBaseUrlEnv}.");

        var apiKeyOpt = new Option<string?>(
            "--api-key",
            () => null,
            $"Optional {DefaultApiKeyHeader} (defaults to {MeshApiKeyEnv}).");

        var meshTokenOpt = new Option<string?>(
            "--mesh-token",
            () => null,
            $"Optional {DefaultMeshTokenHeader} for mutating /api/mesh when the hub enforces mesh tokens.");

        var timeoutOpt = new Option<int>("--timeout-seconds", () => 120, "HTTP timeout");
        var jsonOpt = new Option<bool>("--json", () => false, "Pretty-print JSON response bodies when possible");

        var pathArg = new Argument<string>("path", "Path from server root, e.g. /api/mesh/fleet/nodes or /health");

        var getCmd = new Command("get", "GET from hub");
        getCmd.Add(baseUrlOpt);
        getCmd.Add(apiKeyOpt);
        getCmd.Add(meshTokenOpt);
        getCmd.Add(timeoutOpt);
        getCmd.Add(jsonOpt);
        getCmd.AddArgument(pathArg);
        getCmd.SetHandler(InvokeGetAsync, baseUrlOpt, apiKeyOpt, meshTokenOpt, timeoutOpt, jsonOpt, pathArg);

        var pathPostArg = new Argument<string>("path", "Path, e.g. /api/mesh/fleet/nodes");
        var bodyFileOpt = new Option<string?>("--body-file", "UTF-8 JSON body file");
        var bodyOpt = new Option<string?>("--body", "Inline JSON body");

        var postCmd = new Command("post", "POST to hub");
        postCmd.Add(baseUrlOpt);
        postCmd.Add(apiKeyOpt);
        postCmd.Add(meshTokenOpt);
        postCmd.Add(timeoutOpt);
        postCmd.Add(jsonOpt);
        postCmd.AddArgument(pathPostArg);
        postCmd.Add(bodyFileOpt);
        postCmd.Add(bodyOpt);
        postCmd.SetHandler(InvokePostAsync, baseUrlOpt, apiKeyOpt, meshTokenOpt, timeoutOpt, jsonOpt, pathPostArg, bodyFileOpt, bodyOpt);

        var pathPatchArg = new Argument<string>("path", "Path, e.g. /api/mesh/tasks/{id}/status");
        var patchCmd = new Command("patch", "PATCH on hub");
        patchCmd.Add(baseUrlOpt);
        patchCmd.Add(apiKeyOpt);
        patchCmd.Add(meshTokenOpt);
        patchCmd.Add(timeoutOpt);
        patchCmd.Add(jsonOpt);
        patchCmd.AddArgument(pathPatchArg);
        patchCmd.Add(bodyFileOpt);
        patchCmd.Add(bodyOpt);
        patchCmd.SetHandler(InvokePatchAsync, baseUrlOpt, apiKeyOpt, meshTokenOpt, timeoutOpt, jsonOpt, pathPatchArg, bodyFileOpt, bodyOpt);

        AddCommand(getCmd);
        AddCommand(postCmd);
        AddCommand(patchCmd);
    }

    private static string? ResolveApiKey(string? fromOption) =>
        string.IsNullOrWhiteSpace(fromOption) ? Environment.GetEnvironmentVariable(MeshApiKeyEnv) : fromOption.Trim();

    private static string? ResolveMeshToken(string? fromOption) =>
        string.IsNullOrWhiteSpace(fromOption) ? Environment.GetEnvironmentVariable("NEXO_MESH_MUTATING_TOKEN") : fromOption.Trim();

    private static string ResolveBaseUrl(string? fromOption)
    {
        var v = string.IsNullOrWhiteSpace(fromOption) ? Environment.GetEnvironmentVariable(DirectorBaseUrlEnv) : fromOption.Trim();
        return string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();
    }

    internal static Uri BuildRequestUri(string baseUrl, string path)
    {
        var b = baseUrl.Trim().TrimEnd('/');
        var p = path.Trim();
        if (string.IsNullOrEmpty(p))
            throw new ArgumentException("Path is required.", nameof(path));
        if (!p.StartsWith('/'))
            p = "/" + p;
        return new Uri(b + p, UriKind.Absolute);
    }

    private static void ApplyHeaders(HttpRequestMessage msg, string? apiKey, string? meshToken, HttpMethod method)
    {
        if (!string.IsNullOrEmpty(apiKey))
            msg.Headers.TryAddWithoutValidation(DefaultApiKeyHeader, apiKey);

        if (method != HttpMethod.Get && !string.IsNullOrEmpty(meshToken))
            msg.Headers.TryAddWithoutValidation(DefaultMeshTokenHeader, meshToken);
    }

    private static async Task InvokeGetAsync(
        string? baseUrlOpt,
        string? apiKeyOpt,
        string? meshTokenOpt,
        int timeoutSeconds,
        bool json,
        string path)
    {
        var baseUrl = ResolveBaseUrl(baseUrlOpt);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.Error.WriteLine($"Set --base-url or environment variable {DirectorBaseUrlEnv}.");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            using var client = CreateHttpClient(timeoutSeconds);
            var uri = BuildRequestUri(baseUrl, path);
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            ApplyHeaders(req, ResolveApiKey(apiKeyOpt), ResolveMeshToken(meshTokenOpt), HttpMethod.Get);
            using var resp = await client.SendAsync(req).ConfigureAwait(false);
            await WriteResponseAsync(resp, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
    }

    private static Task InvokePostAsync(
        string? baseUrlOpt,
        string? apiKeyOpt,
        string? meshTokenOpt,
        int timeoutSeconds,
        bool json,
        string path,
        string? bodyFile,
        string? bodyJson) =>
        SendWithBodyAsync(HttpMethod.Post, baseUrlOpt, apiKeyOpt, meshTokenOpt, timeoutSeconds, json, path, bodyFile, bodyJson);

    private static Task InvokePatchAsync(
        string? baseUrlOpt,
        string? apiKeyOpt,
        string? meshTokenOpt,
        int timeoutSeconds,
        bool json,
        string path,
        string? bodyFile,
        string? bodyJson) =>
        SendWithBodyAsync(HttpMethod.Patch, baseUrlOpt, apiKeyOpt, meshTokenOpt, timeoutSeconds, json, path, bodyFile, bodyJson);

    private static async Task SendWithBodyAsync(
        HttpMethod method,
        string? baseUrlOpt,
        string? apiKeyOpt,
        string? meshTokenOpt,
        int timeoutSeconds,
        bool printJson,
        string path,
        string? bodyFile,
        string? bodyJson)
    {
        var baseUrl = ResolveBaseUrl(baseUrlOpt);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.Error.WriteLine($"Set --base-url or environment variable {DirectorBaseUrlEnv}.");
            Environment.ExitCode = 1;
            return;
        }

        string payload;
        if (!string.IsNullOrWhiteSpace(bodyFile))
        {
            if (!File.Exists(bodyFile))
            {
                Console.Error.WriteLine($"Body file not found: {bodyFile}");
                Environment.ExitCode = 1;
                return;
            }

            payload = await File.ReadAllTextAsync(bodyFile).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(bodyJson))
            payload = bodyJson;
        else
        {
            Console.Error.WriteLine("Provide --body or --body-file for POST/PATCH.");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            using var client = CreateHttpClient(timeoutSeconds);
            var uri = BuildRequestUri(baseUrl, path);
            using var req = new HttpRequestMessage(method, uri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            ApplyHeaders(req, ResolveApiKey(apiKeyOpt), ResolveMeshToken(meshTokenOpt), method);
            using var resp = await client.SendAsync(req).ConfigureAwait(false);
            await WriteResponseAsync(resp, printJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
    }

    private static HttpClient CreateHttpClient(int timeoutSeconds)
    {
        var s = Math.Clamp(timeoutSeconds, 5, 3600);
        return new HttpClient { Timeout = TimeSpan.FromSeconds(s) };
    }

    private static async Task WriteResponseAsync(HttpResponseMessage resp, bool preferFormattedJson)
    {
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        Console.WriteLine($"{(int)resp.StatusCode} {resp.ReasonPhrase}");

        if (string.IsNullOrEmpty(body))
        {
            Environment.ExitCode = resp.IsSuccessStatusCode ? 0 : 1;
            return;
        }

        if (preferFormattedJson &&
            resp.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                Console.WriteLine(JsonSerializer.Serialize(doc.RootElement, PrettyJson));
            }
            catch
            {
                Console.WriteLine(body);
            }
        }
        else
            Console.WriteLine(body);

        Environment.ExitCode = resp.IsSuccessStatusCode ? 0 : 1;
    }
}

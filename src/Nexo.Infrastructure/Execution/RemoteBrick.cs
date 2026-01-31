using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Nexo.BrickContracts;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Brick implementation that delegates ExecuteAsync to a remote brick host via HTTP.
/// </summary>
public sealed class RemoteBrick : Brick
{
    private readonly HttpClient _httpClient;
    private readonly string _executeBaseUrl;
    private readonly ILogger<RemoteBrick>? _logger;

    public RemoteBrick(
        BrickCatalogEntryDto catalogEntry,
        HttpClient httpClient,
        string executeBaseUrl,
        ILogger<RemoteBrick>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _executeBaseUrl = executeBaseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(executeBaseUrl));
        _logger = logger;

        Id = catalogEntry.Id;
        Name = catalogEntry.Name;
        Version = catalogEntry.Version;
        Icon = catalogEntry.Icon;
        Category = Enum.TryParse<BrickCategory>(catalogEntry.Category, true, out var cat) ? cat : BrickCategory.Control;
        Description = catalogEntry.Description;
        Interface = new BrickInterface
        {
            Inputs = catalogEntry.Interface.Inputs.Select(i => new BrickInputDefinition(i.Name, i.Type, i.Description, i.Required, i.Default)).ToList(),
            Outputs = catalogEntry.Interface.Outputs.Select(o => new BrickOutputDefinition(o.Name, o.Type, o.Description)).ToList()
        };
        Implementations = new BrickImplementations
        {
            Deterministic = catalogEntry.HasDeterministic ? new DeterministicImplementation { Id = "remote", Name = "Remote", Description = "Remote deterministic", Executor = "Remote" } : null,
            Agentic = catalogEntry.HasAgentic ? new AgenticImplementation { Id = "remote", Name = "Remote", Description = "Remote agentic", Executor = "Remote" } : null
        };
        Metadata = new BrickMetadata
        {
            Author = catalogEntry.Metadata.Author,
            License = catalogEntry.Metadata.License,
            Repository = catalogEntry.Metadata.Repository,
            UsageCount = catalogEntry.Metadata.UsageCount,
            LastUpdated = catalogEntry.Metadata.LastUpdated
        };
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var wireInput = BrickValueSerializer.ToWireDictionary(input);
        var request = new BrickExecuteRequestDto
        {
            WireFormatVersion = WireFormatVersion.Current,
            BrickId = Id,
            Implementation = implementation.ToString(),
            Input = wireInput,
            ExecutionContext = new ExecutionContextDto
            {
                CorrelationId = null,
                AgentId = context.AgentId,
                BehaviorId = context.BehaviorId,
                IsAirGapped = context.IsAirGapped,
                AuditMode = context.AuditMode,
                Provider = context.Provider,
                Variables = context.Variables != null ? new Dictionary<string, object>(context.Variables) : null
            }
        };

        var url = $"{_executeBaseUrl}/api/bricks/{Uri.EscapeDataString(Id)}/execute";
        _logger?.LogDebug("RemoteBrick executing {BrickId} at {Url}", Id, url);

        using var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<BrickExecuteResponseDto>(cancellationToken).ConfigureAwait(false);
        if (dto == null)
            throw new InvalidOperationException("Remote brick returned null response.");

        if (!dto.Success)
            throw new InvalidOperationException(dto.Error ?? "Remote brick execution failed.");

        return BrickValueSerializer.FromWireToBrickOutput(dto.Output, dto.Summary);
    }
}

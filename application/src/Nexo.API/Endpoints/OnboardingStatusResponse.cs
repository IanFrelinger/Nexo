using System.Text;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexo.Brick.Contracts;
using Nexo.Brick.Contracts.Capabilities;
using Nexo.Contracts;
using Nexo.Core.Application.Copilot.Models;
using Nexo.Core.Application.Copilot.Ports;
using Nexo.Core.Application.Product.Models;
using Nexo.Core.Application.Product.Ports;
using Nexo.Core.Application.Knowledge.Models;
using Nexo.Core.Application.Knowledge.Ports;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Application.Bricks;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.RuntimeStudio;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using Nexo.Infrastructure.Execution;
using Nexo.API.Security;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.API.Endpoints;

// ── Onboarding ──────────────────────────────────────────────────────

/// <summary>First-run onboarding status for the Director portal.</summary>
/// <remarks>
/// <see cref="MeaiOllamaBaseUrl"/> / <see cref="MeaiOllamaModel"/> report what the default (MEAI) model
/// path will actually dial, resolved by the same precedence as the client
/// (<c>NEXO_OLLAMA_*</c> env → <c>Nexo:Meai:*</c> → legacy <c>OLLAMA_*</c> env → defaults). The
/// <c>ollama</c> entry in <see cref="Providers"/> comes from the provider-factory path (legacy env only);
/// <c>scripts/prod-dry-run.sh</c> compares the two so a container that would dial its own loopback fails
/// the gate instead of "Ollama available" masking a connection-refused model path. Null when the MEAI
/// pipeline is opted out (<c>NEXO_USE_MEAI_PIPELINE=0</c>).
/// </remarks>
public sealed record OnboardingStatusResponse(
    bool IsFirstRun,
    bool ApiReachable,
    IReadOnlyList<ProviderStatus> Providers,
    bool HasCopilotTasks,
    bool HasDailies,
    string? ConfigPath,
    string? ActiveTrustPack,
    bool BuiltInAuthActive,
    bool BuiltInCredentialsConfigured,
    bool RequireAuthForCopilotReads,
    bool CopilotScopedKeyConfigured,
    string ResolvedTenantId,
    string? MeaiOllamaBaseUrl = null,
    string? MeaiOllamaModel = null);

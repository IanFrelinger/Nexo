using System.Text;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ashlar.Brick.Contracts;
using Ashlar.Brick.Contracts.Capabilities;
using Ashlar.Contracts;
using Ashlar.Core.Application.Copilot.Models;
using Ashlar.Core.Application.Copilot.Ports;
using Ashlar.Core.Application.Product.Models;
using Ashlar.Core.Application.Product.Ports;
using Ashlar.Core.Application.Knowledge.Models;
using Ashlar.Core.Application.Knowledge.Ports;
using Ashlar.Core.Application.Agent.UseCases.RunAgent;
using Ashlar.Core.Application.Bricks;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Core.Application.Validation.UseCases.RunValidation;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Registry;
using Ashlar.BackgroundAgents.RuntimeStudio;
using Ashlar.Infrastructure.Testing.ExecutionPlatform;
using Ashlar.Infrastructure.Execution;
using Ashlar.API.Security;
using Ashlar.Orchestration.Coordination;
using Ashlar.Orchestration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.API.Endpoints;

// ── Onboarding ──────────────────────────────────────────────────────

/// <summary>First-run onboarding status for the Director portal.</summary>
/// <remarks>
/// <see cref="MeaiOllamaBaseUrl"/> / <see cref="MeaiOllamaModel"/> report what the default (MEAI) model
/// path will actually dial, resolved by the same precedence as the client
/// (<c>ASHLAR_OLLAMA_*</c> env → <c>Ashlar:Meai:*</c> → legacy <c>OLLAMA_*</c> env → defaults). The
/// <c>ollama</c> entry in <see cref="Providers"/> comes from the provider-factory path (legacy env only);
/// <c>scripts/prod-dry-run.sh</c> compares the two so a container that would dial its own loopback fails
/// the gate instead of "Ollama available" masking a connection-refused model path. Null when the MEAI
/// pipeline is opted out (<c>ASHLAR_USE_MEAI_PIPELINE=0</c>).
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

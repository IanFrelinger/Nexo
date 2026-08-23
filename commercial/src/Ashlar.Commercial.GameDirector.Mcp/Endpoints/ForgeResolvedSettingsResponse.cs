using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using GameDirector.Mcp.Forge;
using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Descriptors;
using Ashlar.Commercial.GameDomain.Macros;
using Ashlar.Commercial.GameDomain.Mapping;
using Ashlar.Commercial.GameDomain.Materials;
using Ashlar.Commercial.GameDomain.Scoping;
using Ashlar.Commercial.GameDomain.Session;
using Ashlar.Commercial.GameDomain.Contracts;

namespace GameDirector.Mcp.Endpoints;

public sealed record ForgeResolvedSettingsResponse(
    ScopeContext Context,
    Dictionary<string, object> ResolvedSettings);

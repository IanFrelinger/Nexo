using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using GameDirector.Mcp.Forge;
using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Descriptors;
using Nexo.Commercial.GameDomain.Macros;
using Nexo.Commercial.GameDomain.Mapping;
using Nexo.Commercial.GameDomain.Materials;
using Nexo.Commercial.GameDomain.Scoping;
using Nexo.Commercial.GameDomain.Session;
using Nexo.Commercial.GameDomain.Contracts;

namespace GameDirector.Mcp.Endpoints;

public sealed record ForgeEngineManifestResponse(string Json);

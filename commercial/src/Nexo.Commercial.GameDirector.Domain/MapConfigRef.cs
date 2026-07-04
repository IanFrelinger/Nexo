using System.Text.Json;
using Nexo.Commercial.GameDomain.Session;

namespace GameDirector.Domain;
public sealed record MapConfigRef(string MapId, string Path, DateTimeOffset LastValidatedUtc);

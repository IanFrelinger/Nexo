using System.Text.Json;
using Nexo.Commercial.GameDomain.Session;

namespace GameDirector.Domain;

public sealed record BalanceSheetRef(string SheetId, string Path, DateTimeOffset LastSeenUtc);

# Geospatial Application Redundancy Analysis

## Executive Summary

This document identifies significant code redundancy across the geospatial application components. The analysis reveals **~60-70% code duplication** across services, controllers, and commands, with opportunities for substantial refactoring to improve maintainability and reduce bugs.

## 1. Service Layer Redundancy (High Priority)

### 1.1 Identical Service Patterns

**Files Affected:**
- `GeoTerrainService.cs`
- `GeoVectorService.cs`
- `WorldService.cs`

**Redundant Patterns:**

#### A. Job Creation Pattern (100% Duplicate)
All three services have identical job creation logic:
```csharp
var jobId = Guid.NewGuid().ToString("N");
var outputPath = Path.Combine(_outputDirectory, $"{jobId}.{request.Format}");
var job = new JobStatusResponse
{
    JobId = jobId,
    Status = "pending",
    Progress = 0,
    CreatedAt = DateTime.UtcNow
};
await _jobRepository.CreateJobAsync(job);
```

**Recommendation:** Extract to `BaseGeospatialService<TRequest>` or `JobManager` helper.

#### B. Async Task.Run Pattern (100% Duplicate)
All services use identical async processing:
```csharp
_ = Task.Run(async () =>
{
    try
    {
        job = job with { Status = "processing", Progress = 10 };
        await _jobRepository.UpdateJobAsync(job);
        // ... command execution ...
        job = job with { Status = "completed", Progress = 100, ... };
        await _jobRepository.UpdateJobAsync(job);
        // webhook handling
    }
    catch (Exception ex)
    {
        // identical error handling
    }
});
```

**Recommendation:** Extract to `JobProcessor<TRequest, TCommand>` base class or `IJobOrchestrator`.

#### C. Bounds Parsing (100% Duplicate)
All services parse bounds identically:
```csharp
var boundsParts = request.Bounds.Split(',');
if (boundsParts.Length != 4)
{
    throw new ArgumentException("Bounds must be in format: minLat,maxLat,minLon,maxLon");
}
var bounds = $"{boundsParts[0]},{boundsParts[1]},{boundsParts[2]},{boundsParts[3]}";
```

**Recommendation:** Extract to `BoundsParser` utility class or extension method.

#### D. Webhook Handling (100% Duplicate)
All services have identical webhook logic:
```csharp
if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
{
    await _webhookService.SendWebhookAsync(webhookUrl, jobId, "completed");
}
// ... and in catch block ...
```

**Recommendation:** Extract to `WebhookNotifier` helper or base service method.

#### E. GetJobStatusAsync & GetJobOutputPathAsync (100% Duplicate)
All three services have identical implementations:
```csharp
public async Task<JobStatusResponse?> GetJobStatusAsync(string jobId)
{
    return await _jobRepository.GetJobAsync(jobId);
}

public async Task<string?> GetJobOutputPathAsync(string jobId, string format)
{
    var status = await _jobRepository.GetJobAsync(jobId);
    if (status != null && status.Status == "completed")
    {
        return status.OutputPath;
    }
    return null;
}
```

**Recommendation:** Move to base class or `IJobService` (already exists but not used).

### 1.2 Service-Specific Differences

The only differences are:
- Output directory name (`geoterrain`, `geovector`, `world`)
- Command interface type (`IGeoTerrainCommand`, `IGeoVectorCommand`, `IWorldCommand`)
- Command method called (different signatures)

**Refactoring Opportunity:** Create generic `GeospatialService<TCommand, TRequest>` base class.

## 2. Controller Layer Redundancy (High Priority)

### 2.1 Identical Controller Patterns

**Files Affected:**
- `GeoTerrainController.cs`
- `GeoVectorController.cs`
- `WorldController.cs`

#### A. Generate/Extract Endpoint (90% Duplicate)
All controllers have nearly identical POST endpoints:
```csharp
[HttpPost("generate")] // or "extract"
public async Task<ActionResult<JobResponse>> Generate([FromBody] TRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    try
    {
        var jobId = await _service.GenerateAsync(request);
        return Accepted(new JobResponse { JobId = jobId, Status = "accepted" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error...");
        return StatusCode(500, new ErrorResponse { Message = ex.Message });
    }
}
```

**Recommendation:** Extract to `BaseGeospatialController<TService, TRequest>`.

#### B. GetJobStatus Endpoint (100% Duplicate)
All controllers have identical GET endpoints:
```csharp
[HttpGet("jobs/{jobId}")]
public async Task<ActionResult<JobStatusResponse>> GetJobStatus(string jobId)
{
    var status = await _service.GetJobStatusAsync(jobId);
    if (status == null)
        return NotFound(new ErrorResponse { Message = $"Job {jobId} not found" });
    return Ok(status);
}
```

**Recommendation:** Move to base controller.

#### C. SSE Progress Streaming (100% Duplicate)
All controllers have **identical** SSE implementation (~50 lines):
```csharp
[HttpGet("jobs/{jobId}/progress")]
public async Task GetJobProgress(string jobId, CancellationToken cancellationToken)
{
    Response.ContentType = "text/event-stream";
    Response.Headers["Cache-Control"] = "no-cache";
    Response.Headers["Connection"] = "keep-alive";
    
    var lastProgress = -1;
    var lastStatus = "";
    
    while (!cancellationToken.IsCancellationRequested)
    {
        var status = await _service.GetJobStatusAsync(jobId);
        // ... identical polling and streaming logic ...
    }
}
```

**Recommendation:** Extract to `SseProgressStreamer` middleware or base controller method.

#### D. Download Endpoint (95% Duplicate)
All controllers have nearly identical download logic:
```csharp
[HttpGet("jobs/{jobId}/download")]
public async Task<ActionResult> Download(string jobId, [FromQuery] string format = "...")
{
    var filePath = await _service.GetJobOutputPathAsync(jobId, format);
    if (filePath == null || !File.Exists(filePath))
        return NotFound(new ErrorResponse { Message = $"Output file not found..." });
    
    var contentType = GetContentType(format); // Different per controller
    var fileName = Path.GetFileName(filePath);
    return PhysicalFile(filePath, contentType, fileName);
}
```

**Recommendation:** Extract to base controller with virtual `GetContentType` method.

### 2.2 Controller-Specific Differences

Only differences:
- Route prefix (`/api/v1/geoterrain`, `/api/v1/geovector`, `/api/v1/world`)
- Service type injected
- `GetContentType` implementation (different format mappings)
- `GeoTerrainController` has additional validation endpoints

**Refactoring Opportunity:** Create `BaseGeospatialController<TService>` with template method pattern.

## 3. Command Layer Redundancy (Medium Priority)

### 3.1 ParseBounds Method (100% Duplicate)

**Files Affected:**
- `GeoTerrainCommand.cs` (line 1482)
- `GeoVectorCommand.cs` (line 743)
- `WorldCommand.cs` (line 1424)

All three commands have **identical** `ParseBounds` implementations:
```csharp
private static GeoBounds ParseBounds(string text)
{
    var parts = text.Split(',');
    if (parts.Length != 4)
        throw new ArgumentException("Bounds must be: minLat,minLon,maxLat,maxLon");
    
    return new GeoBounds
    {
        MinLatitude = new Latitude(double.Parse(parts[0])),
        MinLongitude = new Longitude(double.Parse(parts[1])),
        MaxLatitude = new Latitude(double.Parse(parts[2])),
        MaxLongitude = new Longitude(double.Parse(parts[3]))
    };
}
```

**Recommendation:** Move to `GeoBounds.Parse(string)` static method or `BoundsParser` utility.

### 3.2 BuildElevationProvider Method (95% Duplicate)

**Files Affected:**
- `GeoTerrainCommand.cs`
- `GeoVectorCommand.cs`
- `WorldCommand.cs`

All three commands have nearly identical provider building logic:
```csharp
private IElevationProvider BuildElevationProvider(
    string provider,
    string? localRoot,
    string? srtmBaseUrl,
    bool persistDownloads,
    bool enableCache,
    bool airGapped)
{
    provider = (provider ?? "echo").Trim().ToLowerInvariant();
    
    if (airGapped && provider is "http" or "srtmhttp" or "hybrid")
        provider = "local";
    
    IElevationProvider inner = provider switch
    {
        "echo" => new EchoElevationProvider(),
        "local" => new LocalFileElevationProvider(localRoot),
        "http" or "srtmhttp" => new SrtmHttpElevationProvider(...),
        "hybrid" => BuildHybrid(...),
        _ => throw new InvalidOperationException(...)
    };
    
    return enableCache ? new CachedElevationProvider(inner) : inner;
}
```

**Recommendation:** Extract to `ElevationProviderFactory` class.

### 3.3 BuildVectorProvider Method (90% Duplicate)

**Files Affected:**
- `GeoVectorCommand.cs`
- `WorldCommand.cs`

Both commands have nearly identical vector provider building logic with minor variations.

**Recommendation:** Extract to `VectorProviderFactory` class.

### 3.4 BuildHybrid Methods (95% Duplicate)

**Files Affected:**
- `GeoTerrainCommand.cs` (`BuildHybrid`)
- `GeoVectorCommand.cs` (`BuildHybrid`)
- `WorldCommand.cs` (`BuildHybridElevation`)

All have identical hybrid provider construction logic.

**Recommendation:** Consolidate into factory classes.

### 3.5 ResolveMapboxToken Method (100% Duplicate)

**Files Affected:**
- `GeoVectorCommand.cs`
- `WorldCommand.cs`

Identical token resolution:
```csharp
private static string ResolveMapboxToken(string? token)
{
    token = string.IsNullOrWhiteSpace(token) 
        ? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN") 
        : token;
    // ... validation ...
}
```

**Recommendation:** Move to `MapboxTokenResolver` utility class.

### 3.6 Origin Calculation (100% Duplicate)

**Files Affected:**
- `GeoVectorCommand.cs` (appears 3 times)
- `WorldCommand.cs`

Identical origin calculation:
```csharp
var origin = new GeoPoint
{
    Latitude = new Latitude((geoBounds.MinLatitude.Degrees + geoBounds.MaxLatitude.Degrees) * 0.5),
    Longitude = new Longitude((geoBounds.MinLongitude.Degrees + geoBounds.MaxLongitude.Degrees) * 0.5)
};
```

**Recommendation:** Add `GeoBounds.Center` property.

## 4. Program.cs Redundancy (Low Priority)

### 4.1 Duplicate HTTP Client Registration

**File:** `Nexo.API/Program.cs` (lines 50-57)

```csharp
// Register HTTP clients
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("geoterrain.srtm");
builder.Services.AddHttpClient("geovector.mapbox");

// Register geospatial services (reuse CLI configuration pattern)
builder.Services.AddHttpClient();  // DUPLICATE
builder.Services.AddHttpClient("geoterrain.srtm");  // DUPLICATE
builder.Services.AddHttpClient("geovector.mapbox");  // DUPLICATE
```

**Recommendation:** Remove duplicate registrations.

## 5. SDK Client Redundancy (Low Priority)

### 5.1 Similar Client Patterns

**Files Affected:**
- `GeoTerrainClient.cs`
- `GeoVectorClient.cs`
- `WorldClient.cs`

All clients have similar structure:
- Constructor with provider/logger
- Extract/Generate method
- Export method (with similar validation)

**Recommendation:** Consider base client class if more methods are added.

## 6. Validation Redundancy (Medium Priority)

### 6.1 Bounds Validation

Bounds validation logic appears in multiple places:
- Service layer (string parsing)
- Command layer (ParseBounds)
- Controller layer (some validation endpoints)

**Recommendation:** Centralize in `GeoBounds` class with `TryParse` and validation methods.

## 7. Error Handling Redundancy (Medium Priority)

### 7.1 Identical Try-Catch Patterns

All services use identical error handling:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing ... job {JobId}", jobId);
    job = job with
    {
        Status = "failed",
        ErrorMessage = ex.Message,
        CompletedAt = DateTime.UtcNow
    };
    await _jobRepository.UpdateJobAsync(job);
    
    if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
    {
        await _webhookService.SendWebhookAsync(webhookUrl, jobId, "failed", ex.Message);
    }
}
```

**Recommendation:** Extract to `JobErrorHandler` or base service method.

## 8. Summary of Redundancy Metrics

### Code Duplication Estimates

| Component | Duplication % | Lines Duplicated | Refactoring Priority |
|-----------|--------------|------------------|---------------------|
| Services | ~70% | ~200 lines | **HIGH** |
| Controllers | ~80% | ~250 lines | **HIGH** |
| Commands | ~40% | ~300 lines | **MEDIUM** |
| Program.cs | ~5% | ~10 lines | **LOW** |
| SDK Clients | ~30% | ~50 lines | **LOW** |
| **TOTAL** | **~55%** | **~810 lines** | |

### Refactoring Impact

**High Priority Refactorings:**
1. Create `BaseGeospatialService<TCommand, TRequest>` - **Eliminates ~200 lines**
2. Create `BaseGeospatialController<TService>` - **Eliminates ~250 lines**
3. Extract `JobOrchestrator` - **Eliminates ~150 lines**
4. Extract `BoundsParser` utility - **Eliminates ~50 lines**
5. Extract `SseProgressStreamer` - **Eliminates ~150 lines**

**Medium Priority Refactorings:**
1. Extract `ElevationProviderFactory` - **Eliminates ~100 lines**
2. Extract `VectorProviderFactory` - **Eliminates ~80 lines**
3. Add `GeoBounds.Parse()` and `GeoBounds.Center` - **Eliminates ~30 lines**

**Total Potential Reduction:** ~1,010 lines of duplicate code

## 9. Recommended Refactoring Strategy

### Phase 1: Extract Common Utilities (Low Risk)
1. Create `BoundsParser` utility class
2. Add `GeoBounds.Parse()` and `GeoBounds.Center` properties
3. Extract `MapboxTokenResolver` utility
4. Remove duplicate HTTP client registrations

### Phase 2: Extract Factories (Medium Risk)
1. Create `ElevationProviderFactory`
2. Create `VectorProviderFactory`
3. Update commands to use factories

### Phase 3: Service Layer Refactoring (High Impact)
1. Create `BaseGeospatialService<TCommand, TRequest>`
2. Extract `JobOrchestrator` for async processing
3. Extract `WebhookNotifier` helper
4. Refactor all three services to inherit from base

### Phase 4: Controller Layer Refactoring (High Impact)
1. Create `BaseGeospatialController<TService>`
2. Extract `SseProgressStreamer` helper
3. Refactor all three controllers to inherit from base

### Phase 5: Consolidate Job Management (Medium Risk)
1. Ensure `IJobService` is used consistently
2. Move common job methods to `IJobService` implementation

## 10. Benefits of Refactoring

1. **Maintainability:** Single source of truth for common patterns
2. **Bug Fixes:** Fix once, apply everywhere
3. **Testing:** Test common logic once
4. **Consistency:** Guaranteed consistent behavior
5. **Code Size:** Reduce codebase by ~1,000 lines
6. **Onboarding:** Easier for new developers to understand

## 11. Risks and Mitigation

**Risks:**
- Breaking changes during refactoring
- Over-abstraction making code harder to understand
- Performance impact from additional abstraction layers

**Mitigation:**
- Comprehensive test coverage before refactoring
- Incremental refactoring with tests after each step
- Keep abstractions simple and focused
- Profile performance-critical paths

## 12. Next Steps

1. Review this analysis with the team
2. Prioritize refactoring phases based on current development needs
3. Create tickets for each refactoring phase
4. Start with Phase 1 (low risk, quick wins)
5. Measure code reduction and maintainability improvements

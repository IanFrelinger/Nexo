# Geospatial App - Quick Wins Action Plan

**Created:** January 27, 2026  
**Status:** Ready to implement

This document identifies **immediate quick wins** - features that are already implemented but just need to be exposed or completed.

---

## 🚀 Quick Win #1: Expose Roads/Water Endpoints (1-2 days)

### Current State
✅ **Code already exists!** `GeoVectorService.ExtractFeaturesAsync()` already handles roads and water:
- Lines 68-94: Roads extraction
- Lines 95-121: Water extraction

### What's Missing
❌ The API only exposes a generic `/extract` endpoint that defaults to buildings. Need dedicated endpoints.

### Action Items
1. **Add dedicated endpoints to `GeoVectorController.cs`:**
   ```csharp
   [HttpPost("extract/roads")]
   public async Task<ActionResult<JobResponse>> ExtractRoads([FromBody] VectorExtractionRequest request)
   {
       request.FeatureKind = "road";
       return await ExtractFeatures(request);
   }

   [HttpPost("extract/water")]
   public async Task<ActionResult<JobResponse>> ExtractWater([FromBody] VectorExtractionRequest request)
   {
       request.FeatureKind = "water";
       return await ExtractFeatures(request);
   }
   ```

2. **Update API documentation** (`docs/API_REFERENCE.md`)

3. **Add Swagger examples**

**Files to Modify:**
- `src/Nexo.API/Controllers/GeoVectorController.cs`
- `docs/API_REFERENCE.md`

**Estimated Effort:** 1-2 days

---

## 🚀 Quick Win #2: Complete Validation Endpoints (2-3 days)

### Current State
✅ **Endpoints exist but are stubbed!** 
- `GeoTerrainController.ValidateMesh()` - Returns 501 Not Implemented
- `GeoTerrainController.ValidateIntegrity()` - Returns 501 Not Implemented

✅ **Validation infrastructure exists:**
- `DataIntegrityChecker.DetectCorruption()` - Fully implemented
- `MeshQualityMetrics` - Fully implemented
- Used in CLI commands already

### What's Missing
❌ Need to implement the actual validation logic in the API endpoints

### Action Items
1. **Implement `ValidateIntegrity` endpoint:**
   ```csharp
   [HttpPost("validate-integrity")]
   public async Task<ActionResult<ValidationResponse>> ValidateIntegrity(
       [FromBody] IntegrityValidationRequest request)
   {
       // Load elevation grid from bounds
       var grid = await LoadElevationGridAsync(request.Bounds, request.Provider);
       
       // Run validation
       var report = DataIntegrityChecker.DetectCorruption(grid, _logger);
       
       return Ok(new ValidationResponse 
       { 
           IsValid = !report.IsCorrupted,
           Issues = report.Issues 
       });
   }
   ```

2. **Implement `ValidateMesh` endpoint:**
   ```csharp
   [HttpPost("validate-mesh")]
   public async Task<ActionResult<ValidationResponse>> ValidateMesh(
       [FromBody] MeshValidationRequest request)
   {
       // Load mesh from file
       var mesh = await LoadMeshAsync(request.MeshPath);
       
       // Run quality analysis
       var quality = MeshQualityMetrics.ComputeTriangleQuality(mesh);
       var issues = new List<string>();
       
       if (quality.SliverTriangleCount > 0)
           issues.Add($"{quality.SliverTriangleCount} sliver triangles detected");
       
       // ... more checks
       
       return Ok(new ValidationResponse 
       { 
           IsValid = issues.Count == 0,
           Issues = issues 
       });
   }
   ```

3. **Add helper methods to load grids/meshes**

**Files to Modify:**
- `src/Nexo.API/Controllers/GeoTerrainController.cs`
- `src/Nexo.API/Services/GeoTerrainService.cs` (may need to add helper methods)

**Estimated Effort:** 2-3 days

---

## 🚀 Quick Win #3: Wire Up CLI Validation Flags (2-3 days)

### Current State
✅ **Flag exists:** `--validate-integrity` in `Program.cs` line 392
✅ **Infrastructure exists:** Used in `GeoTerrainCommand.BoundsToObjAsync()` lines 465-486
✅ **Mesh quality exists:** `MeshQualityMetrics` is fully implemented

### What's Missing
❌ Flag not wired to all commands
❌ `--mesh-quality-report` flag doesn't exist
❌ Partial failure reporting not in CLI output

### Action Items
1. **Add `--mesh-quality-report` flag to all geospatial commands:**
   - `bounds-to-obj`
   - `tile-to-obj`
   - `terrain-rgb-tile-to-obj`

2. **Ensure `--validate-integrity` is passed through all command handlers:**
   - Check `GeoTerrainCommand` methods
   - Check `GeoVectorCommand` methods
   - Check `WorldCommand` methods

3. **Expose partial failure reporting in JSON output:**
   ```csharp
   if (json)
   {
       var result = new
       {
           success = true,
           partialFailures = new
           {
               tilesDownloaded = 8,
               tilesTotal = 10,
               featuresExtracted = 1234
           }
       };
       Console.WriteLine(JsonSerializer.Serialize(result));
   }
   ```

**Files to Modify:**
- `src/Nexo.CLI/Program.cs` (add flags)
- `src/Nexo.CLI/Commands/GeoTerrain/GeoTerrainCommand.cs`
- `src/Nexo.CLI/Commands/GeoVector/GeoVectorCommand.cs`
- `src/Nexo.CLI/Commands/World/WorldCommand.cs`

**Estimated Effort:** 2-3 days

---

## 🚀 Quick Win #4: Add Job Cleanup Service (2-3 days)

### Current State
✅ **Job storage exists:** `IJobRepository` interface
❌ **No cleanup:** Jobs accumulate indefinitely

### What's Missing
❌ Background service to clean up old jobs
❌ Configuration for retention period

### Action Items
1. **Create `JobCleanupService`:**
   ```csharp
   public class JobCleanupService : BackgroundService
   {
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               var cutoff = DateTime.UtcNow.AddDays(-7); // 7 day retention
               await _jobRepository.DeleteJobsOlderThanAsync(cutoff);
               await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
           }
       }
   }
   ```

2. **Register in DI:**
   ```csharp
   services.AddHostedService<JobCleanupService>();
   ```

3. **Add configuration:**
   ```json
   {
     "JobRetention": {
       "Days": 7,
       "CleanupIntervalHours": 1
     }
   }
   ```

**Files to Create:**
- `src/Nexo.API/Services/JobCleanupService.cs`

**Files to Modify:**
- `src/Nexo.API/Program.cs` (register service)
- `appsettings.json` (add config)

**Estimated Effort:** 2-3 days

---

## 🚀 Quick Win #5: Implement Persistent Job Storage (1 week)

### Current State
✅ **Interface exists:** `IJobRepository`
❌ **In-memory only:** `InMemoryJobRepository` loses data on restart

### What's Missing
❌ SQLite implementation
❌ Database schema
❌ Migration from in-memory

### Action Items
1. **Create SQLite job repository:**
   ```csharp
   public class SqliteJobRepository : IJobRepository
   {
       private readonly string _connectionString;
       
       public async Task<string> CreateJobAsync(string jobType, string outputPath)
       {
           // Insert into jobs table
       }
       
       public async Task<JobStatusResponse> GetJobAsync(string jobId)
       {
           // Query from jobs table
       }
       
       // ... other methods
   }
   ```

2. **Create database schema:**
   ```sql
   CREATE TABLE jobs (
       job_id TEXT PRIMARY KEY,
       job_type TEXT NOT NULL,
       status TEXT NOT NULL,
       progress INTEGER DEFAULT 0,
       output_path TEXT,
       error_message TEXT,
       created_at TEXT NOT NULL,
       completed_at TEXT
   );
   ```

3. **Register in DI:**
   ```csharp
   services.AddSingleton<IJobRepository, SqliteJobRepository>();
   ```

**Files to Create:**
- `src/Nexo.API/Infrastructure/SqliteJobRepository.cs`
- `src/Nexo.API/Infrastructure/Migrations/001_CreateJobsTable.sql`

**Files to Modify:**
- `src/Nexo.API/Program.cs` (register repository)

**Estimated Effort:** 1 week

---

## 📊 Implementation Priority

### Week 1: Critical Exposures
1. ✅ Expose roads/water endpoints (1-2 days)
2. ✅ Complete validation endpoints (2-3 days)
3. ✅ Wire up CLI validation flags (2-3 days)

**Total: 5-8 days**

### Week 2: Production Readiness
4. ✅ Add job cleanup service (2-3 days)
5. ✅ Implement persistent job storage (1 week)

**Total: 1.5-2 weeks**

---

## 🎯 Success Criteria

After completing these quick wins:

1. **API Completeness:**
   - ✅ Roads/water extraction endpoints available
   - ✅ Validation endpoints functional
   - ✅ Jobs persist across restarts

2. **CLI Usability:**
   - ✅ Validation flags work on all commands
   - ✅ Quality reports accessible
   - ✅ Partial failures visible in output

3. **Production Readiness:**
   - ✅ Jobs don't accumulate indefinitely
   - ✅ Data survives server restarts
   - ✅ Validation catches issues early

---

## 🔍 Code Locations Reference

### API Controllers
- `src/Nexo.API/Controllers/GeoVectorController.cs` - Vector extraction
- `src/Nexo.API/Controllers/GeoTerrainController.cs` - Terrain generation & validation
- `src/Nexo.API/Controllers/WorldController.cs` - World bundles

### Services
- `src/Nexo.API/Services/GeoVectorService.cs` - Vector extraction logic (roads/water already here!)
- `src/Nexo.API/Services/GeoTerrainService.cs` - Terrain generation logic
- `src/Nexo.API/Services/WorldService.cs` - World bundle logic

### CLI Commands
- `src/Nexo.CLI/Commands/GeoTerrain/GeoTerrainCommand.cs` - Terrain CLI
- `src/Nexo.CLI/Commands/GeoVector/GeoVectorCommand.cs` - Vector CLI
- `src/Nexo.CLI/Commands/World/WorldCommand.cs` - World CLI
- `src/Nexo.CLI/Program.cs` - CLI setup & flags

### Validation Infrastructure
- `src/Nexo.Adapters.GeoTerrain/Validation/DataIntegrityChecker.cs` - Integrity checks
- `src/Nexo.GeoTerrain/MeshQualityMetrics.cs` - Mesh quality analysis
- `src/Nexo.GeoTerrain/MeshQualityAnalyzer.cs` - Quality analyzer

### Job Management
- `src/Nexo.API/Infrastructure/IJobRepository.cs` - Job storage interface
- `src/Nexo.API/Infrastructure/InMemoryJobRepository.cs` - Current implementation

---

## 💡 Key Insights

1. **Most features are already implemented** - just need exposure
2. **Roads/water extraction** is the easiest win (code exists!)
3. **Validation endpoints** are stubbed - need implementation
4. **CLI flags** exist but not fully wired
5. **Job persistence** is the biggest gap for production

---

## 🚦 Next Steps

1. **Start with Quick Win #1** (roads/water endpoints) - easiest, highest impact
2. **Then Quick Win #2** (validation endpoints) - high value
3. **Then Quick Win #3** (CLI flags) - improves UX
4. **Then Quick Win #4** (job cleanup) - prevents issues
5. **Finally Quick Win #5** (persistent storage) - production requirement

**Estimated Total Time:** 2-3 weeks for all quick wins

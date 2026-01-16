using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Adapters;
using Nexo.Agents.AutonomousDev.Bricks;
using Nexo.Agents.AutonomousDev.Configuration;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Agents.UniversalTester;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text.Json;

namespace Nexo.Agents.AutonomousDev;

/// <summary>
/// Autonomous Development Agent that builds, tests with mock users, and iterates until complete.
/// </summary>
public class AutonomousDevAgent
{
    private readonly IProviderFactory _providerFactory;
    private readonly UniversalTesterAgent _tester;
    private readonly ILogger<AutonomousDevAgent>? _logger;
    private readonly IApprovalCallback? _approvalCallback;
    private readonly ISessionStore? _sessionStore;
    
    public AutonomousDevAgent(
        IProviderFactory providerFactory,
        UniversalTesterAgent tester,
        ILogger<AutonomousDevAgent>? logger = null,
        IApprovalCallback? approvalCallback = null,
        ISessionStore? sessionStore = null)
    {
        _providerFactory = providerFactory;
        _tester = tester;
        _logger = logger;
        _approvalCallback = approvalCallback;
        _sessionStore = sessionStore;
    }
    
    /// <summary>
    /// Executes a development task autonomously.
    /// </summary>
    public async Task<DevelopmentSession> ExecuteAsync(
        DevTaskConfig config,
        IExecutionContext context,
        CancellationToken ct = default)
    {
        return await ExecuteInternalAsync(config, context, resumeSession: null, ct);
    }

    /// <summary>
    /// Executes a development task, resuming from a previously saved session (best-effort).
    /// </summary>
    public Task<DevelopmentSession> ExecuteAsync(
        DevTaskConfig config,
        IExecutionContext context,
        DevelopmentSession resumeSession,
        CancellationToken ct = default)
    {
        return ExecuteInternalAsync(config, context, resumeSession, ct);
    }

    private async Task<DevelopmentSession> ExecuteInternalAsync(
        DevTaskConfig config,
        IExecutionContext context,
        DevelopmentSession? resumeSession,
        CancellationToken ct)
    {
        _logger?.LogInformation("Starting autonomous development: {Task}", config.Task);
        
        // Create project adapter
        var project = CreateProjectAdapter(config.ProjectPath, config.ProjectType);
        await project.InitializeAsync(ct);
        
        // Infer test target if not provided
        var testTarget = config.TestTarget ?? await project.GetTestTargetAsync(ct);
        
        // Initialize bricks - create logger factory
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var specBrick = new SpecificationBrick(_providerFactory, loggerFactory.CreateLogger<SpecificationBrick>());
        var planBrick = new PlanningBrick(_providerFactory, loggerFactory.CreateLogger<PlanningBrick>());
        var genBrick = new GenerationBrick(_providerFactory, loggerFactory.CreateLogger<GenerationBrick>());
        var integrationBrick = new IntegrationBrick(loggerFactory.CreateLogger<IntegrationBrick>());
        var buildBrick = new BuildBrick(loggerFactory.CreateLogger<BuildBrick>());
        var testBrick = new TestingBrick(_tester, _providerFactory, loggerFactory.CreateLogger<TestingBrick>());
        var analysisBrick = new AnalysisBrick(_providerFactory, loggerFactory.CreateLogger<AnalysisBrick>());
        
        // Session state (resume if provided)
        var session = resumeSession ?? new DevelopmentSession
        {
            Task = config.Task,
            ProjectPath = config.ProjectPath,
            StartTime = DateTime.UtcNow,
            Status = SessionStatus.InProgress,
            EndTime = DateTime.MinValue
        };
        session = session with { Status = SessionStatus.InProgress };
        await PersistAsync(session, ct);
        
        // ======== PHASE 1: UNDERSTAND ========
        _logger?.LogInformation("Phase 1: Understanding requirements...");

        Specification specification;
        if (session.Specification != null)
        {
            specification = session.Specification;
        }
        else
        {
            var specInput = new BrickInput();
            specInput.Set("task", config.Task);
            if (!string.IsNullOrEmpty(config.DetailedSpec))
                specInput.Set("detailedSpec", config.DetailedSpec);
            specInput.Set("references", config.References ?? Array.Empty<string>());
            if (!string.IsNullOrEmpty(config.AcceptanceCriteria))
                specInput.Set("acceptanceCriteria", config.AcceptanceCriteria);
            specInput.Set("project", project);
            
            var specResult = await specBrick.ExecuteAsync(specInput, ImplementationType.Agentic, context, ct);
            specification = specResult.Get<Specification>("specification");
            session = session with { Specification = specification };
            await PersistAsync(session, ct);
        }
        
        // Check for open questions
        if (specification.OpenQuestions.Count > 0 && config.Autonomy == AutonomyLevel.Supervised)
        {
            _logger?.LogWarning("Open questions need resolution");
            
            if (_approvalCallback != null)
            {
                var approved = await _approvalCallback.ApproveOpenQuestionsAsync(specification, ct);
                if (!approved)
                {
                    return session with { Status = SessionStatus.AwaitingInput, EndTime = DateTime.UtcNow };
                }
            }
            else
            {
                return session with { Status = SessionStatus.AwaitingInput, EndTime = DateTime.UtcNow };
            }
        }
        
        // ======== PHASE 2: PLAN ========
        _logger?.LogInformation("Phase 2: Creating development plan...");

        DevelopmentPlan plan;
        if (session.Plan != null)
        {
            plan = session.Plan;
        }
        else
        {
            var planInput = new BrickInput();
            planInput.Set("specification", specification);
            planInput.Set("project", project);
            planInput.Set("testPersona", config.TestPersona);
            
            var planResult = await planBrick.ExecuteAsync(planInput, ImplementationType.Agentic, context, ct);
            plan = planResult.Get<DevelopmentPlan>("plan");
            session = session with { Plan = plan };
            await PersistAsync(session, ct);
        }
        
        // Request approval for plan in supervised mode
        if (config.Autonomy == AutonomyLevel.Supervised && _approvalCallback != null)
        {
            var approved = await _approvalCallback.ApprovePlanAsync(plan, ct);
            if (!approved)
            {
                return session with { Status = SessionStatus.AwaitingInput, EndTime = DateTime.UtcNow };
            }
        }
        
        // ======== PHASE 3: ITERATION LOOP ========
        var previousArtifacts = session.Iterations.SelectMany(i => i.Artifacts).ToList();
        TestFeedback? lastFeedback = session.Iterations.LastOrDefault()?.Feedback;
        
        var startIteration = session.Iterations.Count + 1;
        for (int iteration = startIteration; iteration <= config.MaxIterations; iteration++)
        {
            _logger?.LogInformation("=== Iteration {Iteration} of {Max} ===", iteration, config.MaxIterations);
            
            var iterationArtifacts = new List<GeneratedArtifact>();

            // ---- GENERATE ----
            _logger?.LogInformation("Generating code...");
            
            var updatedTasks = plan.Tasks.ToList();
            for (var taskIndex = 0; taskIndex < updatedTasks.Count; taskIndex++)
            {
                var task = updatedTasks[taskIndex];
                if (task.Status != Nexo.Agents.AutonomousDev.Models.TaskStatus.Pending) continue;

                var genInput = new BrickInput();
                genInput.Set("task", task);
                genInput.Set("plan", plan);
                genInput.Set("project", project);
                genInput.Set("previousArtifacts", previousArtifacts.ToArray());
                if (lastFeedback != null)
                    genInput.Set("previousFeedback", lastFeedback);
                
                var genResult = await genBrick.ExecuteAsync(genInput, ImplementationType.Agentic, context, ct);
                var artifacts = genResult.Get<GeneratedArtifact[]>("artifacts");
                iterationArtifacts.AddRange(artifacts);
                previousArtifacts.AddRange(artifacts);

                // Mark task complete for this iteration
                updatedTasks[taskIndex] = task with
                {
                    Status = Nexo.Agents.AutonomousDev.Models.TaskStatus.Completed,
                    Attempts = task.Attempts + 1
                };
            }
            
            plan = plan with { Tasks = updatedTasks };
            session = session with { Plan = plan };
            await PersistAsync(session, ct);
            
            // ---- INTEGRATE ----
            _logger?.LogInformation("Applying changes...");
            
            // Request approval before applying changes in supervised mode
            if (config.Autonomy == AutonomyLevel.Supervised && _approvalCallback != null && iterationArtifacts.Count > 0)
            {
                var approved = await _approvalCallback.ApproveChangesAsync(iterationArtifacts, ct);
                if (!approved)
                {
                    _logger?.LogInformation("Changes not approved, skipping iteration");
                    continue;
                }
            }
            
            var intInput = new BrickInput();
            intInput.Set("artifacts", iterationArtifacts.ToArray());
            intInput.Set("project", project);
            intInput.Set("createBackup", iteration == 1);
            
            var intResult = await integrationBrick.ExecuteAsync(intInput, ImplementationType.Deterministic, context, ct);
            var integrationResult = intResult.Get<IntegrationResult>("result");
            
            if (!integrationResult.Success)
            {
                _logger?.LogError("Integration failed");
                continue;
            }
            
            // ---- BUILD ----
            _logger?.LogInformation("Building project...");
            
            var buildInput = new BrickInput();
            buildInput.Set("project", project);
            buildInput.Set("fullRebuild", iteration == 1);
            
            var buildResult = await buildBrick.ExecuteAsync(buildInput, ImplementationType.Deterministic, context, ct);
            var build = buildResult.Get<BuildResult>("result");
            
            if (!build.Success)
            {
                _logger?.LogError("Build failed: {Errors}", string.Join(", ", build.Errors.Take(3)));
                
                // Create feedback from build errors
                lastFeedback = new TestFeedback
                {
                    IterationNumber = iteration,
                    OverallSuccess = false,
                    AcceptanceScore = 0,
                    UserExperience = new MockUserExperience
                    {
                        Narrative = "Could not test - build failed",
                        OverallReaction = "N/A"
                    },
                    Issues = build.Errors.Select(e => new DiscoveredIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = IssueType.Bug,
                        Severity = IssueSeverity.Critical,
                        Description = e,
                        SuggestedFix = "Fix compilation error"
                    }).ToList()
                };
                continue;
            }
            
            // ---- TEST (Mock User) ----
            _logger?.LogInformation("Testing with mock user ({Persona})...", config.TestPersona);
            
            var testInput = new BrickInput();
            testInput.Set("specification", specification);
            testInput.Set("strategy", plan.TestStrategy);
            testInput.Set("testTarget", testTarget);
            testInput.Set("iterationNumber", iteration);
            
            var testResult = await testBrick.ExecuteAsync(testInput, ImplementationType.Agentic, context, ct);
            lastFeedback = testResult.Get<TestFeedback>("feedback");
            
            session = session with
            {
                Iterations = session.Iterations.Append(new IterationRecord
                {
                    Number = iteration,
                    Feedback = lastFeedback,
                    Artifacts = iterationArtifacts.ToList()
                }).ToList()
            };
            await PersistAsync(session, ct);
            
            _logger?.LogInformation("Test results: Score={Score}%, Issues={Issues}",
                lastFeedback.AcceptanceScore, lastFeedback.Issues.Count);
            
            // ---- ANALYZE ----
            _logger?.LogInformation("Analyzing feedback...");
            
            var analysisInput = new BrickInput();
            analysisInput.Set("feedback", lastFeedback);
            analysisInput.Set("specification", specification);
            analysisInput.Set("plan", plan);
            analysisInput.Set("currentArtifacts", previousArtifacts.ToArray());
            analysisInput.Set("currentIteration", iteration);
            analysisInput.Set("maxIterations", config.MaxIterations);
            
            var analysisResult = await analysisBrick.ExecuteAsync(analysisInput, ImplementationType.Agentic, context, ct);
            var decision = analysisResult.Get<IterationDecision>("decision");
            
            _logger?.LogInformation("Decision: {Decision} - {Reasoning}", decision.Decision, decision.Reasoning);
            
            // ---- ACT ON DECISION ----
            switch (decision.Decision)
            {
                case DecisionType.Complete:
                    _logger?.LogInformation("✅ Development complete!");
                    session = session with { EndTime = DateTime.UtcNow, Status = SessionStatus.Completed };
                    
                    if (config.Autonomy == AutonomyLevel.FullyAutonomous)
                    {
                        await project.CommitChangesAsync($"feat: {specification.Summary}", ct);
                    }
                    
                    return session;
                    
                case DecisionType.Iterate:
                    // Update plan with fixes and continue
                    plan = decision.UpdatedPlan ?? plan with
                    {
                        Tasks = UpdateTasksWithFixes(plan.Tasks, decision.PlannedFixes)
                    };
                    session = session with { Plan = plan };
                    await PersistAsync(session, ct);
                    break;
                    
                case DecisionType.NeedsClarification:
                    _logger?.LogWarning("Need human input");
                    
                    if (_approvalCallback != null && config.Autonomy == AutonomyLevel.Supervised)
                    {
                        var clarification = await _approvalCallback.GetClarificationAsync(decision.Reasoning, ct);
                        if (string.IsNullOrEmpty(clarification))
                        {
                            return session with { Status = SessionStatus.AwaitingInput, EndTime = DateTime.UtcNow };
                        }
                        // Use clarification to update plan (simplified - in production would be more sophisticated)
                        _logger?.LogInformation("Received clarification, continuing...");
                        continue;
                    }
                    
                    return session with { Status = SessionStatus.AwaitingInput, EndTime = DateTime.UtcNow };
                    
                case DecisionType.Stuck:
                case DecisionType.NeedsRedesign:
                    _logger?.LogError("❌ Development stuck: {Reason}", decision.AbortReason);
                    return session with { EndTime = DateTime.UtcNow, Status = SessionStatus.Failed };
                    
                case DecisionType.MaxIterationsReached:
                    _logger?.LogWarning("⚠️ Max iterations reached");
                    return session with { EndTime = DateTime.UtcNow, Status = SessionStatus.MaxIterations };
            }
        }
        
        // Ran out of iterations
        return session with { EndTime = DateTime.UtcNow, Status = SessionStatus.MaxIterations };
    }

    private async Task PersistAsync(DevelopmentSession session, CancellationToken ct)
    {
        if (_sessionStore == null) return;
        try
        {
            await _sessionStore.SaveAsync(session, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to persist development session");
        }
    }
    
    private static IProjectAdapter CreateProjectAdapter(string path, ProjectType? type)
    {
        var projectType = type ?? ProjectTypeDetector.InferProjectType(path);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        return projectType switch
        {
            ProjectType.UnityGame => new UnityProjectAdapter(path, loggerFactory.CreateLogger<UnityProjectAdapter>()),
            ProjectType.DotNetApp or ProjectType.DotNetApi => new DotNetProjectAdapter(path, loggerFactory.CreateLogger<DotNetProjectAdapter>()),
            ProjectType.ReactApp or ProjectType.VueApp or ProjectType.NextJs => new WebProjectAdapter(path, loggerFactory.CreateLogger<WebProjectAdapter>()),
            _ => new GenericProjectAdapter(path, loggerFactory.CreateLogger<GenericProjectAdapter>())
        };
    }
    
    private static IReadOnlyList<DevelopmentTask> UpdateTasksWithFixes(
        IReadOnlyList<DevelopmentTask> tasks,
        IReadOnlyList<PlannedFix> fixes)
    {
        var newTasks = fixes.Select(fix => new DevelopmentTask
        {
            Id = $"fix-{fix.IssueId}",
            Title = $"Fix: {fix.Description}",
            Description = fix.ApproachDescription,
            Type = TaskType.ModifyFile,
            TargetFiles = fix.FilesToModify.ToList(),
            Status = Nexo.Agents.AutonomousDev.Models.TaskStatus.Pending
        });
        
        return tasks.Concat(newTasks).ToList();
    }
}

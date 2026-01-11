using Microsoft.AspNetCore.SignalR;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Demo.Api.Hubs;
using Nexo.Demo.Bricks.Security;
using Nexo.Infrastructure.Execution;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register infrastructure services
builder.Services.AddSingleton<IProviderFactory, ProviderFactory>();
builder.Services.AddSingleton<ISemanticCache, SemanticCache>();
builder.Services.AddSingleton<IExecutionStateManager, ExecutionStateManager>();

// Register demo bricks
builder.Services.AddSingleton<OWASPScannerBrick>();

// Register registries with demo data
// Note: We'll build the brick list after the service provider is built
var bricksFactory = new Func<IServiceProvider, List<Brick>>(sp =>
{
    return new List<Brick>
    {
        sp.GetRequiredService<OWASPScannerBrick>()
    };
});

var behaviors = new List<Behavior>
{
    new()
    {
        Id = "security-scan",
        Name = "Security Scan",
        Version = "1.0.0",
        Description = "Scans code for security vulnerabilities",
        Steps = [
            new BehaviorStep
            {
                Id = "scan-step",
                BrickId = "owasp-scanner",
                Implementation = ImplementationType.Auto,
                InputMapping = new Dictionary<string, string>
                {
                    ["code"] = "code",
                    ["language"] = "language"
                },
                OutputMapping = new Dictionary<string, string>
                {
                    ["findings"] = "findings"
                }
            }
        ],
        Inputs = [
            new BehaviorParameter("code", "string", required: true, description: "Source code to scan"),
            new BehaviorParameter("language", "string", required: false, description: "Programming language")
        ],
        Outputs = [
            new BehaviorParameter("findings", "SecurityFinding[]", required: false, description: "Detected vulnerabilities")
        ]
    }
};

var agents = new List<AgentCard>
{
    new()
    {
        Id = "security-agent",
        Name = "Security Agent",
        Version = "1.0.0",
        Icon = "🔒",
        Domain = "Security",
        Description = "AI agent specialized in security analysis",
        Platforms = [Platform.Web, Platform.Api],
        Behaviors = ["security-scan"],
        Memory = new AgentMemoryConfig
        {
            SemanticCache = true,
            PatternLibrary = true,
            CrossProjectSharing = true
        }
    }
};

builder.Services.AddSingleton<IBrickRegistry>(sp => new BrickRegistry(bricksFactory(sp)));
builder.Services.AddSingleton<IBehaviorRegistry>(sp => new BehaviorRegistry(behaviors));
builder.Services.AddSingleton<IAgentRegistry>(sp => new AgentRegistry(agents));

// Register executor
builder.Services.AddScoped<IBehaviorExecutor, BehaviorExecutor>();

var app = builder.Build();

// Configure pipeline
app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ExecutionHub>("/hubs/execution");

app.Run();


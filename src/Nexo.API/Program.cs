using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Nexo.API.Services;
using Nexo.Adapters.GeoTerrain;
using Nexo.Adapters.GeoVector;
using Nexo.CLI;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Nexo Geospatial API",
        Version = "v1",
        Description = "RESTful API for geospatial operations including terrain generation, vector feature extraction, and world bundle creation.",
        Contact = new OpenApiContact
        {
            Name = "Nexo",
            Url = new Uri("https://github.com/IanFrelinger/Nexo")
        }
    });
    
    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register HTTP clients
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("geoterrain.srtm");
builder.Services.AddHttpClient("geovector.mapbox");

// Register CLI commands (needed for service execution)
builder.Services.AddScoped<Nexo.CLI.Commands.GeoTerrain.IGeoTerrainCommand, Nexo.CLI.Commands.GeoTerrain.GeoTerrainCommand>();
builder.Services.AddScoped<Nexo.CLI.Commands.GeoVector.IGeoVectorCommand, Nexo.CLI.Commands.GeoVector.GeoVectorCommand>();
builder.Services.AddScoped<Nexo.CLI.Commands.World.IWorldCommand, Nexo.CLI.Commands.World.WorldCommand>();

// Register logging
builder.Services.AddLogging();

// Register job repository (SQLite)
var dbPath = Path.Combine(Path.GetTempPath(), "nexo-api", "jobs.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddSingleton<IJobRepository>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SqliteJobRepository>>();
    return new SqliteJobRepository(dbPath, logger);
});

// Register API-specific services
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<IGeoTerrainService, GeoTerrainService>();
builder.Services.AddScoped<IGeoVectorService, GeoVectorService>();
builder.Services.AddScoped<IWorldService, WorldService>();
builder.Services.AddScoped<IJobService, JobService>();

// Register background job cleanup service
builder.Services.AddHostedService<JobCleanupService>();

// Register infrastructure needed by CLI commands
builder.Services.AddScoped<Nexo.Infrastructure.Execution.IProviderFactory, Nexo.Infrastructure.Execution.ProviderFactory>();
builder.Services.AddScoped<ILoopKernel, SequentialLoopKernel>();

// Configure authentication
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, Nexo.API.Middleware.ApiKeyAuthenticationHandler>("ApiKey", null);

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexo Geospatial API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at root
    });
}

app.UseCors();

// Add rate limiting (60 requests per minute per client)
app.UseMiddleware<Nexo.API.Middleware.RateLimitingMiddleware>(60);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

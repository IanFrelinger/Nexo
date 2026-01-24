using Microsoft.OpenApi.Models;
using Nexo.API.Services;
using Nexo.Adapters.GeoTerrain;
using Nexo.Adapters.GeoVector;
using Nexo.CLI;

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

// Register geospatial services (reuse CLI configuration pattern)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("geoterrain.srtm");
builder.Services.AddHttpClient("geovector.mapbox");

// Register CLI commands (needed for service execution)
builder.Services.AddScoped<Nexo.CLI.Commands.GeoTerrain.GeoTerrainCommand>();
builder.Services.AddScoped<Nexo.CLI.Commands.GeoVector.GeoVectorCommand>();
builder.Services.AddScoped<Nexo.CLI.Commands.World.WorldCommand>();

// Register logging
builder.Services.AddLogging();

// Register API-specific services
builder.Services.AddScoped<IGeoTerrainService, GeoTerrainService>();
builder.Services.AddScoped<IGeoVectorService, GeoVectorService>();
builder.Services.AddScoped<IWorldService, WorldService>();
builder.Services.AddScoped<IJobService, JobService>();

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
app.UseAuthorization();
app.MapControllers();

app.Run();

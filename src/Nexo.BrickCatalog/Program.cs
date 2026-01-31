using Nexo.BrickCatalog;
using Nexo.BrickContracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CentralCatalogOptions>(
    builder.Configuration.GetSection(CentralCatalogOptions.SectionName));
builder.Services.AddSingleton<CatalogAggregator>();
builder.Services.AddHttpClient("CentralCatalog.Instance");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Nexo Central Brick Catalog",
        Version = "v1",
        Description = "Aggregates brick catalogs from multiple Nexo instances. Each entry includes HostBaseUrl for execute."
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexo Central Brick Catalog v1");
        c.RoutePrefix = string.Empty;
    });
}

// Same path as instance API (/api/bricks) so HttpRemoteBrickCatalog works when pointing at central catalog.
app.MapGet("/api/bricks", async (CatalogAggregator aggregator, CancellationToken ct) =>
{
    var bricks = await aggregator.GetAllAsync(ct).ConfigureAwait(false);
    return Results.Ok(new BrickCatalogResponseDto
    {
        WireFormatVersion = WireFormatVersion.Current,
        Bricks = bricks.ToList()
    });
})
.WithName("GetBricks")
.WithOpenApi()
.Produces<BrickCatalogResponseDto>(StatusCodes.Status200OK);

app.MapGet("/api/bricks/{id}", async (string id, CatalogAggregator aggregator, CancellationToken ct) =>
{
    var entry = await aggregator.GetByIdAsync(id, ct).ConfigureAwait(false);
    return entry != null ? Results.Ok(entry) : Results.NotFound();
})
.WithName("GetBrickById")
.WithOpenApi()
.Produces<BrickCatalogEntryDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();

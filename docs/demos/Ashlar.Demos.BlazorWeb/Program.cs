using Ashlar.Client;
using Ashlar.Demos.BlazorWeb.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAshlarClient(o =>
{
    o.BaseUrl = builder.Configuration["Ashlar:BaseUrl"] ?? "http://localhost:5000";
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync().ConfigureAwait(false);

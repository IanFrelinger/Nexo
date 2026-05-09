# Nexo client demos

Three minimal **`net8.0`** samples under **`Nexo.Demos.sln`** that call **`Nexo.API`** through **`Nexo.Client`** (`GET api/status`). They build on **Linux** with a stock .NET SDK.

Build everything:

```bash
dotnet build Nexo.Demos.sln
```

Run **`Nexo.API`** first (for example `dotnet run --project src/Nexo.API` or your usual host). Default base URL is **`http://localhost:5000`**.

## Console (`Nexo.Demos.ConsoleClient`)

```bash
dotnet run --project docs/demos/Nexo.Demos.ConsoleClient
# or
NEXO_BASE_URL=http://localhost:5000 dotnet run --project docs/demos/Nexo.Demos.ConsoleClient
```

## Blazor Web (`Nexo.Demos.BlazorWeb`)

Interactive Server UI. Configure URL in `appsettings.json` (`Nexo:BaseUrl`) or environment.

```bash
dotnet run --project docs/demos/Nexo.Demos.BlazorWeb
```

Then open the URL from `Properties/launchSettings.json` (default **http://localhost:5288**).

## Avalonia desktop (`Nexo.Demos.Avalonia`)

Cross-platform desktop shell (Skia). Edit the base URL in the window if needed.

```bash
dotnet run --project docs/demos/Nexo.Demos.Avalonia
```

Headless/Linux servers without a display cannot open the Avalonia window; use the **Console** or **Blazor** demo there instead.

# Ashlar client demos

Three minimal **`net8.0`** samples under **`Ashlar.Demos.sln`** that call **`Ashlar.API`** through **`Ashlar.Client`** (`GET api/status`). They build on **Linux** with a stock .NET SDK.

Build everything:

```bash
dotnet build Ashlar.Demos.sln
```

Run **`Ashlar.API`** first (for example `dotnet run --project application/src/Ashlar.API -f net10.0` or your usual host). Default base URL is **`http://localhost:5000`**.

## Console (`Ashlar.Demos.ConsoleClient`)

```bash
dotnet run --project docs/demos/Ashlar.Demos.ConsoleClient
# or
ASHLAR_BASE_URL=http://localhost:5000 dotnet run --project docs/demos/Ashlar.Demos.ConsoleClient
```

## Blazor Web (`Ashlar.Demos.BlazorWeb`)

Interactive Server UI. Configure URL in `appsettings.json` (`Ashlar:BaseUrl`) or environment.

```bash
dotnet run --project docs/demos/Ashlar.Demos.BlazorWeb
```

Then open the URL from `Properties/launchSettings.json` (default **http://localhost:5288**).

## Avalonia desktop (`Ashlar.Demos.Avalonia`)

Cross-platform desktop shell (Skia). Edit the base URL in the window if needed.

```bash
dotnet run --project docs/demos/Ashlar.Demos.Avalonia
```

Headless/Linux servers without a display cannot open the Avalonia window; use the **Console** or **Blazor** demo there instead.

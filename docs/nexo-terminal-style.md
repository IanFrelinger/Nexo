# nexo — terminal style guide

The CLI is the brand's fourth surface (after README, NuGet, GitHub). Same palette, same node language, adapted to a dark terminal. `NexoConsole.cs` implements everything below.

## Palette

| Name  | Hex       | Truecolor            | 256 fallback | Role |
|-------|-----------|----------------------|--------------|------|
| cream | `#F7F2E5` | `38;2;247;242;229`   | 230          | primary text, wordmark |
| sage  | `#7E8F6E` | `38;2;126;143;110`   | 108          | structure: rules, chain, info glyphs |
| gold  | `#D1A23C` | `38;2;209;162;60`    | 179          | **certification truth only** |
| clay  | `#C96F4A` | `38;2;201;111;74`    | 173          | human attention: warn, reject, annotations |
| olive | `#8A8069` | `38;2;138;128;105`   | 101          | dim context: timings, hashes, `· notes` |
| ink   | `#2B2420` | (background)         | 235          | recommended terminal bg for docs/screenshots |

Color discipline is the whole trick: **gold is never decoration.** If a line is gold, a certificate chain actually verified. Clay always means a human should look. Everything structural is sage; everything secondary is olive.

## Glyph vocabulary

| Glyph | Meaning | Color |
|-------|---------|-------|
| `●` | info / a node doing work | sage |
| `◉` | certified (the gold node from the logo) | gold |
| `✓` | pass | gold |
| `▲` | warning · human gate | clay |
| `×` | rejected · fail-closed | clay |
| `○` | pending / queued | olive |
| `↳` | annotation (the "margin note") | clay italic |
| `──` | section rule | sage |

Use `×` (U+00D7), not `✕`/`✗` — it's the one with universal monospace coverage.

## Line format

```
  {glyph} {message} · {context}                              {timing/hash}
```

- 2-space indent everywhere; 68-char content width
- message in cream (or the glyph's color for certified/error lines)
- ` · context` in olive, right column (timings, `ed25519`, short hashes) in olive
- one clay line per screen is plenty — if everything is urgent, nothing is

## Banner

- N, E, X in cream; the O ring in gold with a cream center block — the node-"o" from the wordmark
- `↳ certified!` in clay italic under the O; the `~ + *` doodles live on the banner **only**, never in logs (that's the whole "curated chaos" budget for the terminal)
- Print only in interactive sessions; skip when piped (`Console.IsOutputRedirected`)

## The chain motif

```
●────────●────────◉   brick → behavior → agent
```

Sage line and nodes, gold terminal node. Reuse it as a staged progress indicator: fill nodes left-to-right as layers come up.

## Behavior

- Honor `NO_COLOR` and output redirection (both built into `NexoConsole.UseColor`)
- Truecolor first; the 256 column above is the graceful-degradation map
- Windows: call `NexoConsole.EnableVirtualTerminal()` once at startup for legacy conhost; Windows Terminal needs nothing

## Spectre.Console mapping

If a command uses Spectre instead of raw ANSI:

```csharp
var sage = new Style(new Color(0x7E, 0x8F, 0x6E));
var gold = new Style(new Color(0xD1, 0xA2, 0x3C));
var clay = new Style(new Color(0xC9, 0x6F, 0x4A));
// rules:   new Rule("[#7E8F6E]boot[/]").RuleStyle(sage)
// spinner: Spinner.Known.Dots, style sage
```

Same discipline applies: gold stays reserved for certification.

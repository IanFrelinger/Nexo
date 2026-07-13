# Trust policy pack schema (excerpt)

Trust policy packs are versioned JSON documents under `config/trust-packs/`.

Core fields:

- `id`, `version`, `displayName`, `description`
- `categoryRules`, `sourceRules`, `projectRules`
- `skillRules` (optional) — skill visibility, script allow-list, auto-approve tuples, script limits

See `src/Nexo.Core.Application/Trust/Models/TrustPolicyPack.cs` for the canonical model.

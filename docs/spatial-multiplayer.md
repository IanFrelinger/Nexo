# Spatial multiplayer (v1)

Ashlar.Spatial.Multiplayer uses **single host authority per match scope**, not per-atom authority.

Each `MatchScope` designates one `HostParticipantId` that may publish poses for all `ScopedAtomIds` in that scope. Participants are read-only subscribers. This keeps pose fan-out and rejection logic tractable for LAN-local play without CRDT merge rules or per-atom ownership graphs.

Per-atom host authority is intentionally deferred until a concrete use case requires it.

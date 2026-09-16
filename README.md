# CoopParryFix

Valheim 1.0 client plugin. Keeps perfect parry feeling like the vanilla **250ms** window when other players are nearby and when ping/hitches delay the hit RPC.

Every client needs the DLL. Parry is decided on the owning client, not on the dedicated server.

Previously named ZoneParry. Remove any old `ZoneParry.dll` so both mods do not patch `BlockAttack`.

## What it adds

Vanilla check: `m_blockTimer < 0.25`.

Default extras:

1. **Crowd** — +50ms per extra player in the same 64m zone.
2. **Latency** — + (measured RTT × 0.5) + jitter + a quality penalty, from public `ZNet.GetNetStats` ping/quality.

An 80ms ping with little jitter becomes about +40ms. A hitching host shows up as jitter and lower connection quality, which adds more.

`Max Bonus Milliseconds` (default 500) caps crowd + latency together.

## Install

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

- Thunderstore: install the `Ageous27-CoopParryFix` package, or
- Manual: drop `CoopParryFix.dll` from the [GitHub release zip](https://github.com/Ageous27/CoopParryFix/releases) into `BepInEx/plugins` on **every client**.

Restart after replacing the DLL.

## Config

`BepInEx/config/Ageous.CoopParryFix.cfg` after first launch.

- `Latency.Compensate Latency` — default on.
- `Latency.Ping Scale` — default 0.5 (one-way).
- `Latency.Jitter Scale` — default 1.
- `Latency.Quality Hitch Milliseconds` — default 80 (scaled by 1 − connection quality).
- `Parry.Milliseconds Per Player` — default 50.
- `General.Debug Logs` — prints ping, jitter, crowd, and the final window on each block.

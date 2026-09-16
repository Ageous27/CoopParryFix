# CoopParryFix

Valheim 1.0 client plugin. Keeps perfect parry on the vanilla **250ms** window when this client owns the attacker, and widens it by measured ping/jitter when the hit RPC is late.

Every client needs the DLL. Parry is decided on the owning client, not on the dedicated server.

Compatible with [SmoothServer](https://github.com/MJensen01/SmoothServer). CoopParryFix does not change ZDO ownership, send cadence, compression, or interpolation. If SmoothServer is loaded it logs that and leaves those jobs alone.

Previously named ZoneParry. Remove any old `ZoneParry.dll` so both mods do not patch `BlockAttack`.

## What it adds

Vanilla check: `m_blockTimer < 0.25`.

- If **`attacker.IsOwner()`** (you are simulating the mob): **250ms**, same as solo.
- If not: **+ (RTT × 0.5) + jitter + quality hitch** from public `ZNet.GetNetStats`.
- Crowd extra is **off** by default (`Milliseconds Per Player = 0`). SmoothServer already shortens ownership handoff.

An 80ms ping with little jitter becomes about +40ms on a remote-owned hit. Locally owned hits stay vanilla.

## Install

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

- Thunderstore: install the `Ageous27-CoopParryFix` package, or
- Manual: drop `CoopParryFix.dll` from the [GitHub release zip](https://github.com/Ageous27/CoopParryFix/releases) into `BepInEx/plugins` on **every client**.

Restart after replacing the DLL.

## Config

`BepInEx/config/Ageous.CoopParryFix.cfg` after first launch.

- `Latency.Compensate Latency` — default on. Skipped when you own the attacker.
- `Latency.Ping Scale` — default 0.5 (one-way).
- `Latency.Jitter Scale` — default 1.
- `Latency.Quality Hitch Milliseconds` — default 80 (scaled by 1 − connection quality).
- `Parry.Milliseconds Per Player` — default **0**. Set 50 only if you want the old crowd extra.
- `General.Debug Logs` — default **on**. Prints owner, ping, jitter, crowd, and the final window on each block.

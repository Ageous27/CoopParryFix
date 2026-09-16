# CoopParryFix

Valheim 1.0 plugin. **One DLL** for clients and the dedicated server.

When this client owns the attacker, perfect parry stays the vanilla **250ms** window. When someone else simulates the mob, CoopParryFix measures `you → server → owner PC → server → you` and adds half of that RTT, plus owner hitch above 33ms.

Steam `GetNetStats` ping is often **0** on a Steam dedicated server. That is not "no delay." Owner-path ping still works in that case.

Install on the dedicated server and every client for best results. If CoopParryFix is missing on a client and/or the server, pings time out and the parrying client falls back (`GetNetStats` if ping > 0, otherwise a 40ms floor). Blocking still works; the window is just a coarser guess.

Compatible with [SmoothServer](https://github.com/MJensen01/SmoothServer). Does not use `SS_Ping`. Owner-path delay uses `CPF_Ping` / `CPF_Pong`.

## What it adds

Vanilla check: `m_blockTimer < 0.25`.

- If **`attacker.IsOwner()`**: **250ms**, same as solo.
- If not: **+ (owner-path RTT × 0.5) + hitch above 33ms**, using the **max** sample in the last 5 seconds.
- Crowd extra is **off** by default (`Milliseconds Per Player = 0`).

Co-op check on dedicated: the owner kept 250ms; the other client widened by measured owner-path RTT (~20ms on LAN) while Steam ping stayed 0.

## Install

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

Drop `CoopParryFix.dll` from the [GitHub release zip](https://github.com/Ageous27/CoopParryFix/releases) into `BepInEx/plugins` on the **dedicated server and every client**. One file, not two. Restart after replacing the DLL.

## Config

`BepInEx/config/Ageous.CoopParryFix.cfg` after first launch.

- `Latency.Compensate Latency` — default on. Skipped when you own the attacker.
- `Latency.Ping Scale` — default 0.5 (combat one-way).
- `Latency.Window Seconds` — default 5.
- `Latency.Rtt Stat` — default Max (also P90, Ema).
- `Latency.Hitch Budget Milliseconds` — default 33 (30 FPS). Only frame time above this counts as hitch.
- `Latency.Min Remote Owner Milliseconds` — default 40 when no ping sample is available.
- `Parry.Milliseconds Per Player` — default **0**.
- `General.Debug Logs` — default **on**.

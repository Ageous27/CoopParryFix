# Changelog

## 0.4.1
- CoopParryFix writes `BepInEx/plugins/Ageous27-CoopParryFix/CoopParryFix.log` (next to the DLL), and still mirrors lines into BepInEx `LogOutput.log`.

## 0.4.0
- Same DLL on dedicated server and clients. Routed `CPF_Ping` / `CPF_Pong` to `attacker.GetOwner()` measures you→server→owner→server→you.
- Remote-owned parry window uses max RTT in a 5s window (half, plus hitch above 33ms). Debug Logs default on.
- Missing CoopParryFix on a client or the server: ping times out and falls back to GetNetStats or a 40ms floor. Does not break blocking.

## 0.3.0
- Latency bonus is 0 when this client owns the attacker (`Character.IsOwner()`). Remote-owned hits still use `ZNet.GetNetStats`.
- Crowd extra defaults to 0ms so nearby players do not make parries easier than solo. Set `Milliseconds Per Player` to 50 for the old behavior.
- Soft-detects SmoothServer (`Nosferatu.SmoothServer`) and does not duplicate its net/ownership work.
- Debug Logs default on for new config files.

## 0.2.2
- Renamed the mod to CoopParryFix (was ZoneParry). New GUID `Ageous.CoopParryFix`; remove the old `ZoneParry.dll`.
- Debug BlockAttack prefix no longer calls private `Humanoid.GetCurrentBlocker()` (MethodAccessException on the live assembly).

## 0.2.1
- Fix FieldAccessException spam on `Character.m_nview` (publicized-at-compile, private-at-runtime). That Update exception was breaking combat.
- Latency now uses only public `ZNet.GetNetStats` ping/quality. No private Character fields.
- Clamp the parry window so it cannot become NaN/infinite.

## 0.2.0
- Client-side latency compensation: samples Steam/PlayFab connection ping, jitter, and quality.
- Adds one-way ping plus jitter to the 250ms vanilla window so a locally timed parry still counts when the hit RPC is late.
- Crowd bonus is unchanged (50ms per extra player in the zone).

## 0.1.1
- Debug now logs every blocked hit, not only when the nearby player count changes.
- Stopped using `is Player` (could skip the bonus/log). Player check is `Character.IsPlayer()`.

## 0.1.0
- Initial release for Valheim 1.0.
- Harmony transpile of `Humanoid.BlockAttack` replaces the hardcoded 0.25s perfect-parry window.
- Adds 50ms per extra player in the same 64m world zone. Optional WorldChunk (512m) or radius grouping.

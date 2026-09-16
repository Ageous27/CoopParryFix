using BepInEx.Configuration;

namespace CoopParryFix;

internal enum PlayerGrouping
{
    Zone,
    WorldChunk,
    Radius
}

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<bool> DebugLogs = null!;
    internal static ConfigEntry<int> MillisecondsPerPlayer = null!;
    internal static ConfigEntry<bool> IgnoreFirstPlayer = null!;
    internal static ConfigEntry<int> MaxBonusMilliseconds = null!;
    internal static ConfigEntry<PlayerGrouping> Grouping = null!;
    internal static ConfigEntry<float> RadiusMeters = null!;
    internal static ConfigEntry<bool> CompensateLatency = null!;
    internal static ConfigEntry<float> PingScale = null!;
    internal static ConfigEntry<float> JitterScale = null!;
    internal static ConfigEntry<int> QualityHitchMilliseconds = null!;
    internal static ConfigEntry<int> MaxLatencyBonusMilliseconds = null!;

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true,
            "If off, the vanilla 250ms parry window is left unchanged.");
        DebugLogs = config.Bind("General", "Debug Logs", true,
            "Log every blocked hit: owner, ping, jitter, and the resulting parry window. Default on for new installs.");

        MillisecondsPerPlayer = config.Bind("Parry", "Milliseconds Per Player", 0,
            "Extra ms per billed nearby player. Default 0 (vanilla solo timing). SmoothServer already shortens ownership handoff; set 50 only if you want the old crowd extra on top of latency.");
        IgnoreFirstPlayer = config.Bind("Parry", "Ignore First Player", true,
            "If on (default), you alone keep no crowd extra. Each additional player in the area adds Milliseconds Per Player.");
        MaxBonusMilliseconds = config.Bind("Parry", "Max Bonus Milliseconds", 500,
            "Cap on extra parry time beyond the vanilla 250ms (crowd + latency combined). 0 = no cap.");

        Grouping = config.Bind("Area", "Grouping", PlayerGrouping.Zone,
            "Zone = Valheim 64m world zone (the usual 'chunk'). WorldChunk = 1.0 streaming chunk (8x8 zones, 512m). Radius = players within Radius Meters.");
        RadiusMeters = config.Bind("Area", "Radius Meters", 64f,
            "Only used when Grouping is Radius.");

        CompensateLatency = config.Bind("Latency", "Compensate Latency", true,
            "When this client does not own the attacker, add ping/jitter so a vanilla-timed parry still counts. Locally owned attackers stay at 250ms. Does not change ZDO ownership (leave that to SmoothServer / CombatOwner).");
        PingScale = config.Bind("Latency", "Ping Scale", 0.5f,
            "Fraction of measured RTT added to the window. 0.5 = one-way delay. 1.0 = full round trip.");
        JitterScale = config.Bind("Latency", "Jitter Scale", 1f,
            "Multiplier on ping jitter (how much ping bounces). Catches hitching hosts better than average ping alone.");
        QualityHitchMilliseconds = config.Bind("Latency", "Quality Hitch Milliseconds", 80,
            "Extra ms when Steam/PlayFab connection quality is poor (0 quality = this many ms, 1 quality = 0).");
        MaxLatencyBonusMilliseconds = config.Bind("Latency", "Max Latency Bonus Milliseconds", 250,
            "Cap on the latency portion only. 0 = no extra cap beyond Max Bonus Milliseconds.");
    }
}

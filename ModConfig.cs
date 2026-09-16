using BepInEx.Configuration;

namespace CoopParryFix;

internal enum PlayerGrouping
{
    Zone,
    WorldChunk,
    Radius
}

internal enum RttStatMode
{
    Max,
    P90,
    Ema
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
    internal static ConfigEntry<float> SampleIntervalSeconds = null!;
    internal static ConfigEntry<float> WindowSeconds = null!;
    internal static ConfigEntry<RttStatMode> RttStat = null!;
    internal static ConfigEntry<int> HitchBudgetMilliseconds = null!;
    internal static ConfigEntry<float> HitchScale = null!;
    internal static ConfigEntry<int> MinRemoteOwnerMilliseconds = null!;
    internal static ConfigEntry<float> PingTimeoutSeconds = null!;

    internal static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true,
            "If off, the vanilla 250ms parry window is left unchanged.");
        DebugLogs = config.Bind("General", "Debug Logs", true,
            "Log every blocked hit: owner, ping, hitch, fallback, and the resulting parry window. Default on for new installs.");

        MillisecondsPerPlayer = config.Bind("Parry", "Milliseconds Per Player", 0,
            "Extra ms per billed nearby player. Default 0 (vanilla solo timing). Set 50 only if you want crowd extra on top of latency.");
        IgnoreFirstPlayer = config.Bind("Parry", "Ignore First Player", true,
            "If on (default), you alone keep no crowd extra. Each additional player in the area adds Milliseconds Per Player.");
        MaxBonusMilliseconds = config.Bind("Parry", "Max Bonus Milliseconds", 500,
            "Cap on extra parry time beyond the vanilla 250ms (crowd + latency combined). 0 = no cap.");

        Grouping = config.Bind("Area", "Grouping", PlayerGrouping.Zone,
            "Zone = Valheim 64m world zone (the usual 'chunk'). WorldChunk = 1.0 streaming chunk (8x8 zones, 512m). Radius = players within Radius Meters.");
        RadiusMeters = config.Bind("Area", "Radius Meters", 64f,
            "Only used when Grouping is Radius.");

        CompensateLatency = config.Bind("Latency", "Compensate Latency", true,
            "When this client does not own the attacker, add measured owner-path delay so a vanilla-timed parry still counts. Locally owned attackers stay at 250ms.");
        PingScale = config.Bind("Latency", "Ping Scale", 0.5f,
            "Fraction of owner-path RTT added to the window. 0.5 = combat one-way (you→server→owner→server→you, halved).");
        JitterScale = config.Bind("Latency", "Jitter Scale", 1f,
            "Multiplier on GetNetStats ping jitter. Only used when owner-path ping has no sample (missing mod / timeout).");
        QualityHitchMilliseconds = config.Bind("Latency", "Quality Hitch Milliseconds", 80,
            "GetNetStats quality extra (0 quality = this many ms). Only used when owner-path ping has no sample.");
        MaxLatencyBonusMilliseconds = config.Bind("Latency", "Max Latency Bonus Milliseconds", 250,
            "Cap on the latency portion only. 0 = no extra cap beyond Max Bonus Milliseconds.");
        SampleIntervalSeconds = config.Bind("Latency", "Sample Interval Seconds", 1f,
            "How often to send CPF_Ping to the server and to nearby/recent owners.");
        WindowSeconds = config.Bind("Latency", "Window Seconds", 5f,
            "Seconds of ping/hitch samples kept per owner. Default 5.");
        RttStat = config.Bind("Latency", "Rtt Stat", RttStatMode.Max,
            "How to reduce RTTs in the window: Max (inclusive, default), P90, or Ema.");
        HitchBudgetMilliseconds = config.Bind("Latency", "Hitch Budget Milliseconds", 33,
            "Owner frame time at or below this (default 33ms = 30 FPS) adds no hitch. Extra ms above this are added.");
        HitchScale = config.Bind("Latency", "Hitch Scale", 1f,
            "Multiplier on owner hitch above Hitch Budget.");
        MinRemoteOwnerMilliseconds = config.Bind("Latency", "Min Remote Owner Milliseconds", 40,
            "If the attacker is not local and no ping sample is in yet (GetNetStats 0, missing CoopParryFix on owner/server, timeout), add at least this many ms.");
        PingTimeoutSeconds = config.Bind("Latency", "Ping Timeout Seconds", 3f,
            "If CPF_Pong never arrives, treat that peer as missing CoopParryFix and use fallback. Does not break parry.");
    }
}

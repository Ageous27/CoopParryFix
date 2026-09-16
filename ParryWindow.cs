namespace CoopParryFix;

internal static class ParryWindow
{
    internal const float VanillaSeconds = 0.25f;
    internal const float MaxWindowSeconds = 1f;

    internal static float GetSeconds(Humanoid blocker, Character attacker)
    {
        try
        {
            if (!ModConfig.Enabled.Value)
                return VanillaSeconds;

            if (!blocker || !blocker.IsPlayer())
                return VanillaSeconds;

            int count = CountPlayersAt(blocker.transform.position);
            int billed = ModConfig.IgnoreFirstPlayer.Value ? Math.Max(0, count - 1) : Math.Max(0, count);
            float crowd = billed * (ModConfig.MillisecondsPerPlayer.Value / 1000f);

            bool localOwner = attacker && attacker.IsOwner();
            float latency = 0f;
            if (!localOwner)
                latency = LatencySampler.GetBonusSeconds();

            float bonus = crowd + latency;
            int maxMs = ModConfig.MaxBonusMilliseconds.Value;
            if (maxMs > 0)
                bonus = Mathf.Min(bonus, maxMs / 1000f);

            float window = VanillaSeconds + bonus;
            if (float.IsNaN(window) || float.IsInfinity(window) || window < VanillaSeconds)
                window = VanillaSeconds;
            window = Mathf.Min(window, MaxWindowSeconds);

            if (ModConfig.DebugLogs.Value)
            {
                string attackerName = attacker ? attacker.name : "null";
                Plugin.Log.LogInfo(
                    $"parry window {window * 1000f:0}ms | attacker={attackerName} localOwner={localOwner} | players={count} billed={billed} crowd={crowd * 1000f:0}ms | ping={LatencySampler.LastPingMs}ms ema={LatencySampler.EmaPingMs:0} jitter={LatencySampler.JitterMs:0} q={LatencySampler.LastRemoteQuality:0.00}/{LatencySampler.LastLocalQuality:0.00} latency={latency * 1000f:0}ms");
            }

            return window;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"CoopParryFix GetSeconds failed: {ex}");
            return VanillaSeconds;
        }
    }

    internal static int CountPlayersAt(Vector3 origin)
    {
        var players = Player.GetAllPlayers();
        if (players == null || players.Count == 0)
            return 1;

        var grouping = ModConfig.Grouping.Value;
        int count = 0;

        Vector2s selfZone = default;
        ZoneSystem.ChunkIndex selfChunk = default;
        if (grouping == PlayerGrouping.Zone)
            selfZone = ZoneSystem.GetZone(origin);
        else if (grouping == PlayerGrouping.WorldChunk)
            selfChunk = ZoneSystem.GetZonesChunk(ZoneSystem.GetSectorIndex(origin));

        float radius = ModConfig.RadiusMeters.Value;
        float radiusSq = radius * radius;

        foreach (var other in players)
        {
            if (!other)
                continue;

            Vector3 pos = other.transform.position;
            bool same = grouping switch
            {
                PlayerGrouping.WorldChunk =>
                    ZoneSystem.GetZonesChunk(ZoneSystem.GetSectorIndex(pos)).Equals(selfChunk),
                PlayerGrouping.Radius =>
                    (pos - origin).sqrMagnitude <= radiusSq,
                _ =>
                    ZoneSystem.GetZone(pos).Equals(selfZone)
            };

            if (same)
                count++;
        }

        return Math.Max(1, count);
    }
}

using HarmonyLib;

namespace CoopParryFix;

internal struct LatencyBonus
{
    internal float Seconds;
    internal string Source;
    internal float RttLastMs;
    internal float RttStatMs;
    internal float HitchStatMs;
    internal long OwnerUid;
}

internal static class OwnerPing
{
    internal const string RpcPing = "CPF_Ping";
    internal const string RpcPong = "CPF_Pong";

    private const int MaxPending = 32;
    private const int MaxSamplesPerUid = 64;
    private const float RecentAttackerSeconds = 30f;
    private const float EmaLerp = 0.2f;
    private const float MissingRetrySeconds = 15f;

    private static bool _registered;
    private static int _seq;
    private static float _sampleTimer;
    private static float _frameMsEma = 16f;

    private static readonly Dictionary<int, Pending> PendingPings = new();
    private static readonly Dictionary<long, UidState> States = new();
    private static readonly Dictionary<long, float> RecentOwners = new();

    internal static void NoteAttacker(Character? attacker)
    {
        if (!attacker)
            return;
        try
        {
            long uid = attacker.GetOwner();
            if (uid == 0)
                return;
            RecentOwners[uid] = Time.realtimeSinceStartup;
        }
        catch
        {
            // GetOwner is public; ignore a destroyed character.
        }
    }

    internal static void Tick()
    {
        _frameMsEma = Mathf.Lerp(_frameMsEma, Time.unscaledDeltaTime * 1000f, 0.25f);

        if (!_registered)
            TryRegister();

        PruneTimeouts();
        PruneRecentOwners();

        if (!ModConfig.CompensateLatency.Value)
            return;

        if (!Player.m_localPlayer)
            return;

        _sampleTimer += Time.unscaledDeltaTime;
        float interval = Mathf.Max(0.25f, ModConfig.SampleIntervalSeconds.Value);
        if (_sampleTimer < interval)
            return;
        _sampleTimer = 0f;
        SendSamplePings();
    }

    internal static LatencyBonus GetBonus(Character? attacker)
    {
        var bonus = new LatencyBonus { Source = "none", Seconds = 0f };
        if (!ModConfig.CompensateLatency.Value)
            return bonus;

        long ownerUid = 0;
        if (attacker)
        {
            try { ownerUid = attacker.GetOwner(); }
            catch { ownerUid = 0; }
        }

        bonus.OwnerUid = ownerUid;
        NetStats.Sample();

        bool ownerMissing = IsMissing(ownerUid);
        if (!ownerMissing && TryOwnerPath(ownerUid, ref bonus))
            return Cap(bonus);

        long serverUid = ServerUid();
        bool serverMissing = IsMissing(serverUid);
        if (serverUid != 0 && serverUid != ownerUid && !serverMissing && TryOwnerPath(serverUid, ref bonus))
        {
            bonus.Source = "server-" + StatName();
            return Cap(bonus);
        }

        if (NetStats.LastPingMs > 0)
        {
            bonus.Source = ownerMissing || serverMissing ? "missing-mod-netstats" : "netstats";
            bonus.RttLastMs = NetStats.LastPingMs;
            bonus.RttStatMs = NetStats.EmaPingMs;
            float pingPart = NetStats.EmaPingMs * ModConfig.PingScale.Value;
            float jitterPart = NetStats.JitterMs * ModConfig.JitterScale.Value;
            float quality = Mathf.Clamp01(Mathf.Min(NetStats.LastLocalQuality, NetStats.LastRemoteQuality));
            if (float.IsNaN(quality))
                quality = 1f;
            float qualityPart = (1f - quality) * ModConfig.QualityHitchMilliseconds.Value;
            bonus.Seconds = Mathf.Max(0f, pingPart + jitterPart + qualityPart) / 1000f;
            return Cap(bonus);
        }

        int minMs = Math.Max(0, ModConfig.MinRemoteOwnerMilliseconds.Value);
        bonus.Source = ownerMissing || serverMissing ? "missing-mod-floor" : "min-floor";
        bonus.Seconds = minMs / 1000f;
        return Cap(bonus);
    }

    internal static void TryRegister()
    {
        if (_registered)
            return;
        var rpc = ZRoutedRpc.instance;
        if (rpc == null)
            return;
        try
        {
            rpc.Register<int, long>(RpcPing, OnPing);
            rpc.Register<int, long, float>(RpcPong, OnPong);
            _registered = true;
            Plugin.Log?.LogInfo($"CoopParryFix routed RPCs '{RpcPing}' / '{RpcPong}' registered. Same DLL answers on dedicated and clients. Missing CoopParryFix on a peer times out and falls back.");
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"CoopParryFix RPC register failed: {ex.Message}");
        }
    }

    internal static void OnRoutedRpcCreated()
    {
        _registered = false;
        PendingPings.Clear();
        States.Clear();
        TryRegister();
    }

    private static void OnPing(long sender, int seq, long originMs)
    {
        try
        {
            var rpc = ZRoutedRpc.instance;
            if (rpc == null || sender == 0)
                return;
            rpc.InvokeRoutedRPC(sender, RpcPong, seq, originMs, _frameMsEma);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"CoopParryFix CPF_Ping handler failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void OnPong(long sender, int seq, long originMs, float ownerFrameMs)
    {
        try
        {
            if (!PendingPings.TryGetValue(seq, out var pending))
                return;
            PendingPings.Remove(seq);

            float rtt = NowMs() - originMs;
            if (rtt < 0f)
                rtt = 0f;
            if (float.IsNaN(ownerFrameMs) || ownerFrameMs < 0f)
                ownerFrameMs = 0f;

            var state = GetState(pending.TargetUid);
            state.LastRttMs = rtt;
            state.LastHitchMs = ownerFrameMs;
            state.Timeouts = 0;
            state.Missing = false;
            state.Samples.Add(new Sample
            {
                Time = Time.realtimeSinceStartup,
                RttMs = rtt,
                FrameMs = ownerFrameMs
            });
            Trim(state);

            if (ModConfig.DebugLogs.Value)
            {
                Plugin.Log?.LogInfo(
                    $"CPF_Pong from={sender} target={pending.TargetUid} rtt={rtt:0}ms frame={ownerFrameMs:0}ms n={state.Samples.Count}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogWarning($"CoopParryFix CPF_Pong handler failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void SendSamplePings()
    {
        if (!_registered || ZRoutedRpc.instance == null || !ZNet.instance)
            return;

        long self = ZNet.GetUID();
        var targets = new HashSet<long>();

        long serverUid = ServerUid();
        if (serverUid != 0 && serverUid != self)
            targets.Add(serverUid);

        var local = Player.m_localPlayer;
        if (local)
        {
            foreach (var other in Player.GetAllPlayers() ?? new List<Player>())
            {
                if (!other || other == local)
                    continue;
                if (!SameArea(local.transform.position, other.transform.position))
                    continue;
                long uid = other.GetOwner();
                if (uid != 0 && uid != self)
                    targets.Add(uid);
            }
        }

        float now = Time.realtimeSinceStartup;
        foreach (var kv in RecentOwners)
        {
            if (kv.Key != 0 && kv.Key != self && now - kv.Value <= RecentAttackerSeconds)
                targets.Add(kv.Key);
        }

        foreach (long uid in targets)
            SendPing(uid);
    }

    private static void SendPing(long targetUid)
    {
        if (PendingPings.Count >= MaxPending)
            return;
        if (!ZNet.instance || targetUid == 0 || targetUid == ZNet.GetUID())
            return;

        var state = GetState(targetUid);
        float now = Time.realtimeSinceStartup;
        if (state.Missing && now - state.LastAttempt < MissingRetrySeconds)
            return;

        var rpc = ZRoutedRpc.instance;
        if (rpc == null)
            return;

        int seq = ++_seq;
        long originMs = NowMs();
        state.LastAttempt = now;
        PendingPings[seq] = new Pending { TargetUid = targetUid, SentAt = now };
        try
        {
            rpc.InvokeRoutedRPC(targetUid, RpcPing, seq, originMs);
        }
        catch (Exception ex)
        {
            PendingPings.Remove(seq);
            state.Missing = true;
            if (ModConfig.DebugLogs.Value)
                Plugin.Log?.LogInfo($"CPF_Ping to {targetUid} failed ({ex.GetType().Name}); will fall back.");
        }
    }

    private static void PruneTimeouts()
    {
        float timeout = Mathf.Max(0.5f, ModConfig.PingTimeoutSeconds.Value);
        float now = Time.realtimeSinceStartup;
        var expired = new List<int>();
        foreach (var kv in PendingPings)
        {
            if (now - kv.Value.SentAt >= timeout)
                expired.Add(kv.Key);
        }

        foreach (int seq in expired)
        {
            var pending = PendingPings[seq];
            PendingPings.Remove(seq);
            var state = GetState(pending.TargetUid);
            state.Timeouts++;
            bool becameMissing = false;
            if (state.Timeouts >= 2 && !state.Missing)
            {
                state.Missing = true;
                becameMissing = true;
            }
            if (ModConfig.DebugLogs.Value && (becameMissing || !state.Missing))
            {
                Plugin.Log?.LogInfo(
                    $"CPF_Ping timeout target={pending.TargetUid} timeouts={state.Timeouts} (peer may not have CoopParryFix); falling back.");
            }
        }
    }

    private static void PruneRecentOwners()
    {
        float now = Time.realtimeSinceStartup;
        var drop = new List<long>();
        foreach (var kv in RecentOwners)
        {
            if (now - kv.Value > RecentAttackerSeconds)
                drop.Add(kv.Key);
        }
        foreach (long uid in drop)
            RecentOwners.Remove(uid);
    }

    private static bool IsMissing(long uid)
    {
        return uid != 0 && States.TryGetValue(uid, out var state) && state.Missing;
    }

    private static bool TryOwnerPath(long uid, ref LatencyBonus bonus)
    {
        if (uid == 0 || !States.TryGetValue(uid, out var state))
            return false;
        Trim(state);
        if (state.Samples.Count == 0)
            return false;

        bonus.OwnerUid = uid;
        bonus.RttLastMs = state.LastRttMs;
        bonus.RttStatMs = ReduceRtt(state);
        bonus.HitchStatMs = HitchStat(state);
        bonus.Source = "owner-" + StatName();
        float ms = ModConfig.PingScale.Value * bonus.RttStatMs
            + ModConfig.HitchScale.Value * bonus.HitchStatMs;
        if (float.IsNaN(ms) || float.IsInfinity(ms))
            ms = 0f;
        bonus.Seconds = Mathf.Max(0f, ms) / 1000f;
        return true;
    }

    private static float ReduceRtt(UidState state)
    {
        var values = new List<float>(state.Samples.Count);
        foreach (var s in state.Samples)
            values.Add(s.RttMs);
        if (values.Count == 0)
            return 0f;

        switch (ModConfig.RttStat.Value)
        {
            case RttStatMode.P90:
                values.Sort();
                int i = Mathf.Clamp(Mathf.RoundToInt((values.Count - 1) * 0.9f), 0, values.Count - 1);
                return values[i];
            case RttStatMode.Ema:
                float ema = values[0];
                for (int n = 1; n < values.Count; n++)
                    ema = Mathf.Lerp(ema, values[n], EmaLerp);
                return ema;
            default:
                float max = values[0];
                for (int n = 1; n < values.Count; n++)
                    max = Mathf.Max(max, values[n]);
                return max;
        }
    }

    private static float HitchStat(UidState state)
    {
        float maxFrame = 0f;
        foreach (var s in state.Samples)
            maxFrame = Mathf.Max(maxFrame, s.FrameMs);
        return Mathf.Max(0f, maxFrame - ModConfig.HitchBudgetMilliseconds.Value);
    }

    private static void Trim(UidState state)
    {
        float window = Mathf.Max(1f, ModConfig.WindowSeconds.Value);
        float cutoff = Time.realtimeSinceStartup - window;
        state.Samples.RemoveAll(s => s.Time < cutoff);
        if (state.Samples.Count > MaxSamplesPerUid)
            state.Samples.RemoveRange(0, state.Samples.Count - MaxSamplesPerUid);
    }

    private static LatencyBonus Cap(LatencyBonus bonus)
    {
        int cap = ModConfig.MaxLatencyBonusMilliseconds.Value;
        if (cap > 0)
            bonus.Seconds = Mathf.Min(bonus.Seconds, cap / 1000f);
        return bonus;
    }

    private static string StatName() => ModConfig.RttStat.Value.ToString().ToLowerInvariant();

    private static long ServerUid()
    {
        try
        {
            var rpc = ZRoutedRpc.instance;
            if (rpc == null)
                return 0;
            return rpc.GetServerPeerID();
        }
        catch
        {
            return 0;
        }
    }

    private static bool SameArea(Vector3 a, Vector3 b)
    {
        var grouping = ModConfig.Grouping.Value;
        if (grouping == PlayerGrouping.WorldChunk)
            return ZoneSystem.GetZonesChunk(ZoneSystem.GetSectorIndex(a))
                .Equals(ZoneSystem.GetZonesChunk(ZoneSystem.GetSectorIndex(b)));
        if (grouping == PlayerGrouping.Radius)
        {
            float r = ModConfig.RadiusMeters.Value;
            return (a - b).sqrMagnitude <= r * r;
        }
        return ZoneSystem.GetZone(a).Equals(ZoneSystem.GetZone(b));
    }

    private static UidState GetState(long uid)
    {
        if (!States.TryGetValue(uid, out var state))
        {
            state = new UidState();
            States[uid] = state;
        }
        return state;
    }

    private static long NowMs() => (long)(Time.realtimeSinceStartup * 1000f);

    private struct Sample
    {
        internal float Time;
        internal float RttMs;
        internal float FrameMs;
    }

    private struct Pending
    {
        internal long TargetUid;
        internal float SentAt;
    }

    private sealed class UidState
    {
        internal readonly List<Sample> Samples = new();
        internal float LastRttMs;
        internal float LastHitchMs;
        internal float LastAttempt;
        internal int Timeouts;
        internal bool Missing;
    }
}

[HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, new[] { typeof(bool) })]
internal static class OwnerPingRpcCtorPatch
{
    private static void Postfix()
    {
        try { OwnerPing.OnRoutedRpcCreated(); }
        catch { /* ZRoutedRpc may construct before Plugin.Log exists */ }
    }
}

namespace CoopParryFix;

internal static class LatencySampler
{
    private const float SampleInterval = 0.2f;
    private const float EmaLerp = 0.2f;

    internal static int LastPingMs;
    internal static float LastLocalQuality = 1f;
    internal static float LastRemoteQuality = 1f;
    internal static float EmaPingMs;
    internal static float JitterMs;

    private static float _sampleTimer;

    internal static void Tick()
    {
        try
        {
            _sampleTimer += Time.unscaledDeltaTime;
            if (_sampleTimer < SampleInterval)
                return;
            _sampleTimer = 0f;
            Sample();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"CoopParryFix latency tick failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    internal static float GetBonusSeconds()
    {
        if (!ModConfig.CompensateLatency.Value)
            return 0f;

        Sample();

        float pingPart = EmaPingMs * ModConfig.PingScale.Value;
        float jitterPart = JitterMs * ModConfig.JitterScale.Value;
        float quality = Mathf.Clamp01(Mathf.Min(LastLocalQuality, LastRemoteQuality));
        if (float.IsNaN(quality))
            quality = 1f;
        float qualityPart = (1f - quality) * ModConfig.QualityHitchMilliseconds.Value;
        float ms = pingPart + jitterPart + qualityPart;
        if (float.IsNaN(ms) || float.IsInfinity(ms))
            ms = 0f;

        int cap = ModConfig.MaxLatencyBonusMilliseconds.Value;
        if (cap > 0)
            ms = Mathf.Min(ms, cap);

        return Mathf.Max(0f, ms) / 1000f;
    }

    private static void Sample()
    {
        var znet = ZNet.instance;
        if (!znet)
            return;

        znet.GetNetStats(out float localQ, out float remoteQ, out int ping, out _, out _);
        LastPingMs = Math.Max(0, ping);
        LastLocalQuality = localQ;
        LastRemoteQuality = remoteQ;

        if (EmaPingMs <= 0f)
            EmaPingMs = LastPingMs;
        else
            EmaPingMs = Mathf.Lerp(EmaPingMs, LastPingMs, EmaLerp);

        JitterMs = Mathf.Lerp(JitterMs, Mathf.Abs(LastPingMs - EmaPingMs), EmaLerp);
    }
}

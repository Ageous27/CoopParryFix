namespace CoopParryFix;

internal static class NetStats
{
    private const float EmaLerp = 0.2f;

    internal static int LastPingMs;
    internal static float LastLocalQuality = 1f;
    internal static float LastRemoteQuality = 1f;
    internal static float EmaPingMs;
    internal static float JitterMs;

    internal static void Sample()
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

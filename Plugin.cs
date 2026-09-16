using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace CoopParryFix;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency(SmoothServerGUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string ModGUID = "Ageous.CoopParryFix";
    public const string ModName = "CoopParryFix";
    public const string ModVersion = "0.4.1";
    public const string SmoothServerGUID = "Nosferatu.SmoothServer";

    internal static Plugin Instance = null!;
    internal static PluginLog Log = null!;
    internal static Harmony Harmony = null!;

    private void Awake()
    {
        Instance = this;
        Log = new PluginLog(Logger, PluginLogPath());
        ModConfig.Bind(Config);
        Harmony = new Harmony(ModGUID);
        Harmony.PatchAll();
        LogSmoothServerCompat();
        string side = Application.isBatchMode ? "dedicated server" : "client";
        Log.LogInfo($"{ModName} {ModVersion} loaded ({side}, same DLL). Vanilla 250ms when you own the attacker. Otherwise CPF_Ping to the owner (install on server and all clients for best results; missing peers fall back).");
    }

    private static void LogSmoothServerCompat()
    {
        try
        {
            if (Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey(SmoothServerGUID))
                Log.LogInfo("SmoothServer detected: CoopParryFix does not use SS_Ping. Owner-path delay uses CPF_Ping/CPF_Pong.");
        }
        catch (Exception ex)
        {
            Log.LogWarning($"SmoothServer detect failed: {ex.Message}");
        }
    }

    private void Update()
    {
        try
        {
            OwnerPing.Tick();
        }
        catch (Exception ex)
        {
            Log.LogError($"CoopParryFix Update failed: {ex}");
        }
    }

    private void OnDestroy()
    {
        Harmony?.UnpatchSelf();
        Log?.Close();
    }

    private string PluginLogPath()
    {
        try
        {
            string? dir = Path.GetDirectoryName(Info.Location);
            if (!string.IsNullOrEmpty(dir))
                return Path.Combine(dir, "CoopParryFix.log");
        }
        catch
        {
            // Fall through to the TMM plugin folder name.
        }

        return Path.Combine(Paths.PluginPath, "Ageous27-CoopParryFix", "CoopParryFix.log");
    }
}

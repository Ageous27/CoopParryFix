using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CoopParryFix;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string ModGUID = "Ageous.CoopParryFix";
    public const string ModName = "CoopParryFix";
    public const string ModVersion = "0.2.2";

    internal static Plugin Instance = null!;
    internal static ManualLogSource Log = null!;
    internal static Harmony Harmony = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        ModConfig.Bind(Config);
        Harmony = new Harmony(ModGUID);
        Harmony.PatchAll();
        Log.LogInfo($"{ModName} {ModVersion} loaded. Vanilla parry is 250ms, plus crowd and latency compensation.");
    }

    private void Update()
    {
        try
        {
            LatencySampler.Tick();
        }
        catch (Exception ex)
        {
            Log.LogError($"CoopParryFix Update failed: {ex}");
        }
    }

    private void OnDestroy()
    {
        Harmony?.UnpatchSelf();
    }
}

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CoopParryFix;

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
internal static class BlockAttackPatch
{
    private static readonly MethodInfo GetWindow =
        AccessTools.Method(typeof(ParryWindow), nameof(ParryWindow.GetSeconds))
        ?? throw new InvalidOperationException("CoopParryFix could not find ParryWindow.GetSeconds");

    private static void Prefix(Humanoid __instance, Character attacker)
    {
        if (!ModConfig.DebugLogs.Value || !__instance)
            return;

        try
        {
            bool localOwner = attacker && attacker.IsOwner();
            string attackerName = attacker ? attacker.name : "null";
            Plugin.Log.LogInfo(
                $"BlockAttack {__instance.name} isPlayer={__instance.IsPlayer()} blocking={__instance.IsBlocking()} attacker={attackerName} localOwner={localOwner}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"CoopParryFix BlockAttack prefix failed: {ex}");
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool replaced = false;
        foreach (var ins in instructions)
        {
            if (!replaced && ins.opcode == OpCodes.Ldc_R4 && ins.operand is float value && Mathf.Abs(value - 0.25f) < 0.0001f)
            {
                replaced = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Call, GetWindow);
                continue;
            }

            yield return ins;
        }

        if (!replaced)
            Plugin.Log.LogError("CoopParryFix: Humanoid.BlockAttack no longer has the 0.25s parry window constant. The patch did not apply.");
        else
            Plugin.Log.LogInfo("CoopParryFix: hooked Humanoid.BlockAttack parry window.");
    }
}

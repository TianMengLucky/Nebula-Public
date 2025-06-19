using HarmonyLib;
using Nebula.Game;
using Nebula.Player;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;

namespace AddonDev;

public class Envoy()
    : DefinedAllocatableModifierTemplate("envoy", "EN", Palette.ImpostorRed.ToVirial(), [], false, true, false), DefinedAllocatableModifier
{
    public static Envoy MyRole = new();
    
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Player player, int[] arguments) => new Instance(player);
    
    public class Instance(Player player) : RuntimeAssignableTemplate(player), RuntimeModifier
    {
        void RuntimeAssignable.OnActivated() { }
        public DefinedModifier Modifier => MyRole;
    }
}

[RolePatcher.RolePatch]
internal static class EnvoyPatch
{
    [HarmonyPatch(typeof(CriteriaManager), nameof(CriteriaManager.Trigger)), HarmonyPrefix]
    private static bool TriggerPatch(GameEnd gameEnd, GameEndReason reason)
    {
        if (gameEnd != NebulaGameEnds.CrewmateGameEnd.Get() || reason != GameEndReason.Task) return true;
        var player = NebulaAPI.CurrentGame?.GetAllPlayers().FirstOrDefault(p => p.TryGetModifier<Envoy.Instance>(out _));
        return player == null || player.IsDead;
    }
}
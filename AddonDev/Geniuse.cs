using HarmonyLib;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Game;
using Virial.Runtime;
using Virial.Text;

namespace AddonDev;

public class Geniuse : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private Geniuse() : base("geniuse", "GEN", new Color(187, 255, 255), [CanBeAwareAssignment])
    {
        NebulaAPI.RegisterTip(new WinConditionTip(geniuseWin, () => (MyRole as ISpawnable).IsSpawnable, 
            () => Language.Translate("document.tip.winCond.geniuse.title"), 
            () => Language.Translate("document.tip.winCond.geniuse")));
    }
    
    private static GameEnd? geniuseWin = NebulaAPI.Preprocessor?.CreateEnd("geniuse", new(253,84,167));
    
    public static BoolConfiguration CanBeAwareAssignment = NebulaAPI.Configurations.Configuration("options.role.geniuse.canBeAwareAssignment", true);

    public static Geniuse MyRole = new();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Player player, int[] arguments) => new Instance(player);
    
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        bool RuntimeAssignable.CanBeAwareAssignment => CanBeAwareAssignment;

        public Instance(Player player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() { }
        
        [OnlyHost]
        void OnExiled(PlayerExiledEvent ev) 
        {
            if (!ev.Player.TryGetModifier<Instance>(out _)) return;
            if (ev.Player.Role.Role.Category != RoleCategory.NeutralRole) return;
            NebulaAPI.CurrentGame?.TriggerGameEnd(geniuseWin, GameEndReason.Special, BitMasks.AsPlayer(1u << ev.Player.PlayerId));
        }
        
        // form 凛
        [Local]
        void CheckWin(PlayerCheckExtraWinEvent ev)
        {
            if (ev.Player.AmOwner && MyPlayer.Role.Role.Category == RoleCategory.CrewmateRole && ev.GameEnd == NebulaGameEnds.ImpostorGameEnd.Get())
                ev.SetWin(true);
        }
        
        [Local]
        void BlockWin(PlayerBlockWinEvent ev)
        {
            ev.SetBlockedIf(hasBlock());
            return;

            bool hasBlock()
            {
                if (MyPlayer.IsDead) return false;
                
                if (MyPlayer.Role.Role.Category == RoleCategory.CrewmateRole && ev.GameEnd == NebulaGameEnds.CrewmateGameEnd.Get())
                    return true;

                if (
                    MyPlayer.Role.Role.Category == RoleCategory.ImpostorRole 
                    && 
                    ev.GameEnd == NebulaGameEnd.ImpostorWin 
                    && 
                    hasOtherImpostor()
                    )
                    return true;

                return false;
            }

            bool hasOtherImpostor()
            {
                var players = NebulaGameManager.Instance?.AllPlayerInfo;
                if (players == null) return false;
                foreach (var player in players)
                {
                    if (player.IsDead) continue;
                    if (player.PlayerId == MyPlayer.PlayerId) continue;
                    if (player.Role.Role.Category == RoleCategory.ImpostorRole) return true;
                }
                
                return false;
            }
        }
    }
}

[RolePatcher.RolePatch]
public static class GeniusePatches
{
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin)), HarmonyPostfix]
    private static void ExileControllerBeginPatch(ExileController __instance, [HarmonyArgument(0)] ref ExileController.InitProperties init)
    {
        if (!GeneralConfigurations.ShowRoleOfExiled || !GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor) return;
        var player = NebulaGameManager.Instance?.GetPlayer(init.networkedPlayer.PlayerId);
        if (player == null) return;
        if (player.Role.Role.Category != RoleCategory.NeutralRole || !player.TryGetModifier<Geniuse.Instance>(out _)) return;
        __instance.ImpostorText.text += "\n" + Language.Translate("role.geniuse.exileText");
    }
}

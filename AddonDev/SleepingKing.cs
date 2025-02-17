using Nebula;
using Nebula.Game;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;

namespace AddonDev;

public class SleepingKing : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private SleepingKing() : base("sleepingKing", "SPK", new Color(245, 233, 150), allocateToCrewmate: false,
        allocateToNeutral: false)
    {
    }

    public RuntimeModifier CreateInstance(Player player, int[] arguments) => new Instance(player);
    
    public static SleepingKing MyModifier = new();

    public class Instance(Player player) : RuntimeAssignableTemplate(player), RuntimeModifier
    {
        public void OnActivated()
        {
        }

        public bool Sleep = true;

        [Local]
        void CheckSleep(GameUpdateEvent ev)
        {
            if (!Sleep) return;
            var impostors = new System.Collections.Generic.List<Player>();
            foreach (var player in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
            {
                if (player.IsDead || player.Role.Role.Category != RoleCategory.ImpostorRole) continue;
                impostors.Add(player);
            }

            if (impostors.Count >= 3) return;
            Sleep = false;
                
            MyPlayer.Unbox().MyControl.SetKillTimer(10f);
        }
        
        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            if (Sleep) 
                ev.Result = KillResult.ObviousGuard;
        }
        

        public bool CanKill(Player player)
        {
            return !Sleep;
        }
        

        public DefinedModifier Modifier => MyModifier;
    }
}
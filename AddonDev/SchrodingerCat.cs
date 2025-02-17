using Nebula;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Game;

namespace AddonDev;

public class SchrodingerCat : DefinedRoleTemplate, DefinedRole
{
	private static Team RoleTeam = new("teams.SchrodingerCat", new Color(115, 115, 115), TeamRevealType.OnlyMe);
	private SchrodingerCat() : base("SchrodingerCat", RoleTeam.Color, RoleCategory.NeutralRole, RoleTeam) { }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Player player, int[] arguments) => new Instance(player);
    
    public static SchrodingerCat MyRole = new();
    public class Instance(Player player) : RuntimeAssignableTemplate(player), RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        void RuntimeAssignable.OnActivated() { }
        
        public bool hasGuard = true;
        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            //Damnedが反射するように発動することは無い
            if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse) return;
            //自殺は考慮に入れない
            if (ev.Killer.PlayerId == MyPlayer.PlayerId) return;

            //Avengerのキルは呪いを貫通する(このあと、Avengerに呪いを起こす)
            if (ev.Killer.Role.Role == Avenger.MyRole && (ev.Killer.Role as Avenger.Instance)?.AvengerTarget == ev.Player) return;

            ev.Result = hasGuard ? KillResult.ObviousGuard : KillResult.Kill;
        }
        
        [OnlyMyPlayer]
        void OnGuard(PlayerGuardEvent ev)
        {
            hasGuard = false;
            var nextRole = ev.Murderer.Role.Role;
            var nextArgs = ev.Murderer.Role.RoleArguments;

            using (RPCRouter.CreateSection("SchrodingerCatAction"))
            {
                MyPlayer.Unbox().RpcInvokerSetRole(nextRole, nextArgs).InvokeSingle();
            }

            if(AmOwner) 
                AmongUsUtil.PlayQuickFlash(Palette.ImpostorRed);
        }
    }
}
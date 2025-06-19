using System.Diagnostics.CodeAnalysis;
using Nebula.Roles.Assignment;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Game;
using Virial.Helpers;

namespace AddonDev;

public class Yandere : DefinedRoleTemplate, DefinedRole
{
    public static readonly RoleTeam Team =
        NebulaAPI.Preprocessor?.CreateTeam("Yandere.Team", new Color(124, 187, 223), TeamRevealType.OnlyMe)!;
    
    public Yandere() : base("yandere", new Color(124, 187, 223), RoleCategory.NeutralRole, Team)
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [YandereLover.MyRole.ConfigurationHolder!]);
    }

    public RuntimeRole CreateInstance(Player player, int[] arguments) => new Instance(player);

    public static Yandere MyRole = new Yandere();

    public class Instance(Player player) : RuntimeAssignableTemplate(player), RuntimeRole
    {
        public void OnActivated()
        {
        }

        public DefinedRole Role => MyRole;
    }
}

public class YandereLover : DefinedAllocatableModifierTemplate,
    DefinedAllocatableModifier
{
    public RuntimeModifier CreateInstance(Player player, int[] arguments) => new Instance(player, arguments.Get(0, player.PlayerId));
    
    public static YandereLover MyRole = new();

    public YandereLover() : base("YandereLover", "YL", Yandere.Team.Color)
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Yandere.MyRole.ConfigurationHolder!]);
    }
    
    void HasAssignmentRoutine.TryAssign(IRoleTable roleTable){
        if (roleTable is RoleTable table)
        {
            var has = new List<int>();
            foreach (var r in table.roles)
            {
                if (r.role != Yandere.MyRole) continue;
                has.Add(r.playerId);
                var players = table.roles.Where(n => !has.Contains(n.playerId) && n.role.CanLoad(this)).ToArray();
                var num = Random.Shared.Next(0, players.Length - 1);
                var id = players[num].playerId;
                has.Add(id);
                table.SetModifier(id, MyRole, [r.playerId]);
            }
        }
    }

    public int AssignPriority => 5;

    public class Instance(GamePlayer player, int YandereId) : RuntimeAssignableTemplate(player), RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        int[]? RuntimeAssignable.RoleArguments => [YandereId];
        public int YandereId { get; } = YandereId;
        
        public void OnActivated()
        {
        }
    }
}
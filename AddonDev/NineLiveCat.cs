using Nebula.Roles;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Compat;
using Virial.Configuration;
using Virial.Game;

namespace AddonDev;

public class NineLiveCat() : DefinedSingleAbilityRoleTemplate<NineLiveCat.Ability>("NineLiveCat",
    new Color(152, 191, 213),
    RoleCategory.CrewmateRole,
    NineLiveCatTeam, [ReOwnership, NumOfLives])
{
    private static RoleTeam NineLiveCatTeam =
        NebulaAPI.Preprocessor?.CreateTeam("NineLiveCat.Team", new Color(152, 191, 213), TeamRevealType.OnlyMe)!;
    
    private static readonly IRelativeCoolDownConfiguration ButtonCoolDown =
        NebulaAPI.Configurations.KillConfiguration("options.role.ninelivecat.buttonCoolDown", CoolDownType.Relative, (10f, 30f, 1.5f), 10f, (10f, 30f, 1.5f), 10f, (0.125f, 2f, 0.125f), 1f);
    
    private static readonly BoolConfiguration ReOwnership = 
        NebulaAPI.Configurations.Configuration("options.role.ninelivecat.ReOwnership", false);
    
    private static readonly IntegerConfiguration NumOfLives = 
        NebulaAPI.Configurations.Configuration("options.role.ninelivecat.NumOfLives", new IntegerSelection(Enumerable.Range(1, 9).ToArray()), 3);


    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public Ability(Player player, bool isUsurped) : base(player, isUsurped)
        {
        }
    }

    public override Ability CreateAbility(Player player, int[] arguments)
    {
        return new Ability(player, arguments.GetAsBool(0));
    }
}
using Virial;
using Virial.Assignable;
using Virial.Game;

namespace AddonDev;

public class Spirit : DefinedSingleAbilityRoleTemplate<Spirit.Ability>, DefinedRole
{
    public static Spirit MyRole = new Spirit();
    
    public Spirit() : base("spirit", new Color(124, 187, 223), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam)
    {
        
    }
    
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public Ability(Player player, bool isUsurped) : base(player, isUsurped)
        {
        }
    }

    public override Ability CreateAbility(Player player, int[] arguments)
    {
        return new Ability(player, false);
    }
}
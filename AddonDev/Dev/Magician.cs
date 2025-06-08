using Virial;
using Virial.Assignable;
using Virial.Game;

namespace AddonDev;

public class Magician() : DefinedRoleTemplate("magician", new Color(90, 160, 200), RoleCategory.CrewmateRole,
    NebulaTeams.CrewmateTeam), DefinedRole
{
    public RuntimeRole CreateInstance(Player player, int[] arguments) => new Instance(player);

    public static Magician MyRole = new();

    public class Instance(Player player) : RuntimeAssignableTemplate(player), RuntimeRole
    {
        public void OnActivated()
        {
        }

        public DefinedRole Role => MyRole;
    }
}
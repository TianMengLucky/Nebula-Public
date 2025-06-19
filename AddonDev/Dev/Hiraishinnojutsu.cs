using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Game;

namespace AddonDev;

public class Hiraishinnojutsu() : DefinedRoleTemplate("hiraishinnojutsu",
    Palette.ImpostorRed.ToVirial(),
    RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam), DefinedRole
{
    public static Hiraishinnojutsu MyRole => new();
    public RuntimeRole CreateInstance(Player player, int[] arguments) => new Instance(player);

    public class Instance(Player player) : RuntimeAssignableTemplate(player), RuntimeRole
    {
        public void OnActivated() { }

        public DefinedRole Role => MyRole;
    }
}
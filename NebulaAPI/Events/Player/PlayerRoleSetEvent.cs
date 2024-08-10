using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Player;

public class PlayerRoleSetEvent : AbstractPlayerEvent
{
    public Assignable.RuntimeRole Role { get; private init; }
    internal PlayerRoleSetEvent(Virial.Game.Player player, Assignable.RuntimeRole role) : base(player) {
        Role = role;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Player;

public class PlayerModifierRemoveEvent : AbstractPlayerEvent
{
    public Assignable.RuntimeModifier Modifier { get; private init; }
    internal PlayerModifierRemoveEvent(Virial.Game.Player player, Assignable.RuntimeModifier modifier) : base(player)
    {
        Modifier = modifier;
    }
}

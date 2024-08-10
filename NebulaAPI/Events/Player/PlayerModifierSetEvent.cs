using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virial.Events.Player;

/// <summary>
/// モディファイアが追加された直後に呼び出されます。
/// </summary>
public class PlayerModifierSetEvent : AbstractPlayerEvent
{
    public Assignable.RuntimeModifier Modifier { get; private init; }
    internal PlayerModifierSetEvent(Virial.Game.Player player, Assignable.RuntimeModifier modifier) : base(player)
    {
        Modifier = modifier;
    }
}

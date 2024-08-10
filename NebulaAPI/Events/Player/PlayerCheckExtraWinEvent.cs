using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Game;

namespace Virial.Events.Player;

internal class PlayerCheckExtraWinEvent : AbstractPlayerEvent
{
    public GameEnd GameEnd { get; private init; }
    public BitMask<Virial.Game.Player> WinnersMask { get; private init; }
    public EditableBitMask<ExtraWin> ExtraWinMask { get; private init; }

    public ExtraWinCheckPhase Phase { get; private init; }
    public bool IsExtraWin { get; set; } = false;
    public void SetWin(bool win) => IsExtraWin = win;

    internal PlayerCheckExtraWinEvent(Virial.Game.Player player, BitMask<Virial.Game.Player> winners, EditableBitMask<ExtraWin> extraWinMask, GameEnd gameEnd, ExtraWinCheckPhase phase) : base(player)
    {
        GameEnd = gameEnd;
        WinnersMask = winners;
        ExtraWinMask = extraWinMask;
        Phase = phase;
    }
}

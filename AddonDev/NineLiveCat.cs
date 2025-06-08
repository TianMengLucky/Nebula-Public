using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;

namespace AddonDev;

public class NineLiveCat() : DefinedRoleTemplate("NineLiveCat",
    NineLiveCatTeam.Color,
    RoleCategory.NeutralRole,
    NineLiveCatTeam, [ButtonCoolDown, ReOwnership, NumOfLives, OwnerKnowCat]),DefinedRole
{
    public static RoleTeam NineLiveCatTeam =
        NebulaAPI.Preprocessor?.CreateTeam("NineLiveCat.Team", new Color(152, 191, 213), TeamRevealType.OnlyMe)!;
    
    private static readonly IRelativeCoolDownConfiguration ButtonCoolDown =
        NebulaAPI.Configurations.KillConfiguration("options.role.NineLiveCat.ButtonCoolDown", CoolDownType.Relative, (10f, 30f, 1.5f), 10f, (10f, 30f, 1.5f), 10f, (0.125f, 2f, 0.125f), 1f);
    
    private static readonly BoolConfiguration ReOwnership = 
        NebulaAPI.Configurations.Configuration("options.role.NineLiveCat.ReOwnership", false);
    
    private static readonly IntegerConfiguration NumOfLives = 
        NebulaAPI.Configurations.Configuration("options.role.NineLiveCat.NumOfLives", new IntegerSelection(Enumerable.Range(1, 9).ToArray()), 3);

    private static readonly BoolConfiguration OwnerKnowCat =
        NebulaAPI.Configurations.Configuration("options.role.NineLiveCat.OwnerKnowCat", true);
    
    

    public static NineLiveCat MyRole = new();

    public static ExtraWin OwnerAndCatWin =
        NebulaAPI.Preprocessor?.CreateExtraWin("NineLiveCat.ExtraWin", NineLiveCatTeam.Color)!;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Player player, int[] arguments) => 
        new Instance(player, arguments.Get(0, player.PlayerId), arguments.Get(1, NumOfLives));
    
    public class Instance(Player player, int catId, int lives) : RuntimeAssignableTemplate(player), RuntimeRole
    {
        public int _lives { get; private set; }= lives;
        public int CatId { get; } = catId;
        
        int[]? RuntimeAssignable.RoleArguments => [CatId, _lives];

        private bool IsMyOwner(Player player)
        {
            return player.GetModifiers<NineLiveCatOwner.Instance>().Any(o => o.CatId == CatId);
        }
        
        private NineLiveCatOwner.Instance? MyOwner
        {
            get
            {
                var ownerPlayer = NebulaAPI.CurrentGame?.GetAllPlayers().FirstOrDefault(IsMyOwner);
                if (ownerPlayer == null) return null;
                if (ownerPlayer.TryGetModifier<NineLiveCatOwner.Instance>(out var owner) && owner.CatId == CatId)
                {
                    return owner;
                }
                return null;
            }
        }
        
        
        [OnlyMyPlayer]
        void CheckExtraWins(PlayerCheckExtraWinEvent ev)
        {
            if (MyOwner == null || MyPlayer.IsDead) return;
            if (!ev.WinnersMask.Test(MyOwner.MyPlayer)) return;
            if (ev.WinnersMask.Test(MyPlayer)) return;

            ev.ExtraWinMask.Add(OwnerAndCatWin);
            ev.IsExtraWin = true;
        }

        void CheckKill(PlayerCheckKilledEvent ev)
        {
            if (_lives < 1) return;
            if (ev.Player == MyPlayer)
            { 
                Guard();
            }

            if (ev.Player.TryGetModifier<NineLiveCatOwner.Instance>(out var owner) && owner.CatId == CatId)
            {
                Guard();
                KillOwnerNotice.Invoke((MyPlayer, ev.Player));
            }

            return;

            void Guard()
            {
                ev.Result = KillResult.Guard;
                _lives--;
                ShareNextRole.Invoke((player: MyPlayer, numOfLive: _lives));
            }
        }
        
        private static Image ButtonSprite = NebulaAPI.AddonAsset.GetResource("RecogniseButton.png")?.AsImage(115f)!;
        private bool hasOwner;
        public void OnActivated()
        {
            if (AmOwner)
            {
                var Content = Language.Translate("role.NineLiveCat.hud.lives");
                Helpers.TextHudContent("NineLiveCatText", this, tmPro =>
                {
                    tmPro.text = string.Format(Content, _lives);
                });
                var tracker = ObjectTrackers.ForPlayer(this, null, MyPlayer, player =>
                    ObjectTrackers.StandardPredicate.Invoke(player) &&
                    !player.TryGetModifier<NineLiveCatOwner.Instance>(out _));
               var RecogniseButton = new ModAbilityButtonImpl().KeyBind(VirtualKeyInput.Ability).Register(this);
               RecogniseButton.SetSprite(ButtonSprite.GetSprite());
               RecogniseButton.Availability = _ => tracker.CurrentTarget != null && MyPlayer.CanMove;
               RecogniseButton.Visibility = _ => !MyPlayer.IsDead && (ReOwnership || !hasOwner);
               RecogniseButton.OnClick = button =>
               {
                   if (hasOwner && MyOwner != null)
                   {
                       MyOwner.MyPlayer.RemoveModifier(NineLiveCatOwner.MyRole);
                       hasOwner = false;
                   }
                   
                   var target = tracker.CurrentTarget!;
                   target.AddModifier(NineLiveCatOwner.MyRole, [CatId]);
                   button.StartCoolDown();
                   hasOwner = true;
                   SetButtonText();
               };
               RecogniseButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, ButtonCoolDown.CoolDown).SetAsAbilityTimer();
               RecogniseButton.StartCoolDown();
               SetButtonText();

               void SetButtonText()
               {
                   RecogniseButton.SetLabel(hasOwner
                       ? "NineLiveCat.hud.ReOwnership"
                       : "NineLiveCat.hud.Recognise");
               }
            }
        }
        
        public static RemoteProcess<(GamePlayer player, int numOfLive)> ShareNextRole = new(
            "ShareNineLiveCatLive",
            (message, _) =>
            {
                if (message.player.Role is Instance cat)
                {
                    cat._lives = message.numOfLive;
                }
            }
        );
        
        private static Image NoticeSprite = NebulaAPI.AddonAsset.GetResource("KillCatOwner.png")?.AsImage(200f)!;
        public static RemoteProcess<(GamePlayer player, GamePlayer owner)> KillOwnerNotice = new(
            "KillOwnerNotice",
            (message, _) =>
            {
                if (message.player.AmOwner)
                {
                    var arrow = new Arrow(NoticeSprite.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true, OnJustPoint = true };
                    arrow.Register(arrow);
                    arrow.TargetPos = message.owner.Position;
                    NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
                    Helpers.GetPlayer(message.owner.PlayerId)?.ShowFailedMurder();
                }
            }
        );
        
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            var ownerKnow = OwnerKnowCat && (MyOwner?.MyPlayer.AmOwner ?? false);
            if (AmOwner || canSeeAllInfo || ownerKnow)
            {
                name += " ♥".Color(NineLiveCatTeam.UnityColor);
            }
        }

        public DefinedRole Role => MyRole;
    }
}

public class NineLiveCatOwner() : DefinedModifierTemplate("nineLiveCatOwner", NineLiveCat.NineLiveCatTeam.Color, [], true, () => false), DefinedModifier
{
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Player player, int[] arguments) => new Instance(player, arguments.Get(0, 0));
    
    public static NineLiveCatOwner MyRole = new();

    public class Instance(Player player, int catId) : RuntimeAssignableTemplate(player), RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        int[]? RuntimeAssignable.RoleArguments => [CatId];
        public int CatId { get; } = catId;
        private NineLiveCat.Instance? MyCat => NebulaAPI.CurrentGame?.GetAllPlayers().FirstOrDefault(p => p.Role is NineLiveCat.Instance cat && cat.CatId == CatId)?.Role as NineLiveCat.Instance;
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            RuntimeRole? myCatRuntimeRole = MyCat;
            var isCat = myCatRuntimeRole?.AmOwner ?? false;
            if (AmOwner || isCat || canSeeAllInfo)
            {
                name += " ♥".Color(NineLiveCat.NineLiveCatTeam.UnityColor);
            }
        }
        
        public void OnActivated()
        {
        }
    }
}
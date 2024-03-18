﻿using BepInEx.Unity.IL2CPP.Utils;
using Nebula.Behaviour;
using Virial;
using Virial.Assignable;
using Virial.Game;

namespace Nebula.Roles.Impostor;

[NebulaRPCHolder]
public class Sniper : ConfigurableStandardRole, HasCitation
{
    static public Sniper MyRole = new Sniper();
    public override RoleCategory Category => RoleCategory.ImpostorRole;

    public override string LocalizedName => "sniper";
    public override Color RoleColor => Palette.ImpostorRed;
    Citation? HasCitation.Citaion => Citations.TownOfImpostors;
    public override RoleTeam Team => Impostor.MyTeam;

    public override RoleInstance CreateInstance(PlayerModInfo player, int[] arguments) => new Instance(player);

    private KillCoolDownConfiguration SnipeCoolDownOption = null!;
    private NebulaConfiguration ShotSizeOption = null!;
    private NebulaConfiguration ShotEffectiveRangeOption = null!;
    private NebulaConfiguration ShotNoticeRangeOption = null!;
    private NebulaConfiguration StoreRifleOnFireOption = null!;
    private NebulaConfiguration StoreRifleOnUsingUtilityOption = null!;
    private NebulaConfiguration CanSeeRifleInShadowOption = null!;
    private NebulaConfiguration CanKillHidingPlayerOption = null!;
    private NebulaConfiguration AimAssistOption = null!;
    private NebulaConfiguration DelayInAimAssistOption = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        SnipeCoolDownOption = new(RoleConfig, "snipeCoolDown", KillCoolDownConfiguration.KillCoolDownType.Immediate, 2.5f, 10f, 60f, -40f, 40f, 0.125f, 0.125f, 2f, 20f, -10f, 1f);
        ShotSizeOption = new(RoleConfig, "shotSize", null, 0.25f, 4f, 0.25f, 1f, 1f) { Decorator = NebulaConfiguration.OddsDecorator };
        ShotEffectiveRangeOption = new(RoleConfig, "shotEffectiveRange", null, 2.5f, 60f, 2.5f, 25f, 25f) { Decorator = NebulaConfiguration.OddsDecorator };
        ShotNoticeRangeOption = new(RoleConfig, "shotNoticeRange", null, 2.5f, 60f, 2.5f, 15f, 15f) { Decorator = NebulaConfiguration.OddsDecorator };
        StoreRifleOnFireOption = new(RoleConfig, "storeRifleOnFire", null, true, true);
        StoreRifleOnUsingUtilityOption = new(RoleConfig, "storeRifleOnUsingUtility", null, false, false);
        CanSeeRifleInShadowOption = new(RoleConfig, "canSeeRifleInShadow", null, false, false);
        CanKillHidingPlayerOption = new(RoleConfig, "canKillHidingPlayer", null, false, false);
        AimAssistOption = new(RoleConfig, "aimAssist", null, false, false);
        DelayInAimAssistOption = new(RoleConfig, "delayInAimAssistActivation", null, 0f, 20f, 1f, 3f, 3f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    [NebulaRPCHolder]
    public class SniperRifle : INebulaScriptComponent, IGameEntity
    {
        public PlayerModInfo Owner { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        private static SpriteLoader rifleSprite = SpriteLoader.FromResource("Nebula.Resources.SniperRifle.png", 100f);
        public SniperRifle(PlayerModInfo owner) : base()
        {
            Owner = owner;
            Renderer = UnityHelper.CreateObject<SpriteRenderer>("SniperRifle", null, owner.MyControl.transform.position, LayerExpansion.GetObjectsLayer());
            Renderer.sprite = rifleSprite.GetSprite();
            Renderer.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            Renderer.gameObject.layer = MyRole.CanSeeRifleInShadowOption ? LayerExpansion.GetObjectsLayer() : LayerExpansion.GetDefaultLayer();
        }

        void IGameEntity.HudUpdate()
        {
            if (Owner.AmOwner) Owner.RequireUpdateMouseAngle();
            Renderer.transform.localEulerAngles = new Vector3(0, 0, Owner.MouseAngle * 180f / Mathf.PI);
            var pos = Owner.MyControl.transform.position + new Vector3(Mathf.Cos(Owner.MouseAngle), Mathf.Sin(Owner.MouseAngle), -1f) * 0.87f;
            var diff = (pos - Renderer.transform.position) * Time.deltaTime * 7.5f;
            Renderer.transform.position += diff;
            Renderer.flipY = Mathf.Cos(Owner.MouseAngle) < 0f;
        }

        void IGameEntity.OnReleased()
        {
            if (Renderer) GameObject.Destroy(Renderer.gameObject);
            Renderer = null!;
        }

        public PlayerModInfo? GetTarget(float width,float maxLength)
        {
            float minLength = maxLength;
            PlayerModInfo? result = null;

            foreach(var p in NebulaGameManager.Instance!.AllPlayerInfo())
            {
                if (p.IsDead || p.AmOwner || ((!MyRole.CanKillHidingPlayerOption) && p.MyControl.inVent)) continue;

                //インポスターは無視
                if (p.Role.Role.Category == RoleCategory.ImpostorRole) continue;
                //不可視なプレイヤーは無視
                if (p.IsInvisible) continue;

                var pos = p.MyControl.GetTruePosition();
                Vector2 diff = pos - (Vector2)Renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-Renderer.transform.eulerAngles.z);

                if(vec.x>0 && vec.x< minLength && Mathf.Abs(vec.y) < width / 2f)
                {
                    result = p;
                    minLength= vec.x;
                }
            }

            return result;
        }
    }

    [NebulaRPCHolder]
    public class Instance : Impostor.Instance, IGamePlayerEntity
    {
        private ModAbilityButton? equipButton = null;
        private ModAbilityButton? killButton = null;

        static private ISpriteLoader buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.SnipeButton.png", 115f);
        static private ISpriteLoader aimAssistSprite = SpriteLoader.FromResource("Nebula.Resources.SniperGuide.png", 100f);
        public override AbstractRole Role => MyRole;
        public SniperRifle? MyRifle = null;
        public override bool HasVanillaKillButton => false;

        AchievementToken<(bool isCleared, bool triggered)>? acTokenAnother = null;
        StaticAchievementToken? acTokenCommon = null;

        public Instance(PlayerModInfo player) : base(player)
        {
        }

        public override void LocalUpdate()
        {
            if (MyRifle != null && MyRole.StoreRifleOnUsingUtilityOption)
            {
                var p = MyPlayer.MyControl;
                if (p.onLadder || p.inMovingPlat || p.inVent) RpcEquip.Invoke((MyPlayer.PlayerId, false));
            }
        }

        public override void OnActivated()
        {
            base.OnActivated();

            if (AmOwner)
            {
                acTokenAnother = AbstractAchievement.GenerateSimpleTriggerToken("sniper.another1");
                AchievementToken<int> acTokenChallenge = new("sniper.challenge", 0, (val, _) => val >= 2);

                equipButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                equipButton.SetSprite(buttonSprite.GetSprite());
                equipButton.Availability = (button) => MyPlayer.MyControl.CanMove;
                equipButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                equipButton.OnClick = (button) =>
                {
                    if (MyRifle == null)
                    {
                        NebulaAsset.PlaySE(NebulaAudioClip.SniperEquip);
                        equipButton.SetLabel("unequip");
                    }
                    else
                        equipButton.SetLabel("equip");

                    RpcEquip.Invoke((MyPlayer.PlayerId, MyRifle == null));

                    if(MyRifle != null)
                    {
                        var circle = UnityHelper.CreateObject<EffectCircle>("Circle", PlayerControl.LocalPlayer.transform, Vector3.zero, LayerExpansion.GetDefaultLayer());
                        circle.Color = Palette.ImpostorRed;
                        circle.OuterRadius = () => MyRole.ShotNoticeRangeOption.GetFloat();
                        var script = circle.gameObject.AddComponent<ScriptBehaviour>();
                        script.UpdateHandler += () =>
                        {
                            if (MyRifle == null) circle.Disappear();
                        };
                        Bind(new GameObjectBinding(circle.gameObject));
                    }
                };
                equipButton.SetLabel("equip");

                killButton = Bind(new ModAbilityButton(isArrangedAsKillButton: true)).KeyBind(Virial.Compat.VirtualKeyInput.Kill);
                killButton.Availability = (button) => MyRifle != null && MyPlayer.MyControl.CanMove;
                killButton.Visibility = (button) => !MyPlayer.MyControl.Data.IsDead;
                killButton.OnClick = (button) =>
                {
                    NebulaAsset.PlaySE(NebulaAudioClip.SniperShot);
                    var target = MyRifle?.GetTarget(MyRole.ShotSizeOption.GetFloat(), MyRole.ShotEffectiveRangeOption.GetFloat());
                    if (target != null)
                    {
                        MyPlayer.MyControl.ModKill(target!.MyControl, false, PlayerState.Sniped, EventDetail.Kill);

                        acTokenCommon ??= new("sniper.common1");
                        if (MyPlayer.MyControl.GetTruePosition().Distance(target!.MyControl.GetTruePosition()) > 20f) acTokenChallenge.Value++;
                    }
                    else
                    {
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, MyPlayer.MyControl, 0);
                    }
                    Sniper.RpcShowNotice.Invoke(MyPlayer.MyControl.GetTruePosition());

                    button.StartCoolDown();

                    if (MyRole.StoreRifleOnFireOption) RpcEquip.Invoke((MyPlayer.PlayerId, false));

                    acTokenAnother.Value.triggered = true;

                };
                killButton.CoolDownTimer = Bind(new Timer(MyRole.SnipeCoolDownOption.CurrentCoolDown).SetAsKillCoolDown().Start());
                killButton.SetLabelType(Virial.Components.AbilityButton.LabelType.Impostor);
                killButton.SetLabel("snipe");
                killButton.SetCanUseByMouseClick();
            }
        }

        void IGamePlayerEntity.OnDead()
        {
            if (AmOwner && MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));

            if (acTokenAnother != null && (MyPlayer.MyState == PlayerState.Guessed || MyPlayer.MyState == PlayerState.Exiled)) acTokenAnother.Value.isCleared |= acTokenAnother.Value.triggered;
        }

        void IGameEntity.OnMeetingStart()
        {
            if (AmOwner)
            {
                if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
                equipButton?.SetLabel("equip");
            }
        }
        void IGameEntity.OnMeetingEnd()
        {
            if (acTokenAnother != null) acTokenAnother.Value.triggered = false;
        }


        IEnumerator CoShowAimAssist()
        {
            IEnumerator CoUpdateAimAssistArrow(PlayerControl player)
            {
                DeadBody? deadBody = null;
                Vector2 pos = Vector2.zero;
                Vector2 dir = Vector2.zero;
                Vector2 tempDir = Vector2.zero;
                bool isFirst = true;

                Color targetColor = new Color(55f / 225f, 1f, 0f);
                float t = 0f;

                SpriteRenderer? renderer = null;

                while (true)
                {
                    if (MeetingHud.Instance || MyPlayer.IsDead || MyRifle == null) break;

                    if (player.Data.IsDead && !deadBody) deadBody = Helpers.GetDeadBody(player.PlayerId);

                    //死亡して、死体も存在しなければ追跡を終了
                    if (player.Data.IsDead && !deadBody) break;

                    if(renderer == null)
                    {
                        renderer = UnityHelper.CreateObject<SpriteRenderer>("AimAssist", HudManager.Instance.transform, Vector3.zero);
                        renderer.sprite = aimAssistSprite.GetSprite();
                    }

                    pos = player.Data.IsDead ? deadBody!.transform.position : player.transform.position;
                    tempDir = (pos - (Vector2)PlayerControl.LocalPlayer.transform.position).normalized;
                    if (isFirst)
                    {
                        dir = tempDir;
                        isFirst = false;
                    }
                    else
                    {
                        dir = (tempDir + dir).normalized;
                    }
                    
                    float angle = Mathf.Atan2(dir.y, dir.x);
                    renderer.transform.eulerAngles = new Vector3(0, 0, angle * 180f / (float)Math.PI);
                    renderer.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2f, Mathf.Sin(angle) * 2f, -30f);

                    t += Time.deltaTime / 0.8f;
                    if (t > 1f) t = 1f;
                    renderer.color = Color.Lerp(Color.white, targetColor, t).AlphaMultiplied(0.6f);

                    yield return null;
                }

                if (renderer == null) yield break;

                float a = 0.6f;
                while(a > 0f)
                {
                    a -= Time.deltaTime / 0.8f;
                    var color = renderer.color;
                    color.a = a;
                    renderer.color = color;
                    yield return null;
                }
                
                GameObject.Destroy(renderer.gameObject);
            }

            yield return new WaitForSeconds(MyRole.DelayInAimAssistOption.GetFloat());

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (!p.AmOwner) NebulaManager.Instance.StartCoroutine(CoUpdateAimAssistArrow(p).WrapToIl2Cpp());
            }
        }

        void EquipRifle()
        {
            MyRifle = Bind(new SniperRifle(MyPlayer));

            if (AmOwner && MyRole.AimAssistOption) NebulaManager.Instance.StartCoroutine(CoShowAimAssist().WrapToIl2Cpp());
        }

        void UnequipRifle()
        {
            if (MyRifle != null) MyRifle.ReleaseIt();
            MyRifle = null;
        }

        static RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipRifle",
        (message, _) =>
        {
            var role = NebulaGameManager.Instance?.GetModPlayerInfo(message.playerId)?.Role;
            if (role is Sniper.Instance sniper)
            {
                if (message.equip)
                    sniper.EquipRifle();
                else
                    sniper.UnequipRifle();
            }
        }
        );
    }

    private static SpriteLoader snipeNoticeSprite = SpriteLoader.FromResource("Nebula.Resources.SniperRifleArrow.png", 200f);
    public static RemoteProcess<Vector2> RpcShowNotice = RemotePrimitiveProcess.OfVector2(
        "ShowSnipeNotice",
        (message, _) =>
        {
            if ((message - (Vector2)PlayerControl.LocalPlayer.transform.position).magnitude < Sniper.MyRole.ShotNoticeRangeOption.GetFloat())
            {
                var arrow = new Arrow(snipeNoticeSprite.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true };
                arrow.TargetPos = message;
                NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
            }
        }
        );
}

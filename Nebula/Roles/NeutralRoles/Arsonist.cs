﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nebula.Patches;
using Nebula.Objects;
using HarmonyLib;
using Hazel;
using Nebula.Game;

namespace Nebula.Roles.NeutralRoles
{
    public class Arsonist : Template.HasAlignedHologram, Template.HasWinTrigger
    {
        static public Color RoleColor = new Color(255f/255f, 103f/255f, 1/255f);

        static private CustomButton arsonistButton;

        private Module.CustomOption douseDurationOption;
        private Module.CustomOption douseCoolDownOption;
        private Module.CustomOption douseRangeOption;
        private Module.CustomOption canUseVentsOption;

        public bool WinTrigger { get; set; } = false;
        public byte Winner { get; set; } = Byte.MaxValue;

        public override void LoadOptionData()
        { 
            douseDurationOption = CreateOption(Color.white, "douseDuration", 3f, 1f, 10f, 0.5f);
            douseDurationOption.suffix = "second";

            douseCoolDownOption = CreateOption(Color.white, "douseCoolDown", 10f, 0f, 60f, 2.5f);
            douseCoolDownOption.suffix = "second";

            douseRangeOption = CreateOption(Color.white, "douseRange", 1f, 0.5f, 2f, 0.125f);
            douseRangeOption.suffix = "cross";


            canUseVentsOption = CreateOption(Color.white, "canUseVents", true);
        }


        Sprite douseSprite,igniteSprite;
        public Sprite getDouseButtonSprite()
        {
            if (douseSprite) return douseSprite;
            douseSprite = Helpers.loadSpriteFromResources("Nebula.Resources.DouseButton.png", 115f);
            return douseSprite;
        }

        public Sprite getIgniteButtonSprite()
        {
            if (igniteSprite) return igniteSprite;
            igniteSprite = Helpers.loadSpriteFromResources("Nebula.Resources.IgniteButton.png", 115f);
            return igniteSprite;
        }

        static private bool canIgnite = false;

        public override void GlobalInitialize(PlayerControl __instance)
        {
            base.GlobalInitialize(__instance);
        }

        public override void GlobalIntroInitialize(PlayerControl __instance)
        {
            canMoveInVents = canUseVentsOption.getBool();
            VentPermission = canUseVentsOption.getBool() ? VentPermission.CanUseUnlimittedVent : VentPermission.CanNotUse;
        }

        public override void Initialize(PlayerControl __instance)
        {
            base.Initialize(__instance);

            canIgnite = false;
            WinTrigger = false;
        }

        public override void CleanUp()
        {
            base.CleanUp();

            canIgnite = false;
            WinTrigger = false;

            if (arsonistButton != null)
            {
                arsonistButton.Destroy();
                arsonistButton = null;
            }
        }

        public override void OnMeetingEnd()
        {
            base.OnMeetingEnd();

            CheckIgnite();
        }

        private bool CheckIgnite()
        {
            bool cannotIgnite = false;
            foreach (var entry in PlayerIcons)
            {
                if (!entry.Value.gameObject.active) continue;
                if (activePlayers.Contains(entry.Key)) continue;

                cannotIgnite = true; break;
            }

            if (!cannotIgnite)
            {
                //点火可能
                arsonistButton.Sprite = getIgniteButtonSprite();
                arsonistButton.SetLabel("button.label.ignite");
                canIgnite = true;
                arsonistButton.Timer = 0f;
            }
            else
            {
                arsonistButton.Timer = arsonistButton.MaxTimer;
            }

            return !cannotIgnite;
        }

        public override void ButtonInitialize(HudManager __instance)
        {
            if (arsonistButton != null)
            {
                arsonistButton.Destroy();
            }
            arsonistButton = new CustomButton(
                () => {
                    if (canIgnite)
                    {
                        arsonistButton.isEffectActive = false;
                        arsonistButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                        RPCEventInvoker.WinTrigger(this);
                    }
                },
                () => { return !PlayerControl.LocalPlayer.Data.IsDead; },
                () => {
                    if (arsonistButton.isEffectActive && Game.GameData.data.myData.currentTarget == null)
                    {
                        arsonistButton.Timer = 0f;
                        arsonistButton.isEffectActive = false;
                    }
                    return PlayerControl.LocalPlayer.CanMove && (Game.GameData.data.myData.currentTarget!=null|| canIgnite); },
                () => {
                    arsonistButton.Timer = arsonistButton.MaxTimer;
                    arsonistButton.isEffectActive = false;
                    arsonistButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
                },
                getDouseButtonSprite(),
                new Vector3(-1.8f, 0, 0),
                __instance,
                KeyCode.F,
                true,
                douseDurationOption.getFloat(),
                () => {
                    if (Game.GameData.data.myData.currentTarget != null)
                    {
                        activePlayers.Add(Game.GameData.data.myData.currentTarget.PlayerId);
                        Game.GameData.data.myData.currentTarget = null;
                    }

                    CheckIgnite();
                },
                false,
                "button.label.douse"
            );
            arsonistButton.MaxTimer = douseCoolDownOption.getFloat();
            arsonistButton.EffectDuration = douseDurationOption.getFloat();
        }


        public override void MyPlayerControlUpdate()
        {
            base.MyPlayerControlUpdate();

            Game.MyPlayerData data = Game.GameData.data.myData;
            data.currentTarget = Patches.PlayerControlPatch.SetMyTarget(1.8f*douseRangeOption.getFloat(), false, false, activePlayers);
            Patches.PlayerControlPatch.SetPlayerOutline(data.currentTarget, Color.yellow);
        }

        public override void OnRoleRelationSetting()
        {
            RelatedRoles.Add(Roles.Empiric);
            RelatedRoles.Add(Roles.EvilAce);
        }

        public Arsonist()
            : base("Arsonist", "arsonist", RoleColor, RoleCategory.Neutral, Side.Arsonist, Side.Arsonist,
                 new HashSet<Side>() { Side.Arsonist }, new HashSet<Side>() { Side.Arsonist },
                 new HashSet<Patches.EndCondition>() { Patches.EndCondition.ArsonistWin },
                 true, VentPermission.CanUseUnlimittedVent, true, false, false)
        {
            arsonistButton = null;

            Patches.EndCondition.ArsonistWin.TriggerRole = this;
        }
    }
}

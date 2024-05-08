﻿using AmongUs.GameOptions;
using Nebula.Behaviour;
using Virial.Assignable;

namespace Nebula.Roles.Crewmate;

public class Doctor : ConfigurableStandardRole
{
    static public Doctor MyRole = new Doctor();

    public override RoleCategory Category => RoleCategory.CrewmateRole;

    public override string LocalizedName => "doctor";
    public override Color RoleColor => new Color(128f / 255f, 255f / 255f, 221f / 255f);
    public override RoleTeam Team => Crewmate.MyTeam;

    public override RoleInstance CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    private NebulaConfiguration PortableVitalsChargeOption = null!;
    private NebulaConfiguration MaxPortableVitalsChargeOption = null!;
    private NebulaConfiguration ChargesPerTasksOption = null!;

    protected override void LoadOptions()
    {
        base.LoadOptions();

        PortableVitalsChargeOption = new(RoleConfig, "portableVitalsCharge", null, 2.5f, 60f, 2.5f, 10f, 10f) { Decorator = NebulaConfiguration.SecDecorator };
        MaxPortableVitalsChargeOption = new(RoleConfig, "maxPortableVitalsCharge", null, 2.5f, 60f, 2.5f, 10f, 10f) { Decorator = NebulaConfiguration.SecDecorator };
        ChargesPerTasksOption = new(RoleConfig, "chargesPerTasks", null, 0.5f, 10f, 0.5f, 1f, 1f) { Decorator = NebulaConfiguration.SecDecorator };
    }

    public class Instance : Crewmate.Instance
    {
        private ModAbilityButton? vitalButton = null;
        public override AbstractRole Role => MyRole;
        private float vitalTimer = MyRole.PortableVitalsChargeOption.GetFloat();

        public Instance(GamePlayer player) : base(player)
        {
        }

        public override void OnTaskCompleteLocal()
        {
            vitalTimer = Mathf.Min(MyRole.MaxPortableVitalsChargeOption.GetFloat(), vitalTimer + MyRole.ChargesPerTasksOption.GetFloat());
        }

        public override void OnActivated()
        {
            StaticAchievementToken? acTokenCommon = null;
            StaticAchievementToken? acTokenChallenge = null;

            if (AmOwner)
            {
                vitalButton = Bind(new ModAbilityButton()).KeyBind(Virial.Compat.VirtualKeyInput.Ability);
                vitalButton.SetSprite(HudManager.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton].Image);
                vitalButton.Availability = (button) => MyPlayer.CanMove && vitalTimer > 0f;
                vitalButton.Visibility = (button) => !MyPlayer.IsDead;
                vitalButton.OnClick = (button) =>
                {
                    VitalsMinigame? vitalsMinigame = null;
                    foreach (RoleBehaviour role in RoleManager.Instance.AllRoles)
                    {
                        if (role.Role == RoleTypes.Scientist)
                        {
                            vitalsMinigame = UnityEngine.Object.Instantiate(role.gameObject.GetComponent<ScientistRole>().VitalsPrefab, Camera.main.transform, false);
                            break;
                        }
                    }
                    if (vitalsMinigame == null) return;
                    vitalsMinigame.transform.SetParent(Camera.main.transform, false);
                    vitalsMinigame.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                    vitalsMinigame.Begin(null);

                    ConsoleTimer.MarkAsNonConsoleMinigame();

                    vitalsMinigame.BatteryText.gameObject.SetActive(true);
                    vitalsMinigame.BatteryText.transform.localPosition = new Vector3(2.2f, -2.45f, 0f);
                    foreach (var sprite in vitalsMinigame.BatteryText.gameObject.GetComponentsInChildren<SpriteRenderer>()) sprite.transform.localPosition = new Vector3(-0.45f, 0f);

                    IEnumerator CoUpdate()
                    {
                        acTokenCommon ??= new("doctor.common1");

                        int lastAliveCount = NebulaGameManager.Instance!.AllPlayerInfo().Count(p => !p.IsDead);

                        while (vitalsMinigame.amClosing != Minigame.CloseState.Closing)
                        {
                            vitalTimer -= Time.deltaTime;
                            if (vitalTimer < 0f)
                            {
                                vitalsMinigame.BatteryText.gameObject.SetActive(false);
                                break;
                            }

                            vitalsMinigame.BatteryText.text = Language.Translate("role.doctor.gadgetLeft").Replace("%SECOND%", string.Format("{0:f1}", vitalTimer));

                            yield return null;
                        }

                        //生存者の数に変化がある
                        if(lastAliveCount != NebulaGameManager.Instance!.AllPlayerInfo().Count(p => !p.IsDead))
                            acTokenChallenge = new("doctor.challenge");
                        

                        if (vitalsMinigame.amClosing != Minigame.CloseState.Closing) vitalsMinigame.Close();
                    }

                    vitalsMinigame.StartCoroutine(CoUpdate().WrapToIl2Cpp());
                };
                vitalButton.SetLabelType(Virial.Components.AbilityButton.LabelType.Utility);
                vitalButton.SetLabel("vital");
            }
        }
    }
}

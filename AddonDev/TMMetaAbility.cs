using Virial;
using Virial.Attributes;
using Virial.DI;
using Virial.Game;

namespace AddonDev;

[NebulaPreprocess(PreprocessPhase.BuildNoSModule)]
public class TMMetaAbility : DependentLifespan, IGameOperator, IModule
{
    static TMMetaAbility() => DIManager.Instance.RegisterGeneralModule<IGameModeFreePlay>(() => new TMMetaAbility().Register(NebulaAPI.CurrentGame!));
    public TMMetaAbility()
    {
    }
}
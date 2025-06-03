global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;

global using GamePlayer = Virial.Game.Player;

using System.Reflection;
using HarmonyLib;
using Virial.Attributes;
using Virial.Runtime;

namespace AddonDev;

public static class AddonLib
{
    public static Harmony AddonHarmony = new("meng.tian.addon.nebula");

    public static Virial.Color ToVirial(this UnityEngine.Color color)
    {
        return new Virial.Color(color.r, color.g, color.b, color.a);
    }
}

[NebulaPreprocess(PreprocessPhase.PostRoles)]
internal static class RolePatcher
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class RolePatch : Attribute;
    
    private static void Preprocess(NebulaPreprocessor _)
    {
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetCustomAttribute<RolePatch>() != null);
        foreach (var type in types)
        {
            AddonLib.AddonHarmony.PatchAll(type);
        }
    }
}
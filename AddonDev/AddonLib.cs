global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using static AddonDev.AddonLib;

global using GamePlayer = Virial.Game.Player;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Nebula;
using Nebula.Behavior;
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

    private static ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("TianMengAddon");
    private static NebulaLog nebula => NebulaPlugin.Log;

    public static void LogMessage(string message)
    {
        log.LogMessage(message);
        nebula.Print(NebulaLog.LogLevel.Log, message);
    }

    public static void LogInfo(string message)
    {
        log.LogInfo(message);
        nebula.Print(NebulaLog.LogLevel.Log, message);
    }

    public static void LogWarning(string message)
    {
        log.LogWarning(message);
        nebula.Print(NebulaLog.LogLevel.Warning, message);
    }

    public static void LogFatal(string message)
    {
        log.LogFatal(message);
        nebula.Print(NebulaLog.LogLevel.FatalError, message);
    }

    public static void LogError(string message)
    {
        log.LogError(message);
        nebula.Print(NebulaLog.LogLevel.Error, message);
    }

    public static void LogException(Exception e)
    {
        LogError(e.ToString());
    }
}

[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
internal static class FixPatches
{
    private static void Preprocess(NebulaPreprocessor _)
    {
        AddonHarmony.PatchAll(typeof(FixPatches));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NebulaPlugin), nameof(NebulaPlugin.AllowHttpCommunication), MethodType.Getter)]
    private static bool OnlineMarketplacePrefix(ref bool __result)
    {
        __result = false;
        return false;
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
            try
            {
                AddonHarmony.PatchAll(type);
                LogInfo($"Role Patched {type.FullName}");
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }
    }
}
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using static AddonDev.AddonLib;

global using GamePlayer = Virial.Game.Player;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using BepInEx.Logging;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Nebula;
using Nebula.Utilities;
using Virial;
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

[RolePatcher.RolePatch]
internal static class ResourceReplace
{
    internal static readonly Dictionary<(string, float), SpriteLoader> spriteCacheDic = new();
    internal static Dictionary<string, string>? spriteAndPath;
    
    
    [HarmonyPatch(typeof(SpriteLoader), nameof(SpriteLoader.FromResource)), HarmonyPrefix]
    private static bool SpriteLoader_FormResource(string address, float pixelsPerUnit, ref SpriteLoader __result)
    {
        if (spriteAndPath == null) return true;
        if (spriteCacheDic.TryGetValue((address, pixelsPerUnit), out var spriteLoader))
        {
            __result = spriteLoader;
            return false;
        }

        if (TryGetSprite(address, pixelsPerUnit, out var get))
        {
            __result = get;
            spriteCacheDic[(address, pixelsPerUnit)] = get;
            return false;
        }
        
        return true;
    }

    private static bool TryGetSprite(string address, float pixelsPerUnit, [MaybeNullWhen(false)] out SpriteLoader spriteLoader)
    {
        spriteLoader = null;
        try
        {
            if (spriteAndPath == null)
                return false;

            if (!spriteAndPath.TryGetValue(address, out var path))
            {
                return false;
            }
            
            var stream = NebulaAPI.AddonAsset.GetResource(path)?.AsStream();
            if (stream == null)
            {
                LogWarning($"Sprite {address} not Stream");
                return false;
            }
            
            spriteLoader = new SpriteLoader(new UnloadTextureLoader(stream.ReadBytes()), pixelsPerUnit);
            return true;
        }
        catch (Exception e)
        {
            LogWarning(e.ToString());
            spriteLoader = null;
            return false;
        }
    }
}

[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
internal static class ConfigLoad
{
    private static void Preprocess(NebulaPreprocessor _)
    {
        try
        {
            var jsonFile = NebulaAPI.AddonAsset.GetResource("ReplaceSprite.json");
            if (jsonFile == null)
            {
                LogWarning("ReplaceSprite.json not found");
                return;
            }

            var text = jsonFile.AsStream()?.ReadToEnd();
            if (text == null)
            {
                LogWarning("ReplaceSprite.json is empty");
                return;
            }

            if (HasJsonLib())
            {
                ResourceReplace.spriteAndPath = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
                LogInfo("Has System.Text.Json, Use JsonSerializer");
            }
            else
            {
                ResourceReplace.spriteAndPath =
                    JsonStructure.Deserialize<Dictionary<string, string>>(text);
                LogInfo("Not Has System.Text.Json, Use Nebula.JsonStructure");
            }
            
            LogInfo($"ReplaceSprite.json loaded Path:{ResourceReplace.spriteAndPath?.Count ?? 0}");
        }
        catch (Exception e)
        {
            LogWarning(e.ToString());
        }
    }

    private static bool HasJsonLib()
    {
        return AssemblyLoadContext.Default.Assemblies.Any(n => n.GetName().Name == "System.Text.Json");
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
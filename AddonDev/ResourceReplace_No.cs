using System.Diagnostics.CodeAnalysis;
using BepInEx;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Nebula.Modules;
using Nebula.Utilities;
using Virial.Attributes;
using Virial.Media;
using Virial.Runtime;

namespace ResourceReplace;

internal static class ResourceReplace
{
    internal static readonly Dictionary<(string, float), SpriteLoader> spriteCacheDic = new();
    internal static readonly Dictionary<string, ReplaceInfo> spriteAndPath = new ();
    internal class ReplaceInfo
    {
        public bool IsAddon;
        public bool IsExternal;
        public readonly string Path;
        public NebulaAddon? Addon { get; set; }

        public ReplaceInfo(string path, NebulaAddon addon)
        {
            IsAddon = true;
            Addon = addon;
            Path = path;
        }

        public ReplaceInfo(string path)
        {
            IsExternal = true;
            Path = path;
        }

        public Stream? GetStream()
        {
            if (IsAddon)
            {
                IResourceAllocator? allocator = Addon;
                return allocator?.GetResource(Path)?.AsStream();
            }

            if (IsExternal)
            {
                return !File.Exists(Path) ? null : File.OpenRead(Path);
            }

            return null;
        }
    }
    
    
    [HarmonyPatch(typeof(SpriteLoader), nameof(SpriteLoader.FromResource)), HarmonyPrefix]
    private static bool SpriteLoader_FormResource(string address, float pixelsPerUnit, ref SpriteLoader __result)
    {
        if (spriteAndPath.Count == 0) return true;
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
            if (spriteAndPath.Count == 0)
                return false;

            if (!spriteAndPath.TryGetValue(address, out var replace))
            {
                return false;
            }
            
            
            var stream = replace.GetStream();
            if (stream == null)
            {
                LogWarning($"Sprite {address} not Stream");
                return false;
            }
            
            spriteLoader = new SpriteLoader(new UnloadTextureLoader(stream.ReadBytes()), pixelsPerUnit);
            LogInfo($"Replace {address} {pixelsPerUnit} to {replace.Addon?.Id ?? ""} {replace.Path}");
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
    private static IResourceAllocator AsResource(this NebulaAddon addon) => addon;
    private static void Preprocess(NebulaPreprocessor _)
    {
        AddonHarmony.PatchAll(typeof(ResourceReplace));

        foreach (var addon in NebulaAddon.AllAddons)
        {
            LoadFormAddon(addon);
        }

        var dirPath = Path.Combine(Paths.GameRootPath, "Replace");
        LoadFormExternal(dirPath);
    }

    private static void LoadFormAddon(NebulaAddon addon)
    {
        try
        {
            var jsonFile = addon.AsResource().GetResource("ReplaceSprite.json");
            if (jsonFile == null)
            {
                LogInfo($"{addon.Id} ReplaceSprite.json not found");
                return;
            }

            var text = jsonFile.AsStream()?.ReadToEnd();
            if (text == null)
            {
                LogWarning($"{addon.Id} ReplaceSprite.json is empty");
                return;
            }
            
            var dic = JsonStructure.Deserialize<Dictionary<string, string>>(text);
            if (dic == null)
            {
                LogWarning($"{addon.Id} ReplaceSprite.json is not valid json");
                return;
            }

            var count = 0;
            foreach (var (key,value) in dic)
            {
                if (ResourceReplace.spriteAndPath.ContainsKey(key))
                {
                    LogWarning($"{key} is already, {addon.Id} not replace");
                    continue;
                }
                
                ResourceReplace.spriteAndPath[key] = new ResourceReplace.ReplaceInfo(value, addon);
            }
            
            LogInfo($"{addon.Id} ReplaceSprite.json loaded Path:{count}");
        }
        catch (Exception e)
        {
            LogWarning(e.ToString());
        }
    }

    private static void LoadFormExternal(string dirPath)
    {
        try
        {
            var json = Path.Combine(dirPath, "ReplaceSprite.json");
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                File.WriteAllText(json, new Dictionary<string, string>
                {
                    {"Default", ""}
                }.Serialize());
                return;
            }
            
            if (!File.Exists(json))
            {
                LogWarning("Dir ReplaceSprite.json not found");
                return;
            }

            var dic = JsonStructure.Deserialize<Dictionary<string, string>>(File.ReadAllText(json));
            if (dic == null)
            {
                LogWarning("Dir ReplaceSprite.json is not valid json");
                return;
            }
            
            foreach (var (key, value) in dic)
            {
                if (string.IsNullOrEmpty(key) || key == "Default") continue;
                var path = Path.Combine(dirPath, value);
                if (!File.Exists(path))
                    continue;

                if (ResourceReplace.spriteAndPath.ContainsKey(key))
                {
                    LogWarning($"{key} is already, external not replace");
                    continue;
                }
                
                ResourceReplace.spriteAndPath[key] = new ResourceReplace.ReplaceInfo(path);
            }
        }
        catch (Exception e)
        {
            LogWarning(e.ToString());
        }
    }
}
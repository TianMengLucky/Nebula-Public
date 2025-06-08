using System.IO.Compression;
using System.Text.Json;

namespace AddonBuilder;

internal static class Program
{
    public static AddonConfig? CurrentAddonConfig { get; set; }
    public static async Task Main()
    {
        var dir = Directory.GetCurrentDirectory();
        var DevDir = Path.Combine(dir, "AddonDev");
        var ResourceDir = Path.Combine(DevDir, "Resources");
        var LanguageDir = Path.Combine(DevDir, "Languages");
        Console.WriteLine($"""
                          Current Directory: {dir}
                          AddonDev Directory: {DevDir}
                          Resource Directory: {ResourceDir}
                          Language Directory: {LanguageDir}
                          """);

        if (!File.Exists("Addon.json"))
        {
            Console.WriteLine("Addon.json not found");
            return;
        }
        
        var addonConfig = JsonSerializer.Deserialize<AddonConfig>(await File.ReadAllTextAsync("Addon.json"));
        if (addonConfig == null)
        {
            Console.WriteLine("Addon.json is not valid");
            return;
        }

        CurrentAddonConfig = addonConfig;
        
        var OutDir = Path.Combine(dir, "Output");
        var PackCacheDir = Path.Combine(OutDir, "PackCache");
        var AddonCacheDir = Path.Combine(PackCacheDir, addonConfig.Id);
        Console.WriteLine($"""
                           Output Directory: {OutDir}
                           PackCache Directory: {PackCacheDir}
                           AddonCache Directory: {AddonCacheDir}
                           """);
        
        await CheckDir(OutDir, PackCacheDir, AddonCacheDir);
        await CleanDir(AddonCacheDir);
        
        var CacheResourceDir = Path.Combine(AddonCacheDir, "Resources");
        var CacheLanguageDir = Path.Combine(AddonCacheDir, "Language");
        var CacheScriptDir = Path.Combine(AddonCacheDir, "Scripts");
        await CheckDir(CacheResourceDir, CacheLanguageDir, CacheScriptDir);

        var addonMetaPath = Path.Combine(AddonCacheDir, "addon.meta");
        await File.WriteAllTextAsync(addonMetaPath, addonConfig.GetMetaString());

        var behaviorPath = Path.Combine(CacheScriptDir, ".behaviour");
        if (addonConfig.UseHiddenMembers || addonConfig.LoadRoles)
            await File.WriteAllTextAsync(behaviorPath, addonConfig.GetBehaviourString());

        if (Directory.Exists(LanguageDir))
            await CopyLanguageDir(LanguageDir, CacheLanguageDir);

        if (Directory.Exists(ResourceDir))
            await CopyResourceDir(ResourceDir, CacheResourceDir);

        await CopyScriptDir(DevDir, CacheScriptDir);
        await WriteServer(AddonCacheDir);
        
        var devPostFix = addonConfig.Dev ? $"_Dev{await GetLastDevId(OutDir)}" : "";
        var zipFilePath = Path.Combine(OutDir, $"{addonConfig.Id}@{addonConfig.Version}{devPostFix}.zip");
        await PackZip(AddonCacheDir, zipFilePath);

        await CleanDir(PackCacheDir);
    }

    private static async Task WriteServer(string cachePath)
    {
        if (CurrentAddonConfig == null)
            return;

        if (CurrentAddonConfig.Servers.Count == 0)
            return;

        var filePath = Path.Combine(cachePath, "CustomServer.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(CurrentAddonConfig.Servers));
    }

    private static async Task<int> GetLastDevId(string outPath)
    {
        const int defaultId = 1;
        var path = Path.Combine(outPath, "OutDevVersion.dat");
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, defaultId.ToString());
            return defaultId;
        }
        var devIdText = await File.ReadAllTextAsync(path);
        if (int.TryParse(devIdText, out var id))
        {
            id++;
            await File.WriteAllTextAsync(path, id.ToString());
            return id;
        }
        await File.WriteAllTextAsync(path, defaultId.ToString());
        return defaultId;
    }

    private static async Task PackZip(string dir, string zipFilePath)
    {
        try
        {
            Console.WriteLine($"正在打包 {dir}");
            var ZipFileStream = new MemoryStream();
            ZipFile.CreateFromDirectory(dir, ZipFileStream);
            Console.WriteLine("打包完成");
            Console.WriteLine($"正在写入到{zipFilePath}");
            var writeStream = File.OpenWrite(zipFilePath);
            ZipFileStream.Seek(0, SeekOrigin.Begin);
            await ZipFileStream.CopyToAsync(writeStream);
            writeStream.Close();
            Console.WriteLine("写入完成");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private static async Task CopyScriptDir(string sourceDir, string targetDir)
    {
        try
        {
            Console.WriteLine("正在复制脚本");
            await ReCreateAllDir(sourceDir, targetDir, noCreate:["Resources", "Languages", "Color", "MoreCosmic"]);
            await CopyAllFile(sourceDir, targetDir, "*.cs");
            Console.WriteLine("复制完成");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static async Task CopyLanguageDir(string sourceDir, string targetDir)
    {
        try
        {
            Console.WriteLine("正在复制语言文件");
            await CopyAllFile(sourceDir, targetDir, "*.dat", SearchOption.TopDirectoryOnly);
            Console.WriteLine("复制完成");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    

    private static async Task CopyResourceDir(string sourceDir, string targetDir)
    {
        try
        {
            Console.WriteLine("正在复制资源文件");
            await ReCreateAllDir(sourceDir, targetDir);
            await CopyAllFile(sourceDir, targetDir);
            Console.WriteLine("复制完成");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static Task CopyAllFile(string sourceDir, string targetDir, string pattern = "",
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        foreach (var file in Directory.GetFiles(sourceDir, pattern, searchOption))
        {
            if (Directory.Exists(file) || !File.Exists(file)) continue;
            var emptyPath = file.Replace(sourceDir + "\\", string.Empty);
            if (emptyPath.Contains("Empty")) continue;
            if (emptyPath.Contains("obj") || emptyPath.Contains("bin")) continue;
            if (!(CurrentAddonConfig?.Dev ?? false) && emptyPath.Contains("Dev")) continue;
            var newPath = Path.Combine(targetDir, emptyPath);
            File.Copy(file, newPath);
            Console.WriteLine($"Copy {file} to {newPath}");
        }
        
        return Task.CompletedTask;
    }

    private static Task ReCreateAllDir(string sourceDir, string targetDir, string pattern = "", SearchOption searchOption = SearchOption.AllDirectories, string[]? noCreate = null)
    {
        foreach (var sd in Directory.GetDirectories(sourceDir, pattern, searchOption))
        {
            var emptyPath = sd.Replace(sourceDir + "\\", string.Empty);
            if (emptyPath.Contains("obj") || emptyPath.Contains("bin")) continue;
            if (!(CurrentAddonConfig?.Dev ?? false) && emptyPath.Contains("Dev")) continue;
            if (noCreate != null && noCreate.Any(n => emptyPath.Contains(n))) continue;
            var newPath = Path.Combine(targetDir, emptyPath);
            Directory.CreateDirectory(newPath);
            Console.WriteLine($"ReCreate Directory: {newPath}");
        }
        
        return Task.CompletedTask;
    }

    private static Task CleanDir(params string[] dirs)
    {
        foreach (var dir in dirs)
        {
            try
            {
                if (!Directory.Exists(dir)) continue;
                Console.WriteLine("正在清理目录:" + dir);
                foreach (var d in Directory.GetFiles(dir, "", SearchOption.AllDirectories))
                { 
                    if (!File.Exists(d)) continue;
                    File.Delete(d);
                    Console.WriteLine("Delete File: " + d);
                }

                foreach (var d in Directory.GetDirectories(dir, "", SearchOption.AllDirectories))
                {
                    if (!Directory.Exists(d)) continue;
                    Directory.Delete(d, true);
                    Console.WriteLine("Delete Directory: " + d);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return Task.CompletedTask;
    }

    private static Task CheckDir(params string[] dirs)
    {
        foreach (var dir in dirs)
        {
            if (Directory.Exists(dir)) continue;
            Directory.CreateDirectory(dir);
            Console.WriteLine("Create Directory: " + dir);
        }

        return Task.CompletedTask;
    }
}
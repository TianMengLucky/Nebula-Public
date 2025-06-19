using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace NebulaLoader;

internal class ReleaseContent
{
    public string? name { get; set; } = null!;
}

[BepInPlugin("jp.dreamingpig.amongus.nebula.loader", "NebulaLoader", "1.0.0")]
[BepInProcess("Among Us.exe")]
public class NebulaLoader : BasePlugin
{
    private static readonly string[] AllGithubProxy =
    [
        "https://gh.llkk.cc",
        "https://github.moeyy.xyz",
        "https://ghproxy.cn",
        "https://ghproxy.net",
        "https://gitproxy.click",
        "https://github.tbedu.top"
    ];

    public static string CurrentProxy = "";

    private static NebulaLoader MyPlugin = null!;

    public static ConfigEntry<bool> SkipCheckingConsistency { get; private set; }
    public static ConfigEntry<bool> IgnoringVersionConsistencyOnUpdate { get; private set; }
    public static ConfigEntry<bool> UseSnapshot { get; private set; }
    public static ConfigEntry<bool> AutoUpdate { get; private set; }
    public static ConfigEntry<bool> AutoUpdateIfVersionMismatch { get; private set; }

    private static long GetVanillaSize()
    {
        if (!File.Exists("GameAssembly.dll")) return -1;

        var file = new FileInfo("GameAssembly.dll");
        return file.Length;
    }

    private static string GetTagsUrl(int page)
    {
        return ConvertUrl("https://api.github.com/repos/Dolly1016/Nebula/tags?per_page=100&page=" + page);
    }

    private async Task<List<(string Tag, string Category, int Epoch, int Build, string VisualName)>> FetchAsync(
        HttpClient http)
    {
        List<(string Tag, string Category, int Epoch, int Build, string VisualName)> releases = [];

        var page = 1;
        while (true)
        {
            var response = await http.GetAsync(GetTagsUrl(page));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _Log?.LogError("Bad Response: " + response.StatusCode);
                break;
            }

            var json = await response.Content.ReadAsStringAsync();

            var tags = JsonSerializer.Deserialize<ReleaseContent[]>(json);

            if (tags != null)
                foreach (var tag in tags)
                    if (tag.name != null)
                    {
                        var strings = tag.name.Split(",");
                        if (strings.Length != 4) continue;

                        if (!int.TryParse(strings[2], out var epoch)) continue;
                        if (!int.TryParse(strings[3], out var build)) continue;
                        releases.Add(new ValueTuple<string, string, int, int, string>(tag.name, strings[0], epoch,
                            build, strings[1]));
                    }

            if (tags == null || tags.Length == 0) break;
            page++;
        }

        releases.Sort((v1, v2) => v1.Epoch != v2.Epoch ? v2.Epoch - v1.Epoch : v2.Build - v1.Build);
        return releases;
    }

    private static async Task UpdateAsync(HttpClient http, string tag, string dllFilePath)
    {
        var url = ConvertUrl($"https://github.com/Dolly1016/Nebula/releases/download/{tag}/Nebula.dll");
        var response = await http.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK) return;
        var dllStream = await response.Content.ReadAsStreamAsync();

        try
        {
            if (File.Exists(dllFilePath)) File.Move(dllFilePath, dllFilePath + ".old", true);
            await using var fileStream = File.Create(dllFilePath);
            await dllStream.CopyToAsync(fileStream);
            fileStream.Flush();
        }
        catch (Exception ex)
        {
            _Log?.LogError(ex);
        }
    }

    private async Task<List<(int Epoch, long Size)>> GetAssemblyInfoAsync(HttpClient http)
    {
        var url = ConvertUrl("https://raw.githubusercontent.com/Dolly1016/Nebula/master/epoch.dat");
        var response = await http.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK) return [];
        var result = await response.Content.ReadAsStringAsync();
        var strings = result.Replace("\r\n", "\n").Split('\n');

        List<(int Epoch, long Size)> list = new();
        foreach (var s in strings)
        {
            var splited = s.Split(',');
            if (splited.Length != 2) continue;
            if (int.TryParse(splited[0], out var epoch) && long.TryParse(splited[1], out var size))
                list.Add((epoch, size));
        }

        return list;
    }

    public static string ConvertUrl(string url)
    {
        if (!url.StartsWith("https://raw.githubusercontent.com") && !url.StartsWith("https://github.com"))
            return url;

        if (CurrentProxy == "")
            CheckAndUseProxy();

        var u = CurrentProxy + "/" + url;
        _Log?.LogInfo("Download:" + u);
        return u;
    }

    private static ManualLogSource? _Log { get; set; }
    private static void CheckAndUseProxy()
    {
        _Log?.LogInfo("开始获取Github下载代理");
        using var ping = new Ping();
        var list = new List<pingInfo>();
        foreach (var proxy in AllGithubProxy)
        {
            _Log?.LogInfo($"正在测试:{proxy}");
            var reply = ping.Send(proxy.Replace("https://", string.Empty));
            if (reply.Status == IPStatus.Success)
                list.Add(new pingInfo(proxy, reply.RoundtripTime));
            _Log?.LogInfo($"{proxy} PingTime:{reply.RoundtripTime}");
        }

        CurrentProxy = list.MinBy(n => n.pingTime)?.url ?? "";
        _Log?.LogInfo($"当前代理为:{CurrentProxy}");
    }

    public override void Load()
    {
        MyPlugin = this;
        _Log = Log;
        SkipCheckingConsistency = Config.Bind("Options", "SkipCheckingConsistency", false,
            "When enabled, All checking routines will be skipped.");
        IgnoringVersionConsistencyOnUpdate = Config.Bind("Options", "IgnoringVersionConsistency", false,
            "When enabled, this allows for combinations of NoS and Among Us versions that are not guaranteed.");
        UseSnapshot = Config.Bind("Options", "UseSnapshot", false,
            "When enabled, Get the latest snapshot or stable version.");
        AutoUpdate = Config.Bind("Options", "AutoUpdate", false,
            "When enabled, the automatic update feature is enabled.");
        AutoUpdateIfVersionMismatch = Config.Bind("Options", "AutoUpdateIfVersionMismatching", false,
            "Automatically updates when a version mismatch of Among Us is detected. This setting is ignored when AutoUpdate is enabled.");

        var autoUpdate = AutoUpdate.Value;
        var autoUpdateIfVersionMismatch = AutoUpdateIfVersionMismatch.Value;
        var dllDirectoryPath = "BepInEx" + Path.DirectorySeparatorChar + "nebula";
        var dllFilePath = dllDirectoryPath + Path.DirectorySeparatorChar + "Nebula.dll";

        if (!SkipCheckingConsistency.Value)
        {
            if (!File.Exists(dllFilePath)) autoUpdate = true;

            if (autoUpdate || autoUpdateIfVersionMismatch)
            {
                var size = GetVanillaSize();
                if (size == -1)
                {
                    _Log.LogWarning("Assembly Is Not Found.\nAttempts to load an existing NoS.");
                    TryLoadNebula(dllFilePath);
                    return;
                }

                _Log.LogInfo("Assembly Size: " + size);

                HttpClient http = new();
                http.DefaultRequestHeaders.Add("User-Agent", "Nebula Updater");

                var assemblyInfoTask = GetAssemblyInfoAsync(http);
                _Log.LogInfo("Start getting information about the Among us assembly...");
                assemblyInfoTask.Wait();
                var assemblyCandidates = assemblyInfoTask.Result.Where(tuple => tuple.Size == size)
                    .Select(tuple => tuple.Epoch).Distinct().ToArray();

                switch (assemblyCandidates.Length)
                {
                    case 0 when !IgnoringVersionConsistencyOnUpdate.Value:
                        _Log.LogWarning("Unknown assembly detected.\nAttempts to load an existing NoS.");
                        TryLoadNebula(dllFilePath);
                        return;
                    case 1:
                        _Log.LogInfo("Detected Epoch: " + assemblyCandidates[0]);
                        break;
                }

                var allVersions = FetchAsync(http);
                allVersions.Wait();

                _Log.LogInfo("Releases Count: " + allVersions.Result.Count);
                _Log.LogInfo("Version Matched Releases Count: " +
                            allVersions.Result.Count(v => assemblyCandidates.Contains(v.Epoch)));

                var candidates = allVersions.Result.Where(v =>
                    (IgnoringVersionConsistencyOnUpdate.Value || assemblyCandidates.Contains(v.Epoch)) &&
                    (v.Category == "v" || (UseSnapshot.Value && v.Category == "s"))).ToArray();

                if (candidates.Length == 0)
                {
                    _Log.LogWarning(
                        "There is no NoS that can be implemented in the current environment.\nAttempts to load an existing NoS.");
                    TryLoadNebula(dllFilePath);
                    return;
                }

                Directory.CreateDirectory(dllDirectoryPath);

                var shouldDownload = true;

                if (File.Exists(dllFilePath))
                {
                    var file = FileVersionInfo.GetVersionInfo(dllFilePath);
                    var currentEpoch = file.FileMajorPart;
                    var currentBuild = file.FileMinorPart;

                    if (candidates[0].Epoch == currentEpoch && candidates[0].Build == currentBuild)
                    {
                        _Log.LogInfo("The latest NoS is already in place.");
                        shouldDownload = false;
                    }

                    //バージョン不一致時のみ更新する場合、バージョン候補内のエポックと現在のエポックが一致していれば何もしない。
                    if (!autoUpdate && candidates.Any(c => c.Epoch == currentEpoch)) shouldDownload = false;
                }

                if (shouldDownload)
                {
                    _Log.LogInfo("Installing " + candidates[0].VisualName.Replace('_', ' ') + "...");
                    UpdateAsync(http, candidates[0].Tag, dllFilePath).Wait();
                }
            }
        }

        TryLoadNebula(dllFilePath);
    }

    private static void TryLoadNebula(string dllFilePath)
    {
        var dllFullFilePath = Path.GetFullPath(dllFilePath);
        if (!File.Exists(dllFullFilePath)) return;
        var NebulaAssembly = Assembly.LoadFile(dllFullFilePath);

        var nebulaPluginType = NebulaAssembly.GetType("Nebula.NebulaPlugin");
        nebulaPluginType?.GetMethod("Load", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
        nebulaPluginType?.GetField("LoaderPlugin", BindingFlags.Static | BindingFlags.Public)
            ?.SetValue(null, MyPlugin);
    }

    private record pingInfo(string url, long pingTime);
}
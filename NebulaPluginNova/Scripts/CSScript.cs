using Cpp2IL.Core.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Virial;
using Virial.Runtime;

namespace Nebula.Scripts;

internal class AddonBehaviour
{
    [JsonSerializableField]
    public bool LoadRoles = false;

    [JsonSerializableField]
    public bool UseHiddenMembers = false;
}

internal record AddonScript(Assembly Assembly, NebulaAddon Addon, MetadataReference Reference, AddonBehaviour Behaviour);


[NebulaPreprocess(PreprocessPhase.CompileAddons)]
internal static class AddonScriptManagerLoader
{
    static IEnumerator Preprocess(NebulaPreprocessor preprocessor)
    {
        Patches.LoadPatch.LoadingText = "Compiling Addon Scripts";
        yield return null;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.Scripting.System.Collections.Immutable.dll")!.ReadBytes());
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.Scripting.System.Reflection.Metadata.dll")!.ReadBytes());
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.Scripting.Microsoft.CodeAnalysis.dll")!.ReadBytes());
        Assembly.Load(StreamHelper.OpenFromResource("Nebula.Resources.Scripting.Microsoft.CodeAnalysis.CSharp.dll")!.ReadBytes());

        yield return AddonScriptManager.CoLoad(assemblies);
    }
}


internal static class AddonScriptManager
{
    static private PortableExecutableReference[] ReferenceAssemblies = [];
    static public IEnumerable<AddonScript> ScriptAssemblies => scriptAssemblies;
    static private List<AddonScript> scriptAssemblies = [];
    static public IEnumerator CoLoad(Assembly[] assemblies)
    {
        //参照可能なアセンブリを抽出する
        ReferenceAssemblies = assemblies.Where(a => { try { return ((a.Location?.Length ?? 0) > 0); } catch { return false; } }).Select(a => MetadataReference.CreateFromFile(a.Location)).Append(MetadataReference.CreateFromImage(StreamHelper.OpenFromResource("Nebula.Resources.API.NebulaAPI.dll")!.ReadBytes())).ToArray();
        
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithUsings("Virial", "Virial.Compat", "System", "System.Linq", "System.Collections.Generic")
            .WithNullableContextOptions(NullableContextOptions.Enable)
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithMetadataImportOptions(MetadataImportOptions.All);

        foreach (var addon in NebulaAddon.AllAddons)
        {
            var prefix = addon.InZipPath + "Scripts/";
            
            AddonBehaviour? addonBehaviour = null;
            var behaviour = addon.Archive.GetEntry(prefix + ".behaviour");
            if (behaviour != null)
            {
                using (var stream = behaviour.Open())
                {
                    addonBehaviour = JsonStructure.Deserialize<AddonBehaviour>(stream);
                }
            }

            List<SyntaxTree> trees = [];
            foreach(var entry in addon.Archive.Entries)
            {
                if (!entry.FullName.StartsWith(prefix)) continue;

                if (entry.FullName.EndsWith(".cs"))
                {
                    //解析木をつくる
                    trees.Add(CSharpSyntaxTree.ParseText(entry.Open().ReadToEnd(), parseOptions, entry.FullName.Substring(prefix.Length), Encoding.UTF8));
                }
            }
            
            //解析木が一つも無ければコンパイルは不要
            if (trees.Count == 0) continue;

            Patches.LoadPatch.LoadingText = "Compiling Addon Scripts\n" + addon.Id;
            yield return null;

            var myCompilationOptions = compilationOptions.WithModuleName("Script." + addon.Id.HeadUpper());

            if (addonBehaviour?.UseHiddenMembers ?? false)
            {
                //全Internal, Privateメンバにアクセスできるようにする
                var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic)!;
                topLevelBinderFlagsProperty.SetValue(compilationOptions, (uint)1 << 22);
            }

            var compilation = CSharpCompilation.Create("Script." + addon.Id.HeadUpper(), trees, ReferenceAssemblies, myCompilationOptions)
                .AddReferences(scriptAssemblies.Where(a => addon.Dependency.Contains(a.Addon)).Select(a => a.Reference));
            
            Assembly? assembly = null;
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);

                if (emitResult.Diagnostics.Length > 0) {
                    var log = "Compile Log:";
                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        var pos = diagnostic.Location.GetLineSpan();
                        var location = "(" + pos.Path + " at line " + (pos.StartLinePosition.Line + 1) + ", character" + (pos.StartLinePosition.Character + 1) + ")";

                        log += $"\n[{diagnostic.Severity}, {location}] {diagnostic.Id}, {diagnostic.GetMessage()}";
                    }
                    NebulaPlugin.Log.Print(NebulaLog.LogLevel.Log, NebulaLog.LogCategory.Scripting, log);
                }

                if (emitResult.Success)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
                }
                else
                {
                    NebulaPlugin.Log.Print(NebulaLog.LogLevel.Error, NebulaLog.LogCategory.Scripting, "Compile Error! Scripts is ignored (Addon: " + addon.Id + ")");
                }
            }

            if (assembly != null)
            {
                scriptAssemblies.Add(new(assembly, addon, compilation.ToMetadataReference(), addonBehaviour ?? new()));
                NebulaAPI.Preprocessor?.PickUpPreprocess(assembly);
            }
        }

        yield break;
    }
}


using System.Runtime.Loader;
using CUE4Parse.FileProvider;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet;
using UAssetAPI.UnrealTypes;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 3 && args[0] == "--asset")
            {
                return DumpAsset(args[1], args[2], Path.GetFileName(args[1])) ? 0 : 1;
            }
            if (args.Length < 5 || args[0] != "--paks")
                throw new ArgumentException("Usage: --asset <uasset> <output-json> OR --paks <paks> <aes-key> <output-directory> <inspector-libraries> [path-match]");
            var libraries = Path.GetFullPath(args[4]);
            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var path = Path.Combine(libraries, name.Name + ".dll");
                return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
            };
            return DumpPaks(args[1], args[2], args[3], libraries, args.Length > 5 ? args[5] : null);
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static bool DumpAsset(string path, string output, string packagePath)
    {
        var asset = new UAsset(path, EngineVersion.VER_UE4_22);
        KismetSerializer.asset = asset;
        var errors = new List<string>();
        var exports = new List<object>();
        for (var i = 0; i < asset.Exports.Count; i++)
        {
            var export = asset.Exports[i];
            var entry = new Dictionary<string, object?>
            {
                ["index"] = i + 1, ["name"] = export.ObjectName.ToString(),
                ["type"] = export.GetType().Name, ["outer"] = export.OuterIndex.Index,
                ["class"] = export.ClassIndex.Index, ["super"] = export.SuperIndex.Index
            };
            try
            {
                if (export is PropertyExport property && property.Property != null)
                    entry["property"] = JToken.Parse(asset.SerializeJsonObject(property.Property, true));
                if (export is ClassExport cls)
                {
                    entry["superStruct"] = cls.SuperStruct.Index;
                    entry["children"] = cls.Children.Select(x => x.Index).ToArray();
                    entry["classDefaultObject"] = cls.ClassDefaultObject.Index;
                    entry["flags"] = cls.ClassFlags.ToString();
                }
                if (export is FunctionExport function)
                {
                    entry["flags"] = function.FunctionFlags.ToString();
                    entry["children"] = function.Children.Select(x => x.Index).ToArray();
                    entry["script"] = function.ScriptBytecode is { Length: > 0 }
                        ? KismetSerializer.SerializeScript(function.ScriptBytecode) : null;
                }
            }
            catch (Exception ex) { errors.Add($"Export {i + 1} {export.ObjectName}: {ex.Message}"); }
            exports.Add(entry);
        }
        var report = new
        {
            schemaVersion = 1, packagePath, engine = "UE4_22", objectVersion = (int)asset.ObjectVersion,
            propertyCount = asset.Exports.OfType<PropertyExport>().Count(),
            functionCount = asset.Exports.OfType<FunctionExport>().Count(),
            imports = asset.Imports.Select((x, i) => new { index = -i - 1, name = x.ObjectName.ToString(),
                className = x.ClassName.ToString(), classPackage = x.ClassPackage.ToString(), outer = x.OuterIndex.Index }),
            exports, errors,
            note = "Legacy property metadata, imports and Kismet only. No opaque asset buffers, textures, saves or native ABI certification."
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, JsonConvert.SerializeObject(report, Formatting.Indented));
        Console.WriteLine($"{packagePath}: {report.propertyCount} properties, {report.functionCount} functions, {errors.Count} export errors");
        return errors.Count == 0;
    }

    private static int DumpPaks(string paks, string key, string output, string libraries, string? fixtureMatch)
    {
        if (OperatingSystem.IsWindows())
        {
            CUE4Parse.Compression.ZlibHelper.Initialize(Path.Combine(libraries, "zlib-ng2.dll"));
            CUE4Parse.Compression.OodleHelper.Initialize(Path.Combine(libraries, "oo2core_9_win64.dll"));
        }
        using var provider = new DefaultFileProvider(new DirectoryInfo(paks), SearchOption.AllDirectories, null, null);
        provider.Versions.Game = EGame.GAME_UE4_22;
        provider.Versions.Ver = EGame.GAME_UE4_22.GetVersion();
        provider.Initialize();
        var aes = new FAesKey(key);
        foreach (var archive in provider.UnloadedVfs.ToArray()) provider.SubmitKey(archive.EncryptionKeyGuid, aes);
        provider.PostMount();
        var matches = fixtureMatch != null ? new[] { fixtureMatch } : new[]
        {
            "Dungeons/Content/UI/Inventory/UMG_Inventory", "Dungeons/Content/UI/Inventory/Salvage/",
            "Dungeons/Content/UI/Inventory/UMG_Item", "Dungeons/Content/UI/Inventory/Inspector2/UMG_InventoryItem",
            "Dungeons/Content/UI/Grid/", "Dungeons/Content/Actors/Characters/Player/BP_PlayerController"
        };
        var candidates = provider.Files.Keys.Where(x => x.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase)
            && matches.Any(m => x.Contains(m, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x).ToArray();
        var errors = new List<string>();
        var completed = new List<string>();
        var temp = Path.Combine(Path.GetTempPath(), "MCDQoL-evidence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        Directory.CreateDirectory(output);
        try
        {
            for (var i = 0; i < candidates.Length; i++)
            {
                var virtualPath = candidates[i];
                var local = Path.Combine(temp, i.ToString());
                Directory.CreateDirectory(local);
                try
                {
                    foreach (var pair in provider.SavePackage(virtualPath))
                        File.WriteAllBytes(Path.Combine(local, Path.GetFileName(pair.Key.Replace('\\', '/'))), pair.Value);
                    var input = Path.Combine(local, Path.GetFileName(virtualPath));
                    var name = i.ToString("D3") + "_" + Path.GetFileNameWithoutExtension(virtualPath) + ".json";
                    if (DumpAsset(input, Path.Combine(output, name), virtualPath)) completed.Add(virtualPath);
                    else errors.Add(virtualPath + ": incomplete export metadata; see asset JSON errors.");
                }
                catch (Exception ex) { errors.Add(virtualPath + ": " + ex); Console.Error.WriteLine(errors.Last()); }
                finally { Directory.Delete(local, true); }
            }
        }
        finally { Directory.Delete(temp, true); }
        if (candidates.Length == 0) errors.Add("No targeted assets visible. Check archive access/key.");
        File.WriteAllText(Path.Combine(output, "EXPORT_REPORT.json"), JsonConvert.SerializeObject(new
        { schemaVersion = 1, engine = "UE4_22", candidateCount = candidates.Length, completed, errors }, Formatting.Indented));
        return errors.Count == 0 ? 0 : 1;
    }
}

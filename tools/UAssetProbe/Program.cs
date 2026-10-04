using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: UAssetProbe <path-to-uasset>");
    return 2;
}

var path = args[0];
var versions = new[]
{
    EngineVersion.VER_UE4_20,
    EngineVersion.VER_UE4_21,
    EngineVersion.VER_UE4_22,
    EngineVersion.VER_UE4_23,
    EngineVersion.VER_UE4_24,
    EngineVersion.VER_UE4_25,
    EngineVersion.VER_UE4_26,
    EngineVersion.VER_UE4_27,
};

foreach (var version in versions)
{
    Console.WriteLine($"=== {version} ===");
    try
    {
        var asset = new UAsset(path, version);
        Console.WriteLine("SUCCESS");
        Console.WriteLine($"ObjectVersion={asset.ObjectVersion} ({(int)asset.ObjectVersion})");
        Console.WriteLine($"Names={asset.GetNameMapIndexList().Count}");
        Console.WriteLine($"Imports={asset.Imports.Count}");
        Console.WriteLine($"Exports={asset.Exports.Count}");

        Console.WriteLine();
        Console.WriteLine("== IMPORTS ==");
        for (var i = 0; i < asset.Imports.Count; i++)
        {
            var import = asset.Imports[i];
            Console.WriteLine($"I{i + 1}: {import.ClassPackage}.{import.ClassName} {import.ObjectName} outer={import.OuterIndex.Index}");
        }

        Console.WriteLine();
        Console.WriteLine("== EXPORTS ==");
        for (var i = 0; i < asset.Exports.Count; i++)
        {
            var export = asset.Exports[i];
            var extra = export is FunctionExport fn
                ? $" bytecode={fn.ScriptBytecode?.Length ?? 0}"
                : string.Empty;
            Console.WriteLine($"E{i + 1}: {export.GetType().Name} {export.ObjectName}{extra}");
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

return 1;

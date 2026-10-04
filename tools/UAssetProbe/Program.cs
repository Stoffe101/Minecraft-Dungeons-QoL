using UAssetAPI;
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

var success = false;

foreach (var version in versions)
{
    Console.WriteLine($"=== {version} ===");
    try
    {
        var asset = new UAsset(path, version);
        Console.WriteLine($"SUCCESS");
        Console.WriteLine($"ObjectVersion={asset.ObjectVersion} ({(int)asset.ObjectVersion})");
        Console.WriteLine($"Names={asset.GetNameMapIndexList().Count}");
        Console.WriteLine($"Imports={asset.Imports.Count}");
        Console.WriteLine($"Exports={asset.Exports.Count}");
        success = true;
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

return success ? 0 : 1;

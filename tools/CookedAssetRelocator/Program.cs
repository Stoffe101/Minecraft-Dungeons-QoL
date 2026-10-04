using UAssetAPI;
using UAssetAPI.UnrealTypes;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: CookedAssetRelocator <input.uasset-or-umap> <output>");
    return 2;
}
var asset = new UAsset(Path.GetFullPath(args[0]), EngineVersion.VER_UE4_22);
var count = 0;
for (var i = 0; i < asset.GetNameMapIndexList().Count; i++)
{
    var oldName = asset.GetNameReference(i).Value;
    var newName = oldName.Replace("LetMeMove", "MinecraftDungeonsQoL", StringComparison.Ordinal)
        .Replace("BP_WASD_Movement", "BP_MCDQoL_Manager", StringComparison.Ordinal);
    if (newName == oldName) continue;
    asset.SetNameReference(i, new FString(newName));
    count++;
}
if (count == 0) throw new InvalidDataException("Expected a LetMeMove template; no package names were relocated.");
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
asset.Write(output);
var reopened = new UAsset(output, EngineVersion.VER_UE4_22);
if (reopened.GetNameMapIndexList().Any(n => n.Value.Contains("LetMeMove", StringComparison.Ordinal)
    || n.Value.Contains("BP_WASD_Movement", StringComparison.Ordinal)))
    throw new InvalidDataException("Old template package names survived relocation.");
Console.WriteLine($"[OK] Relocated {count} names and re-opened {Path.GetFileName(output)}.");
return 0;

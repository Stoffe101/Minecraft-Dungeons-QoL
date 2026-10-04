using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: CookedActorPatcher <input.uasset> <output.uasset> [mode]");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var mode = args.Length >= 3 ? args[2] : "roundtrip";

Directory.CreateDirectory(Path.GetDirectoryName(output)!);

var asset = new UAsset(input, EngineVersion.VER_UE4_22);
KismetSerializer.asset = asset;

if (mode.Equals("roundtrip", StringComparison.OrdinalIgnoreCase))
{
    var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["W"] = "F6",
        ["S"] = "F7",
        ["A"] = "F8",
        ["D"] = "F9"
    };

    var changes = 0;
    foreach (var fn in asset.Exports.OfType<FunctionExport>())
    {
        if (fn.ScriptBytecode is not { Length: > 0 }) continue;

        foreach (var root in fn.ScriptBytecode)
        {
            uint offset = 0;
            root.Visit(asset, ref offset, (expr, _) =>
            {
                if (expr is EX_NameConst nameConst)
                {
                    var oldValue = nameConst.Value?.ToString();
                    if (oldValue != null && replacements.TryGetValue(oldValue, out var newValue))
                    {
                        nameConst.Value = new FName(asset, newValue);
                        changes++;
                    }
                }
            });
        }
    }

    if (changes < 4)
    {
        throw new InvalidOperationException($"Expected to patch at least four key constants but changed {changes}.");
    }

    Console.WriteLine($"Patched {changes} key constants.");
}
else
{
    throw new ArgumentException($"Unknown mode: {mode}");
}

asset.Write(output);

var reopened = new UAsset(output, EngineVersion.VER_UE4_22);
KismetSerializer.asset = reopened;

var functionCount = reopened.Exports.OfType<FunctionExport>().Count();
if (functionCount == 0)
{
    throw new InvalidDataException("Round-tripped asset has no FunctionExport entries.");
}

var names = new HashSet<string>(StringComparer.Ordinal);
foreach (var fn in reopened.Exports.OfType<FunctionExport>())
{
    if (fn.ScriptBytecode is not { Length: > 0 }) continue;
    foreach (var root in fn.ScriptBytecode)
    {
        uint offset = 0;
        root.Visit(reopened, ref offset, (expr, _) =>
        {
            if (expr is EX_NameConst nc && nc.Value != null)
            {
                names.Add(nc.Value.ToString());
            }
        });
    }
}

if (mode.Equals("roundtrip", StringComparison.OrdinalIgnoreCase))
{
    foreach (var expected in new[] { "F6", "F7", "F8", "F9" })
    {
        if (!names.Contains(expected))
        {
            throw new InvalidDataException($"Re-opened asset is missing patched key constant {expected}.");
        }
    }
}

Console.WriteLine($"[OK] Wrote and re-opened {output}");
Console.WriteLine($"     Imports={reopened.Imports.Count}, Exports={reopened.Exports.Count}, Functions={functionCount}");
return 0;

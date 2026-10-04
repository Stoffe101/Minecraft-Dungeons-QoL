using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

if (args.Length != 2) { Console.Error.WriteLine("Usage: CookedLoadProbe <manager.uasset> <output.uasset>"); return 2; }
var asset = new UAsset(args[0], EngineVersion.VER_UE4_22);
CookedDependencyGraph.Validate(asset);
var originalCount = asset.Exports.Count;
var children = asset.Exports.OfType<StructExport>().Select(x => x.Children.Select(c => c.Index).ToArray()).ToArray();
foreach (var fn in asset.Exports.OfType<FunctionExport>())
{
    fn.ScriptBytecode = new KismetExpression[] { new EX_Return { ReturnExpression = new EX_Nothing() }, new EX_EndOfScript() };
    fn.ScriptBytecodeRaw = null;
}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
asset.Write(args[1]);
var reopened = new UAsset(args[1], EngineVersion.VER_UE4_22);
CookedDependencyGraph.Validate(reopened);
if (reopened.Exports.Count != originalCount) throw new InvalidDataException("Probe changed export count");
var after = reopened.Exports.OfType<StructExport>().Select(x => x.Children.Select(c => c.Index).ToArray()).ToArray();
if (after.Length != children.Length || after.Where((x, i) => !x.SequenceEqual(children[i])).Any())
    throw new InvalidDataException("Probe changed reflected children");
foreach (var fn in reopened.Exports.OfType<FunctionExport>())
    if (fn.ScriptBytecode is not { Length: 2 } code || code[0] is not EX_Return { ReturnExpression: EX_Nothing }
        || code[1] is not EX_EndOfScript)
        throw new InvalidDataException("Probe contains executable behavior");
Console.WriteLine("[OK] Load probe retains manager schema; every function immediately returns.");
return 0;

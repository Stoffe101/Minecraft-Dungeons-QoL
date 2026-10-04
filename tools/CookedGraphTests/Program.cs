using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

if (args.Length != 1) { Console.Error.WriteLine("Usage: CookedGraphTests <diagnostic-actor.uasset>"); return 2; }
var asset = new UAsset(args[0], EngineVersion.VER_UE4_22);
var fn = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString().StartsWith("ExecuteUbergraph_"));
var code = fn.ScriptBytecode;
DiagnosticGraphValidator.Validate(asset, code);
void Rejected(Action mutate, Action restore, string reason)
{
    mutate();
    try {
        DiagnosticGraphValidator.Validate(asset, code);
        throw new Exception($"Validator accepted {reason}");
    } catch (InvalidDataException) { Console.WriteLine($"[PASS] rejects {reason}"); }
    finally { restore(); }
}
var jump = code.OfType<EX_Jump>().First();
var oldJump = jump.CodeOffset;
Rejected(() => jump.CodeOffset = uint.MaxValue, () => jump.CodeOffset = oldJump, "out-of-range jump");
Rejected(() => jump.CodeOffset = 1, () => jump.CodeOffset = oldJump, "jump into statement payload");
var conditional = code.OfType<EX_JumpIfNot>().First();
var oldConditional = conditional.CodeOffset;
Rejected(() => conditional.CodeOffset = 1, () => conditional.CodeOffset = oldConditional, "conditional jump into payload");
EX_Context? context = null;
foreach (var root in code) {
    uint offset = 0;
    root.Visit(asset, ref offset, (expr, _) => { if (expr is EX_Context ctx) context ??= ctx; });
}
var oldSkip = context!.Offset;
Rejected(() => context.Offset++, () => context.Offset = oldSkip, "invalid null-context skip");
// Mutate an existing call in-place to preserve instruction sizes and jump layout.
EX_VirtualFunction? call = null;
foreach (var root in code) {
    uint offset = 0;
    root.Visit(asset, ref offset, (expr, _) => { if (expr is EX_VirtualFunction vf) call ??= vf; });
}
var oldName = call!.VirtualFunctionName;
foreach (var name in new[] { "SalvageItemInSlot", "RemoveItem", "SalvageItemUndo", "Swap" })
    Rejected(() => call.VirtualFunctionName = new FName(asset, name), () => call.VirtualFunctionName = oldName, name);
DiagnosticGraphValidator.Validate(asset, code);
Console.WriteLine("[PASS] original diagnostic graph remains valid after 8 rejection tests");
return 0;

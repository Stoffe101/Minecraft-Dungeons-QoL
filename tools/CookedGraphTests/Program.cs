using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

if (args.Length != 1) { Console.Error.WriteLine("Usage: CookedGraphTests <diagnostic-actor.uasset>"); return 2; }
FunctionOwnerTests.Run(args[0]);
var asset = new UAsset(args[0], EngineVersion.VER_UE4_22);
var fn = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString().StartsWith("ExecuteUbergraph_"));
var code = fn.ScriptBytecode;
if (asset.Exports.Any(x => x.ObjectName.ToString() == "ProbeText")) return InventoryProbeTests.Run(asset, code);
DiagnosticGraphValidator.Validate(asset, code);
void Rejected(Action mutate, Action restore, string reason, string? expected = null)
{
    mutate();
    try {
        DiagnosticGraphValidator.Validate(asset, code);
        throw new Exception($"Validator accepted {reason}");
    } catch (InvalidDataException ex) {
        if (expected != null && !ex.Message.Contains(expected)) throw new Exception($"Wrong rejection for {reason}: {ex.Message}");
        Console.WriteLine($"[PASS] rejects {reason}");
    }
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
foreach (var name in new[] { "SalvageItemInSlot", "RemoveItem", "SalvageItemUndo", "Swap", "GetTotalInvestedEnchantmentPoints" })
    Rejected(() => call.VirtualFunctionName = new FName(asset, name), () => call.VirtualFunctionName = oldName, name);
bool HasMember(KismetExpression root, string name)
{
    var found = false;
    uint offset = 0;
    root.Visit(asset, ref offset, (expr, _) => {
        var index = expr switch { EX_InstanceVariable iv => iv.Variable.Old, EX_LocalVariable lv => lv.Variable.Old, _ => new FPackageIndex(0) };
        var member = index.IsImport() ? index.ToImport(asset).ObjectName.ToString()
            : index.IsExport() ? index.ToExport(asset).ObjectName.ToString() : "";
        if (member == name) found = true;
    });
    return found;
}
var gate = code.OfType<EX_JumpIfNot>().Single(x => DiagnosticGraphValidator.IsInventoryOpenCondition(asset,x.BooleanExpression));
var openMember = asset.Imports.Single(x => x.ObjectName.ToString() == "IsVisible");
var oldOpenName = openMember.ObjectName;
Rejected(() => openMember.ObjectName = new FName(asset, "UnverifiedOpenFlag"), () => openMember.ObjectName = oldOpenName,
    "missing inventory-open guard", "Expected one inventory-open guard");
var gateRoot = Array.IndexOf(code, gate);
uint gateFallthrough = 0;
for (var i = 0; i <= gateRoot; i++) {
    using var stream = new MemoryStream();
    using var writer = new AssetBinaryWriter(stream, asset);
    gateFallthrough += (uint)ExpressionSerializer.WriteExpression(code[i], writer);
}
var oldGateTarget = gate.CodeOffset;
Rejected(() => gate.CodeOffset = gateFallthrough, () => gate.CodeOffset = oldGateTarget, "closed inventory falls through to hotkeys", "reachable without open inventory");
foreach (var guard in code.OfType<EX_JumpIfNot>().Where(x => HasMember(x.BooleanExpression, "MCDQoL_EquipNativeSlot") && HasMember(x.BooleanExpression, "MCDQoL_CurrentItem")).ToArray()) {
    var compare = (EX_CallMath)guard.BooleanExpression;
    var oldOperand = compare.Parameters[0];
    var slot = FPackageIndex.FromExport(asset.Exports.FindIndex(x => x.ObjectName.ToString() == "MCDQoL_CurrentSlot"));
    Rejected(() => compare.Parameters[0] = new EX_LocalVariable { Variable = new KismetPropertyPointer { Old = slot } },
        () => compare.Parameters[0] = oldOperand, "missing equipment exclusion", "explicit equipment guards");
}
var owner = asset.Exports.OfType<ClassExport>().Single();
var child = owner.Children.Last();
var edge = owner.SerializationBeforeSerializationDependencies.FindIndex(x => x.Index == child.Index);
if (edge < 0) throw new Exception("Expected new property preload edge");
Rejected(() => owner.SerializationBeforeSerializationDependencies.RemoveAt(edge),
    () => owner.SerializationBeforeSerializationDependencies.Insert(edge, child), "missing generated child preload", "Missing child preload");
var field = child.ToExport(asset);
var outer = field.CreateBeforeCreateDependencies.ToArray();
Rejected(() => field.CreateBeforeCreateDependencies.Clear(),
    () => field.CreateBeforeCreateDependencies.AddRange(outer), "missing field outer creation", "Missing field outer");
var array = asset.Exports.OfType<PropertyExport>().First(x => x.Property is UAssetAPI.FieldTypes.UArrayProperty);
var deps = array.SerializationBeforeSerializationDependencies.ToArray();
Rejected(() => array.SerializationBeforeSerializationDependencies.Clear(),
    () => array.SerializationBeforeSerializationDependencies.AddRange(deps), "missing array inner preload", "Missing array inner");
var addedField = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "MCDQoL_CurrentSlot");
var template = addedField.TemplateIndex;
Rejected(() => addedField.TemplateIndex = new FPackageIndex(0), () => addedField.TemplateIndex = template,
    "missing field archetype", "Missing property archetype");
var wrongTemplate = asset.Exports.OfType<PropertyExport>().First(x => x.Property is UAssetAPI.FieldTypes.UIntProperty).TemplateIndex;
Rejected(() => addedField.TemplateIndex = wrongTemplate, () => addedField.TemplateIndex = template,
    "wrong property archetype type", "Property archetype type mismatch");
var creation = addedField.SerializationBeforeCreateDependencies.ToArray();
Rejected(() => addedField.SerializationBeforeCreateDependencies.Clear(),
    () => addedField.SerializationBeforeCreateDependencies.AddRange(creation),
    "missing property class/archetype preload", "Missing property class/archetype preload");
DiagnosticGraphValidator.Validate(asset, code);
Console.WriteLine("[PASS] original diagnostic graph remains valid after 19 rejection tests");
return 0;

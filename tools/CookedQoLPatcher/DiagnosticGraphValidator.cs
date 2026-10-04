using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Kismet;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;

public static class DiagnosticGraphValidator
{
    public static StructPropertyData InventoryTickDefaults(UAsset asset)
    {
        var owner = asset.Exports.OfType<ClassExport>().Single();
        var cdo = (NormalExport)owner.ClassDefaultObject.ToExport(asset);
        return cdo.Data.OfType<StructPropertyData>().Single(x => x.Name.ToString() == "PrimaryActorTick");
    }
    public static void ConfigureInventoryTick(UAsset asset)
    {
        var owner = asset.Exports.OfType<ClassExport>().Single();
        var cdo = (NormalExport)owner.ClassDefaultObject.ToExport(asset);
        var replicated = cdo.Data.Where(x => x.Name.ToString() == "bReplicates").ToArray();
        if (replicated.Length == 0) cdo.Data.Add(new BoolPropertyData(new FName(asset, "bReplicates")) { Value = false });
        else if (replicated.Length == 1 && replicated[0] is BoolPropertyData flag) flag.Value = false;
        else throw new InvalidDataException("Invalid inventory actor replication flag.");
        var tick = InventoryTickDefaults(asset);
        foreach (var name in new[] { "bCanEverTick", "bStartWithTickEnabled", "bTickEvenWhenPaused" })
        {
            var fields = tick.Value.Where(x => x.Name.ToString() == name).ToArray();
            if (fields.Length > 1 || fields.Length == 1 && fields[0] is not BoolPropertyData)
                throw new InvalidDataException("Invalid inventory actor tick flag: " + name);
            if (fields.Length == 0) tick.Value.Add(new BoolPropertyData(new FName(asset, name)) { Value = true });
            else ((BoolPropertyData)fields[0]).Value = true;
        }
        var intervals = tick.Value.Where(x => x.Name.ToString() == "TickInterval").ToArray();
        if (intervals.Length > 1 || intervals.Length == 1 && intervals[0] is not FloatPropertyData)
            throw new InvalidDataException("Invalid inventory actor tick interval.");
        if (intervals.Length == 0) tick.Value.Add(new FloatPropertyData(new FName(asset, "TickInterval")) { Value = 0f });
        else ((FloatPropertyData)intervals[0]).Value = 0f;
    }
    public static void ValidateInventoryTick(UAsset asset)
    {
        var owner = asset.Exports.OfType<ClassExport>().Single();
        var cdo = (NormalExport)owner.ClassDefaultObject.ToExport(asset);
        var replicated = cdo.Data.Where(x => x.Name.ToString() == "bReplicates").ToArray();
        if (replicated.Length != 1 || replicated[0] is not BoolPropertyData { Value: false })
            throw new InvalidDataException("Inventory actor must not replicate.");
        var tick = InventoryTickDefaults(asset);
        if (tick.StructType.ToString() != "ActorTickFunction")
            throw new InvalidDataException("Wrong inventory actor tick struct.");
        foreach (var name in new[] { "bCanEverTick", "bStartWithTickEnabled", "bTickEvenWhenPaused" })
        {
            var fields = tick.Value.Where(x => x.Name.ToString() == name).ToArray();
            if (fields.Length != 1 || fields[0] is not BoolPropertyData { Value: true })
                throw new InvalidDataException("Inventory actor tick flag must be enabled: " + name);
        }
        var intervals = tick.Value.Where(x => x.Name.ToString() == "TickInterval").ToArray();
        if (intervals.Length != 1 || intervals[0] is not FloatPropertyData { Value: 0f })
            throw new InvalidDataException("Inventory actor tick interval must be zero.");
    }
    // Keep the reviewed cooked actor's event entry contract: ReceiveTick calls offset 10.
    public static EX_ComputedJump TickDispatch(UAsset asset)
    {
        var uber = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString().StartsWith("ExecuteUbergraph_"));
        var entry = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "EntryPoint"
            && x.OuterIndex.ToExport(asset) == uber);
        return new EX_ComputedJump { CodeOffsetExpression = new EX_LocalVariable {
            Variable = new KismetPropertyPointer(FPackageIndex.FromExport(asset.Exports.IndexOf(entry))) } };
    }
    public static void ValidateTickEntry(UAsset asset, IReadOnlyList<KismetExpression> code)
    {
        var uber = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString().StartsWith("ExecuteUbergraph_"));
        if (code.Count < 2 || code[0] is not EX_ComputedJump dispatch
            || dispatch.CodeOffsetExpression is not EX_LocalVariable entry
            || !entry.Variable.Old.IsExport()
            || entry.Variable.Old.ToExport(asset).ObjectName.ToString() != "EntryPoint"
            || entry.Variable.Old.ToExport(asset).OuterIndex.ToExport(asset) != uber)
            throw new InvalidDataException("Missing tick entry dispatcher.");
        using var stream = new MemoryStream();
        using var writer = new AssetBinaryWriter(stream, asset);
        if (ExpressionSerializer.WriteExpression(code[0], writer) != 10)
            throw new InvalidDataException("Tick entry body must start at offset 10.");
        var tick = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "ReceiveTick");
        var calls = new List<EX_LocalFinalFunction>();
        foreach (var root in tick.ScriptBytecode) {
            uint offset = 0;
            root.Visit(asset, ref offset, (e, _) => { if (e is EX_LocalFinalFunction f) calls.Add(f); });
        }
        if (calls.Count != 1 || calls[0].StackNode.ToExport(asset) != uber
            || calls[0].Parameters.Length != 1 || calls[0].Parameters[0] is not EX_IntConst n || n.Value != 10)
            throw new InvalidDataException("ReceiveTick must call the graph body at offset 10.");
    }
    public static bool IsInventoryOpenCondition(UAsset asset, KismetExpression expression)
    {
        if (expression is not EX_Context { ObjectExpression: EX_LocalVariable hud,
            ContextExpression: EX_FinalFunction call } || !hud.Variable.Old.IsExport()
            || hud.Variable.Old.ToExport(asset).ObjectName.ToString() != "MCDQoL_InventoryHUD"
            || !call.StackNode.IsImport() || call.Parameters.Length != 0) return false;
        var function = call.StackNode.ToImport(asset);
        if (function.ObjectName.ToString() != "IsVisible" || !function.OuterIndex.IsImport()) return false;
        var owner = function.OuterIndex.ToImport(asset);
        return owner.ObjectName.ToString() == "Widget" && owner.OuterIndex.IsImport()
            && owner.OuterIndex.ToImport(asset).ObjectName.ToString() == "/Script/UMG";
    }
    public static bool IsLocalPlayerCondition(UAsset asset, KismetExpression expression)
    {
        if (expression is not EX_Context { ObjectExpression: EX_LocalVariable pc,
            ContextExpression: EX_FinalFunction call } || !pc.Variable.Old.IsExport()
            || pc.Variable.Old.ToExport(asset).ObjectName.ToString() != "CallFunc_GetPlayerController_ReturnValue"
            || !call.StackNode.IsImport() || call.Parameters.Length != 0) return false;
        var function = call.StackNode.ToImport(asset);
        if (function.ObjectName.ToString() != "IsLocalPlayerController" || !function.OuterIndex.IsImport()) return false;
        var owner = function.OuterIndex.ToImport(asset);
        return owner.ObjectName.ToString() == "Controller" && owner.OuterIndex.IsImport()
            && owner.OuterIndex.ToImport(asset).ObjectName.ToString() == "/Script/Engine";
    }
    // UE4 native const-reference parameters must read a typed value, not a nested call.
    // StepCompiledInRef can otherwise reuse a property address left by that inner call.
    public static void ValidateReferenceArguments(UAsset asset, IReadOnlyList<KismetExpression> code)
    {
        string CallName(KismetExpression e)=>e switch {
            EX_FinalFunction f when f.StackNode.IsImport()=>f.StackNode.ToImport(asset).ObjectName.ToString(),
            EX_VirtualFunction f=>f.VirtualFunctionName.ToString(), _=>""
        };
        bool Typed(KismetExpression value, bool text) {
            if(!text && value is EX_StringConst)return true;
            if(text && value is EX_TextConst)return true;
            var index=value switch {EX_LocalVariable l=>l.Variable.Old,EX_InstanceVariable i=>i.Variable.Old,_=>null};
            return index != null && index.IsExport() && index.ToExport(asset) is PropertyExport p
                && (text ? p.Property is UAssetAPI.FieldTypes.UTextProperty : p.Property is UAssetAPI.FieldTypes.UStrProperty);
        }
        foreach(var root in code) {
            uint offset=0;
            root.Visit(asset,ref offset,(e,_)=> {
                var name=CallName(e);
                var parameters=e switch {EX_FinalFunction f=>f.Parameters,EX_VirtualFunction f=>f.Parameters,_=>null};
                int[] indices=name switch {
                    "BuildString_Int"=>new[]{0,1,3}, "Concat_StrStr" or "EqualEqual_StrStr"=>new[]{0,1},
                    "Conv_StringToText" or "Conv_StringToName" or "Conv_TextToString" or "SetText"=>new[]{0},
                    "PrintString"=>new[]{1}, _=>Array.Empty<int>()
                };
                foreach(var index in indices)
                    if(parameters==null || index>=parameters.Length || !Typed(parameters[index],name is "Conv_TextToString" or "SetText"))
                        throw new InvalidDataException("Reference argument must be a typed value: "+name+"["+index+"]");
            });
            if(root is EX_Let {Expression:EX_Context context} assignment) {
                var name=CallName(context.ContextExpression);
                if(name is "GetDisplayNameText" or "Conv_StringToText" or "Conv_TextToString" or "BuildString_Int" or "Concat_StrStr") {
                    if(!Typed(assignment.Variable,name is "GetDisplayNameText" or "Conv_StringToText")
                        || context.RValuePointer.Old.Index!=assignment.Value.Old.Index)
                        throw new InvalidDataException("Native scalar return needs a matching typed local: "+name);
                }
            }
        }
    }
    public static int Validate(UAsset asset, IReadOnlyList<KismetExpression> code, bool requireEquipmentGuards = true)
    {
        KismetSerializer.asset = asset;
        ValidateReferenceArguments(asset, code);
        CookedDependencyGraph.Validate(asset);
        ValidateTickEntry(asset, code);
        ValidateInventoryTick(asset);
        int Size(KismetExpression expr)
        {
            using var stream = new MemoryStream();
            using var writer = new AssetBinaryWriter(stream, asset);
            return ExpressionSerializer.WriteExpression(expr, writer);
        }
        var boundaries = new HashSet<uint>();
        uint position = 0;
        foreach (var root in code)
        {
            boundaries.Add(position);
            position = checked(position + (uint)Size(root));
        }
        var forbidden = new HashSet<string>(StringComparer.Ordinal) {
            "SalvageItemInSlot", "RemoveItem", "SalvageItemUndo", "Swap", "GetTotalInvestedEnchantmentPoints"
        };
        foreach (var root in code)
        {
            if (root is EX_Jump j && !boundaries.Contains(j.CodeOffset))
                throw new InvalidDataException("Jump does not target a statement boundary.");
            if (root is EX_JumpIfNot jn && !boundaries.Contains(jn.CodeOffset))
                throw new InvalidDataException("Conditional jump does not target a statement boundary.");
            uint offset = 0;
            root.Visit(asset, ref offset, (expr, _) => {
                if (expr is EX_Context ctx && ctx.Offset != (uint)Size(ctx.ContextExpression))
                    throw new InvalidDataException("Invalid context skip offset.");
                string? call = expr switch {
                    EX_VirtualFunction vf => vf.VirtualFunctionName.ToString(),
                    EX_FinalFunction ff when ff.StackNode.IsImport() => ff.StackNode.ToImport(asset).ObjectName.ToString(),
                    _ => null
                };
                if (call != null && forbidden.Contains(call))
                    throw new InvalidDataException($"Diagnostic build contains forbidden call: {call}");
            });
        }

        // Prove every hotkey/native inventory read is behind the observed inventory-open guard.
        string Member(FPackageIndex index) => index.IsImport() ? index.ToImport(asset).ObjectName.ToString()
            : index.IsExport() ? index.ToExport(asset).ObjectName.ToString() : "";
        bool HasMember(KismetExpression root, string name)
        {
            var found = false;
            uint offset = 0;
            root.Visit(asset, ref offset, (expr, _) => {
                if (expr is EX_InstanceVariable iv && Member(iv.Variable.Old) == name) found = true;
                if (expr is EX_LocalVariable lv && Member(lv.Variable.Old) == name) found = true;
            });
            return found;
        }
        var offsets = new Dictionary<uint, int>();
        position = 0;
        for (var i = 0; i < code.Count; i++) { offsets[position] = i; position += (uint)Size(code[i]); }
        IEnumerable<int> Next(int i)
        {
            if (code[i] is EX_Return or EX_EndOfScript) yield break;
            if (code[i] is EX_Jump jump) { yield return offsets[jump.CodeOffset]; yield break; }
            if (code[i] is EX_JumpIfNot branch) yield return offsets[branch.CodeOffset];
            if (i + 1 < code.Count) yield return i + 1;
        }
        HashSet<int> Reach(int start, int blocked = -1)
        {
            var seen = new HashSet<int>();
            var todo = new Stack<int>(); todo.Push(start);
            while (todo.TryPop(out var i))
                if (i != blocked && seen.Add(i)) foreach (var n in Next(i)) todo.Push(n);
            return seen;
        }
        var guards = Enumerable.Range(0, code.Count).Where(i => code[i] is EX_JumpIfNot g
            && IsInventoryOpenCondition(asset, g.BooleanExpression)).ToArray();
        if (guards.Length != 1) throw new InvalidDataException("Expected one inventory-open guard.");
        var guard = guards[0];
        var localGuards = Enumerable.Range(0, code.Count).Where(i => code[i] is EX_JumpIfNot g
            && IsLocalPlayerCondition(asset, g.BooleanExpression)).ToArray();
        if (localGuards.Length != 1) throw new InvalidDataException("Expected one local-player guard.");
        var localGuard = localGuards[0];
        var beforeLocal = Reach(0, localGuard);
        var remotePath = Reach(offsets[((EX_JumpIfNot)code[localGuard]).CodeOffset]);
        var beforeGate = Reach(0, guard);
        var closedPath = Reach(offsets[((EX_JumpIfNot)code[guard]).CodeOffset]);
        for (var i = 0; i < code.Count; i++)
        {
            var sensitive = false;
            uint offset = 0;
            code[i].Visit(asset, ref offset, (expr, _) => {
                var name = expr is EX_FinalFunction ff ? Member(ff.StackNode) : "";
                if (name is "WasInputKeyJustPressed" or "GetInventorySlots" or "GetSalvageInfo" or "SpawnObject" or "AddChildToCanvas") sensitive = true;
            });
            if (sensitive && (beforeLocal.Contains(i) || remotePath.Contains(i)))
                throw new InvalidDataException("Inventory input/read reachable without local player.");
            if (sensitive && (beforeGate.Contains(i) || closedPath.Contains(i)))
                throw new InvalidDataException("Inventory input/read reachable without open inventory.");
        }
        var equipmentGuards = code.OfType<EX_JumpIfNot>().Count(g =>
            HasMember(g.BooleanExpression, "MCDQoL_CurrentItem") && HasMember(g.BooleanExpression, "MCDQoL_EquipNativeSlot"));
        if (requireEquipmentGuards && equipmentGuards != 2) throw new InvalidDataException("Selection and preview need explicit equipment guards.");
        return checked((int)position);
    }
}

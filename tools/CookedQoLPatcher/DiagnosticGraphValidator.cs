using UAssetAPI;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Kismet;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;

public static class DiagnosticGraphValidator
{
    public static int Validate(UAsset asset, IReadOnlyList<KismetExpression> code)
    {
        KismetSerializer.asset = asset;
        CookedDependencyGraph.Validate(asset);
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
            && HasMember(g.BooleanExpression, "IsInventoryOpen")).ToArray();
        if (guards.Length != 1) throw new InvalidDataException("Expected one inventory-open guard.");
        var guard = guards[0];
        var beforeGate = Reach(0, guard);
        var closedPath = Reach(offsets[((EX_JumpIfNot)code[guard]).CodeOffset]);
        for (var i = 0; i < code.Count; i++)
        {
            var sensitive = false;
            uint offset = 0;
            code[i].Visit(asset, ref offset, (expr, _) => {
                var name = expr is EX_FinalFunction ff ? Member(ff.StackNode) : "";
                if (name is "WasInputKeyJustPressed" or "GetInventorySlots" or "GetSalvageInfo") sensitive = true;
            });
            if (sensitive && (beforeGate.Contains(i) || closedPath.Contains(i)))
                throw new InvalidDataException("Inventory input/read reachable without open inventory.");
        }
        var equipmentGuards = code.OfType<EX_JumpIfNot>().Count(g =>
            HasMember(g.BooleanExpression, "MCDQoL_CurrentItem") && HasMember(g.BooleanExpression, "MCDQoL_EquipNativeSlot"));
        if (equipmentGuards != 2) throw new InvalidDataException("Selection and preview need explicit equipment guards.");
        return checked((int)position);
    }
}

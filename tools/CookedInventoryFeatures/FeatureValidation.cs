using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

static class FeatureValidation
{
    public static void VerifyPreserved(UAsset original, UAsset patched)
    {
        var hud = original.Exports.OfType<ClassExport>().Single().ObjectName.ToString() == "UMG_InventoryHUD_C";
        var prefixes = hud ? new Dictionary<string, int> { ["Tick"] = 1, ["OpenCloseInventory"] = 1, ["SlotClicked"] = patched.Exports.OfType<FunctionExport>().Single(f => f.ObjectName.ToString() == "SlotClicked").ScriptBytecode.Length == 5 ? 1 : 0 }
            : new Dictionary<string, int> { ["CanSalavage"] = 3, ["SalvageSlot"] = 2 };
        byte[] Code(UAsset a, IEnumerable<KismetExpression> expressions) {
            using var stream = new MemoryStream(); using var writer = new AssetBinaryWriter(stream, a);
            foreach (var expression in expressions) ExpressionSerializer.WriteExpression(expression, writer);
            return stream.ToArray();
        }
        for (var i = 0; i < original.Exports.Count; i++)
        {
            var before = original.Exports[i]; var after = patched.Exports[i];
            if (before.ObjectName.ToString() != after.ObjectName.ToString() || before.OuterIndex.Index != after.OuterIndex.Index
                || before.ClassIndex.Index != after.ClassIndex.Index || before.TemplateIndex.Index != after.TemplateIndex.Index
                || !before.Extras.SequenceEqual(after.Extras)) throw new InvalidDataException("Original export changed: " + before.ObjectName);
            if (before is PropertyExport p && after is PropertyExport q && original.SerializeJsonObject(p.Property, true) != patched.SerializeJsonObject(q.Property, true))
                throw new InvalidDataException("Original property changed: " + before.ObjectName);
            if (before is NormalExport normal && after is NormalExport other && original.SerializeJsonObject(normal.Data, true) != patched.SerializeJsonObject(other.Data, true))
                throw new InvalidDataException("Original widget data changed: " + before.ObjectName);
            if (before is not FunctionExport fn || after is not FunctionExport actual) continue;
            if (fn.FunctionFlags != actual.FunctionFlags || !fn.Children.Select(x => x.Index).SequenceEqual(actual.Children.Select(x => x.Index)))
                throw new InvalidDataException("Original function signature changed: " + fn.ObjectName);
            var prefix = prefixes.GetValueOrDefault(fn.ObjectName.ToString());
            var shift = (uint)actual.ScriptBytecode.Take(prefix).Sum(x => x.GetSize(patched));
            foreach (var root in fn.ScriptBytecode) {
                uint position = 0; root.Visit(original, ref position, (e, _) => {
                    switch (e) {
                        case EX_Jump j: j.CodeOffset += shift; break;
                        case EX_JumpIfNot j: j.CodeOffset += shift; break;
                        case EX_PushExecutionFlow p: p.PushingAddress += shift; break;
                        case EX_SwitchValue s:
                            s.EndGotoOffset += shift;
                            for (var j = 0; j < s.Cases.Length; j++) { var c = s.Cases[j]; c.NextOffset += shift; s.Cases[j] = c; }
                            break;
                    }
                });
            }
            if (!Code(original, fn.ScriptBytecode).SequenceEqual(Code(patched, actual.ScriptBytecode.Skip(prefix))))
                throw new InvalidDataException("Original function body changed beyond its guard prefix/relocation: " + fn.ObjectName);
        }
        Console.WriteLine($"[PASS] {original.Exports.Count} original exports preserved; function changes limited to {string.Join(", ", prefixes.Keys)} guard prefixes and relocated jumps.");
    }

    public static void RepairAdded(UAsset asset, int originalCount)
    {
        void Add(List<FPackageIndex> list, FPackageIndex p) { if (!p.IsNull() && !list.Any(x => x.Index == p.Index)) list.Add(p); }
        foreach (var e in asset.Exports.Skip(originalCount))
        {
            Add(e.CreateBeforeCreateDependencies, e.OuterIndex);
            if (e is PropertyExport p)
            {
                p.TemplateIndex = CookedDependencyGraph.PropertyArchetype(asset, p.ClassIndex);
                Add(e.SerializationBeforeCreateDependencies, p.ClassIndex); Add(e.SerializationBeforeCreateDependencies, p.TemplateIndex);
                if (p.Property is UObjectProperty o) Add(e.CreateBeforeSerializationDependencies, o.PropertyClass);
                if (p.Property is UArrayProperty a) Add(e.SerializationBeforeSerializationDependencies, a.Inner);
                if (p.Property is UStructProperty s) Add(e.SerializationBeforeSerializationDependencies, s.Struct);
                if (p.Property is UDelegateProperty d) Add(e.SerializationBeforeSerializationDependencies, d.SignatureFunction);
            }
        }
        // Keep the game's preload graph intact. Only append dependencies for new children
        // and new bytecode references; do not force every original UI function to load every import.
        foreach (var e in asset.Exports.OfType<StructExport>())
            foreach (var child in e.Children.Where(x => x.Index > originalCount))
                Add(e.SerializationBeforeSerializationDependencies, child);
        foreach (var fn in asset.Exports.OfType<FunctionExport>().Where(x => x.ObjectName.ToString().StartsWith("MCDQoL_") || x.ObjectName.ToString() is "Tick" or "OpenCloseInventory" or "CanSalavage" or "SalvageSlot" or "SlotClicked"))
            foreach (var root in fn.ScriptBytecode)
            {
                uint offset = 0;
                root.Visit(asset, ref offset, (e, _) => {
                    FPackageIndex? reference = e switch {
                        EX_FinalFunction f => f.StackNode,
                        EX_ObjectConst o => o.Value,
                        EX_StructConst s => s.Struct,
                        EX_LocalVariable l => l.Variable.Old,
                        EX_LocalOutVariable l => l.Variable.Old,
                        EX_InstanceVariable i => i.Variable.Old,
                        _ => null
                    };
                    if (reference != null && (reference.IsImport() || reference.Index > originalCount)) Add(fn.CreateBeforeSerializationDependencies, reference);
                });
            }
        asset.DependsMap = asset.Exports.Select(e => e.SerializationBeforeSerializationDependencies.Concat(e.CreateBeforeSerializationDependencies)
            .Concat(e.SerializationBeforeCreateDependencies).Concat(e.CreateBeforeCreateDependencies).Select(x => x.Index).Distinct().ToArray()).ToList();
    }

    public static void Validate(UAsset asset, bool nativeBatch)
    {
        FunctionImportContracts.Validate(asset);
        foreach (var fn in asset.Exports.OfType<FunctionExport>().Where(x => x.ObjectName.ToString().StartsWith("MCDQoL_")))
            FunctionLayoutContracts.Validate(asset, fn);
        foreach (var function in asset.Exports.OfType<FunctionExport>().Where(x => x.ObjectName.ToString().StartsWith("MCDQoL_")))
            foreach (var root in function.ScriptBytecode) {
                uint position = 0; root.Visit(asset, ref position, (expression, _) => {
                    if (expression is not EX_BindDelegate binding) return;
                    var handler = asset.Exports.OfType<FunctionExport>().SingleOrDefault(x => x.ObjectName == binding.FunctionName);
                    if (handler == null || !asset.Exports.OfType<ClassExport>().Single().FuncMap.Keys.Any(x => x == handler.ObjectName)
                        || handler.Children.Any(x => ((PropertyExport)x.ToExport(asset)).Property.PropertyFlags.HasFlag(EPropertyFlags.CPF_Parm)))
                        throw new InvalidDataException("Button click requires an owned, registered, zero-parameter handler: " + binding.FunctionName);
                });
            }
        foreach (var fn in asset.Exports.OfType<FunctionExport>().Where(x => x.ObjectName.ToString().StartsWith("MCDQoL_") || x.ObjectName.ToString() is "Tick" or "OpenCloseInventory" or "CanSalavage" or "SalvageSlot" or "SlotClicked"))
        {
            var offsets = new HashSet<uint>(); uint offset = 0;
            foreach (var root in fn.ScriptBytecode) { offsets.Add(offset); offset += (uint)root.GetSize(asset); }
            foreach (var root in fn.ScriptBytecode)
            {
                uint position = 0;
                root.Visit(asset, ref position, (e, _) => {
                    uint? target = e switch { EX_Jump j => j.CodeOffset, EX_JumpIfNot j => j.CodeOffset, EX_PushExecutionFlow p => p.PushingAddress, _ => null };
                    if (target != null && !offsets.Contains(target.Value)) throw new InvalidDataException("Jump target is not a root boundary: " + fn.ObjectName);
                });
            }
            if (fn.ObjectName.ToString().StartsWith("MCDQoL_")) DiagnosticGraphValidator.ValidateReferenceArguments(asset, fn.ScriptBytecode);
        }
        if (asset.Exports.OfType<ClassExport>().Single().ObjectName.ToString() == "UMG_InventoryItemInspector_C")
        {
            var guard = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "MCDQoL_IsFavorite");
            foreach (var name in new[] { "CanSalavage", "SalvageSlot" })
            {
                var fn = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == name);
                if (fn.ScriptBytecode[0] is not EX_JumpIfNot { BooleanExpression: EX_LocalFinalFunction f } || f.StackNode.ToExport(asset) != guard)
                    throw new InvalidDataException("Missing synchronous favorite guard: " + name);
                var expectedReturn = name == "CanSalavage" ? 2 : 1;
                if (fn.ScriptBytecode[expectedReturn] is not EX_Return) throw new InvalidDataException("Favorite path must return before vanilla salvage.");
            }
            var batch = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "MCDQoL_SalvageQueued");
            var nativeCalls = 0; var delegateCalls = 0;
            foreach (var root in batch.ScriptBytecode) {
                uint offset = 0; root.Visit(asset, ref offset, (e, _) => {
                    if (e is EX_FinalFunction f && f.StackNode.IsImport() && f.StackNode.ToImport(asset).ObjectName.ToString() == "SalvageItemInSlot") {
                        nativeCalls++;
                        if (f.Parameters.Length != 2 || f.Parameters[1] is not EX_LocalOutVariable) throw new InvalidDataException("Native success parameter ABI mismatch.");
                    }
                    if (e is EX_CallMulticastDelegate) delegateCalls++;
                });
            }
            if (nativeCalls != (nativeBatch ? 1 : 0) || delegateCalls != (nativeBatch ? 1 : 0)) throw new InvalidDataException("Batch mode/native undo notification mismatch.");
        }
    }
}

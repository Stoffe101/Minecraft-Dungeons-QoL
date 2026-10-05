using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

// Executes the generated action expressions with mocked object fields/native
// predicates. This tests graph logic, not UE's loader, reflection, UI or ABI.
static class InventoryActionTests
{
    public static void Run(string fixture)
    {
        var asset = new UAsset(fixture, EngineVersion.VER_UE4_22);
        // Actor fixtures need not have an empty function available for Graph's donor.
        var donor = (FunctionExport)asset.Exports.OfType<FunctionExport>().First().Clone();
        donor.ObjectName = new FName(asset, "MCDQoL_TestDonor"); donor.Children = Array.Empty<FPackageIndex>();
        asset.Exports.Add(donor);
        var g = new Graph(asset, "MCDQoL_ActionTestSeed");
        var itemClass = g.Class("/Script/Dungeons", "InventoryItem");
        var slotClass = g.Class("/Script/Dungeons", "InventoryItemSlot");
        var widgetClass = g.Class("/Script/ActionTest", "SlotWidget");
        var source = g.Object("TestSource", widgetClass, true);
        var slot = g.Object("TestPendingSlot", slotClass, true);
        var item = g.Object("TestPendingItem", itemClass, true);
        var request = g.Boolean("TestRequest", true);
        var mode = g.Boolean("TestMode", true); var armed = g.Boolean("TestArmed", true); var running = g.Boolean("TestRunning", true);
        g.Finish();
        var capture = new Graph(asset, "MCDQoL_TestCapture");
        InventoryActions.Capture(capture, request, slot, item, capture.I(source), widgetClass, slotClass); capture.Finish();
        var resolve = InventoryActions.Resolver(g, "MCDQoL_TestResolve", slot, item, slotClass, itemClass);
        var toggle = InventoryActions.ToggleMode(g, mode, armed, running);
        var state = new Dictionary<string, object?>(); var vm = new ActionVm(asset, state);
        object first = new(), second = new();
        var nativeSlot = new Dictionary<string, object?> { ["Item"] = first };
        var widget = new Dictionary<string, object?> { ["InventoryItemSlot"] = nativeSlot };
        state[source.ObjectName.ToString()] = widget;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("[PASS] action graph " + name); }
        vm.Run(capture.Function);
        Check(ReferenceEquals(state[slot.ObjectName.ToString()], nativeSlot), "captures the clicked physical slot");
        Check(ReferenceEquals(state[item.ObjectName.ToString()], first), "captures the item at the click");
        Check((bool)state[request.ObjectName.ToString()]!, "records the action request");
        Check((bool)vm.Run(resolve)!, "accepts unchanged slot/item identity");
        nativeSlot["Item"] = second;
        Check(!(bool)vm.Run(resolve)!, "rejects replacement item before tick");
        nativeSlot["Item"] = null;
        Check(!(bool)vm.Run(resolve)!, "rejects emptied slot");
        nativeSlot["Item"] = first;
        state[slot.ObjectName.ToString()] = null;
        Check(!(bool)vm.Run(resolve)!, "rejects missing pending slot");
        state[slot.ObjectName.ToString()] = nativeSlot; state[item.ObjectName.ToString()] = null;
        Check(!(bool)vm.Run(resolve)!, "rejects missing expected item");
        state[source.ObjectName.ToString()] = null; vm.Run(capture.Function);
        Check(state[slot.ObjectName.ToString()] == null && state[item.ObjectName.ToString()] == null, "null click source clears stale captures");
        state[source.ObjectName.ToString()] = widget; vm.Run(capture.Function);
        state[source.ObjectName.ToString()] = new Dictionary<string, object?>();
        Check((bool)vm.Run(resolve)!, "highlight change does not redirect a captured click");
        var queue = new List<object?> { first, second }; state["SelectionQueue"] = queue;
        state[mode.ObjectName.ToString()] = false; state[armed.ObjectName.ToString()] = true;
        state[running.ObjectName.ToString()] = false;
        vm.Run(toggle);
        Check((bool)state[mode.ObjectName.ToString()]!, "enters multi-select mode");
        Check(!(bool)state[armed.ObjectName.ToString()]!, "mode change cancels armed confirmation");
        vm.Run(toggle);
        Check(!(bool)state[mode.ObjectName.ToString()]!, "exits multi-select mode");
        Check(ReferenceEquals(state["SelectionQueue"], queue) && queue.Count == 2, "retains the queue when finishing selection");
        state[running.ObjectName.ToString()] = true; state[armed.ObjectName.ToString()] = true;
        vm.Run(toggle);
        Check(!(bool)state[mode.ObjectName.ToString()]! && (bool)state[armed.ObjectName.ToString()]!, "mode change is refused during salvage");
        // Prove tests are sensitive to the historical identity bug, not only happy paths.
        var old = resolve.ScriptBytecode.OfType<EX_LetBool>().Last().AssignmentExpression;
        var assignment = resolve.ScriptBytecode.OfType<EX_LetBool>().Last();
        assignment.AssignmentExpression = new EX_True(); nativeSlot["Item"] = second;
        Check((bool)vm.Run(resolve)!, "negative control detects removed identity comparison");
        assignment.AssignmentExpression = old;
        Check(!(bool)vm.Run(resolve)!, "restored identity comparison rejects replacement again");
        FunctionLayoutContracts.Validate(asset, resolve);
        void RejectLayout(Action mutate, Action restore, string expected) {
            mutate();
            try {
                FunctionLayoutContracts.Validate(asset, resolve);
                throw new Exception("Accepted invalid function layout: " + expected);
            } catch (InvalidDataException ex) {
                Check(ex.Message.Contains(expected), "rejects " + expected);
            } finally { restore(); }
        }
        var children = resolve.Children;
        RejectLayout(() => resolve.Children = children.Reverse().ToArray(), () => resolve.Children = children, "Parameter follows a local");
        var flags = resolve.FunctionFlags;
        RejectLayout(() => resolve.FunctionFlags &= ~EFunctionFlags.FUNC_HasDefaults, () => resolve.FunctionFlags = flags, "Missing local-initialization flag");
        RejectLayout(() => resolve.FunctionFlags &= ~EFunctionFlags.FUNC_HasOutParms, () => resolve.FunctionFlags = flags, "Missing out-parameter flag");
    }

    sealed class ActionVm(UAsset asset, Dictionary<string, object?> instance)
    {
        readonly Dictionary<int, object?> locals = new();
        string Name(FPackageIndex p) => p.IsImport() ? p.ToImport(asset).ObjectName.ToString() : p.ToExport(asset).ObjectName.ToString();
        object? Get(KismetExpression expression, object? target) => expression switch {
            EX_LocalVariable x => locals.GetValueOrDefault(x.Variable.Old.Index),
            EX_LocalOutVariable x => locals.GetValueOrDefault(x.Variable.Old.Index),
            EX_InstanceVariable x => target is Dictionary<string, object?> values ? values.GetValueOrDefault(Name(x.Variable.Old)) : null,
            _ => throw new NotSupportedException(expression.GetType().Name)
        };
        void Put(KismetExpression expression, object? value) {
            switch (expression) {
                case EX_LocalVariable x: locals[x.Variable.Old.Index] = value; break;
                case EX_LocalOutVariable x: locals[x.Variable.Old.Index] = value; break;
                case EX_InstanceVariable x: instance[Name(x.Variable.Old)] = value; break;
                default: throw new NotSupportedException(expression.GetType().Name);
            }
        }
        object? Eval(KismetExpression e, object? target) {
            switch (e) {
                case EX_True: return true;
                case EX_False: return false;
                case EX_NoObject: case EX_Nothing: return null;
                case EX_Self: return instance;
                case EX_ObjectConst x: return x.Value;
                case EX_LocalVariable: case EX_LocalOutVariable: case EX_InstanceVariable: return Get(e, target);
                case EX_Context x:
                    var obj = Eval(x.ObjectExpression, target);
                    return obj == null ? null : Eval(x.ContextExpression, obj);
                case EX_LetObj x: Put(x.VariableExpression, Eval(x.AssignmentExpression, target)); return null;
                case EX_LetBool x: Put(x.VariableExpression, Eval(x.AssignmentExpression, target)); return null;
                case EX_FinalFunction x:
                    // UE evaluates function arguments in Stack.Object (the caller),
                    // not the function's EX_Context receiver such as a library CDO.
                    var args = x.Parameters.Select(p => Eval(p, instance)).ToArray();
                    return Name(x.StackNode) switch {
                        "IsValid" => args[0] != null,
                        "EqualEqual_ObjectObject" => ReferenceEquals(args[0], args[1]),
                        "Not_PreBool" => !(bool)args[0]!,
                        _ => throw new NotSupportedException(Name(x.StackNode))
                    };
                default: throw new NotSupportedException(e.GetType().Name);
            }
        }
        public object? Run(FunctionExport fn) {
            locals.Clear();
            uint position = 0; var offsets = new Dictionary<uint, int>();
            for (var i = 0; i < fn.ScriptBytecode.Length; i++) { offsets.Add(position, i); position += (uint)fn.ScriptBytecode[i].GetSize(asset); }
            var pc = 0;
            for (var steps = 0; steps < 100; steps++) {
                var e = fn.ScriptBytecode[pc++];
                switch (e) {
                    case EX_JumpIfNot x: if (!(bool)Eval(x.BooleanExpression, instance)!) pc = offsets[x.CodeOffset]; break;
                    case EX_Jump x: pc = offsets[x.CodeOffset]; break;
                    case EX_Return x: return Eval(x.ReturnExpression, instance);
                    case EX_EndOfScript: return null;
                    default: Eval(e, instance); break;
                }
            }
            throw new InvalidDataException("Action graph did not terminate.");
        }
    }
}

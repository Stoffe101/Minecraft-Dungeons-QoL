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
        var yes = g.Boolean("TestYes", true); var no = g.Boolean("TestNo", true); var review = g.Boolean("TestReview", true);
        var snapshotSlots = g.ObjectArray("TestSnapshotSlots", slotClass, true); var snapshotItems = g.ObjectArray("TestSnapshotItems", itemClass, true);
        var batchIndex = g.Integer("TestIndex", true); var complete = g.Integer("TestComplete", true); var skipped = g.Integer("TestSkipped", true);
        var dismiss = InventoryActions.DismissReview(g, armed, yes, no, review);
        var approve = InventoryActions.ApproveReview(g, armed, running, snapshotSlots, snapshotItems, batchIndex, complete, skipped, true);
        // Use a second asset graph name to test the shipped non-destructive path too.
        var previewAsset = new UAsset(fixture, EngineVersion.VER_UE4_22);
        var previewDonor = (FunctionExport)previewAsset.Exports.OfType<FunctionExport>().First().Clone();
        previewDonor.Children = Array.Empty<FPackageIndex>(); previewAsset.Exports.Add(previewDonor);
        var previewSeed = new Graph(previewAsset, "MCDQoL_PreviewSeed");
        var previewArmed = previewSeed.Boolean("TestArmed", true); var previewRunning = previewSeed.Boolean("TestRunning", true);
        var previewSlots = previewSeed.ObjectArray("TestSnapshotSlots", previewSeed.Class("/Script/Dungeons", "InventoryItemSlot"), true);
        var previewItems = previewSeed.ObjectArray("TestSnapshotItems", previewSeed.Class("/Script/Dungeons", "InventoryItem"), true);
        var preview = InventoryActions.ApproveReview(previewSeed, previewArmed, previewRunning, previewSlots, previewItems,
            previewSeed.Integer("TestIndex", true), previewSeed.Integer("TestComplete", true), previewSeed.Integer("TestSkipped", true), false);
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
        state[yes.ObjectName.ToString()] = state[no.ObjectName.ToString()] = state[review.ObjectName.ToString()] = true;
        vm.Run(dismiss);
        Check(!(bool)state[armed.ObjectName.ToString()]!, "No dismisses the confirmation");
        Check(new[] { yes, no, review }.All(p => !(bool)state[p.ObjectName.ToString()]!), "No clears pending Yes and repeat Review requests");
        Check(ReferenceEquals(state["SelectionQueue"], queue) && queue.Count == 2, "No preserves selected physical items");
        state[snapshotSlots.ObjectName.ToString()] = new List<object?> { nativeSlot };
        state[snapshotItems.ObjectName.ToString()] = new List<object?> { first };
        state[running.ObjectName.ToString()] = false; vm.Run(approve);
        Check(!(bool)state[running.ObjectName.ToString()]!, "unarmed Yes cannot begin salvage");
        state[armed.ObjectName.ToString()] = true; state[snapshotSlots.ObjectName.ToString()] = new List<object?>(); vm.Run(approve);
        Check(!(bool)state[running.ObjectName.ToString()]!, "empty confirmation cannot begin salvage");
        state[armed.ObjectName.ToString()] = true; state[snapshotSlots.ObjectName.ToString()] = new List<object?> { nativeSlot, nativeSlot }; vm.Run(approve);
        Check(!(bool)state[running.ObjectName.ToString()]!, "mismatched snapshot cannot begin salvage");
        state[armed.ObjectName.ToString()] = true; state[snapshotSlots.ObjectName.ToString()] = new List<object?> { nativeSlot };
        state[batchIndex.ObjectName.ToString()] = 9; vm.Run(approve);
        Check((bool)state[running.ObjectName.ToString()]! && !(bool)state[armed.ObjectName.ToString()]!, "armed Yes starts only the reviewed batch");
        Check((int)state[batchIndex.ObjectName.ToString()]! == 0, "approved batch resets its cursor");
        state[batchIndex.ObjectName.ToString()] = 7; state[armed.ObjectName.ToString()] = true; vm.Run(approve);
        Check((int)state[batchIndex.ObjectName.ToString()]! == 7, "repeat Yes cannot restart a running batch");
        state[running.ObjectName.ToString()] = false; state[armed.ObjectName.ToString()] = true;
        new ActionVm(previewAsset, state).Run(preview);
        Check(!(bool)state[running.ObjectName.ToString()]! && !(bool)state[armed.ObjectName.ToString()]!, "preview Yes dismisses without starting deletion");
        // Prove tests are sensitive to the historical identity bug, not only happy paths.
        var old = resolve.ScriptBytecode.OfType<EX_LetBool>().Last().AssignmentExpression;
        var assignment = resolve.ScriptBytecode.OfType<EX_LetBool>().Last();
        assignment.AssignmentExpression = new EX_True(); nativeSlot["Item"] = second;
        Check((bool)vm.Run(resolve)!, "negative control detects removed identity comparison");
        assignment.AssignmentExpression = old;
        Check(!(bool)vm.Run(resolve)!, "restored identity comparison rejects replacement again");
        var presentation = new Graph(asset, "MCDQoL_TestVisibility");
        var ui = new InventoryAppearance(presentation, "TestPresentation");
        ui.VisibilityWhen(presentation.I(source), presentation.I(mode), 3);
        presentation.Label("END"); presentation.Finish();
        widget["Visibility"] = (byte)1; widget["VisibilityWrites"] = 0;
        state[source.ObjectName.ToString()] = widget; state[mode.ObjectName.ToString()] = false;
        vm.Run(presentation.Function);
        Check((int)widget["VisibilityWrites"]! == 0, "unchanged hidden mark avoids a visibility setter");
        state[mode.ObjectName.ToString()] = true; vm.Run(presentation.Function);
        Check((byte)widget["Visibility"]! == 3 && (int)widget["VisibilityWrites"]! == 1, "selected mark becomes hit-test invisible with one setter");
        vm.Run(presentation.Function); vm.Run(presentation.Function);
        Check((int)widget["VisibilityWrites"]! == 1, "unchanged selected mark never hides and shows again");
        state[mode.ObjectName.ToString()] = false; vm.Run(presentation.Function);
        Check((byte)widget["Visibility"]! == 1 && (int)widget["VisibilityWrites"]! == 2, "deselection collapses the mark once");
        var favoriteUi = new Graph(asset, "MCDQoL_TestFavoriteBadge");
        var badgeUi = new InventoryAppearance(favoriteUi, "TestBadge");
        var badgeFavorites = favoriteUi.ObjectArray("TestBadgeFavorites", itemClass, true);
        FavoriteBadge.ApplyState(favoriteUi, badgeUi, source, favoriteUi.I(item), favoriteUi.I(badgeFavorites));
        favoriteUi.Label("END"); favoriteUi.Finish();
        var lockedItems = new List<object?> { first }; state[badgeFavorites.ObjectName.ToString()] = lockedItems;
        state[item.ObjectName.ToString()] = first; vm.Run(favoriteUi.Function);
        Check((byte)widget["Visibility"]! == 3, "badge shows for the inspected favorite");
        vm.Run(favoriteUi.Function); var writes = (int)widget["VisibilityWrites"]!;
        vm.Run(favoriteUi.Function);
        Check((int)widget["VisibilityWrites"]! == writes, "unchanged favorite badge avoids a visibility setter");
        state[item.ObjectName.ToString()] = second; vm.Run(favoriteUi.Function);
        Check((byte)widget["Visibility"]! == 1, "badge does not transfer to another physical item");
        state[item.ObjectName.ToString()] = first; lockedItems.Clear(); vm.Run(favoriteUi.Function);
        Check((byte)widget["Visibility"]! == 1, "unfavoriting hides the inspected badge");
        state[item.ObjectName.ToString()] = null; lockedItems.Add(null); vm.Run(favoriteUi.Function);
        Check((byte)widget["Visibility"]! == 1, "empty inspector never shows a favorite badge");
        var markerGraph = new Graph(asset, "MCDQoL_TestEquipmentMarkers");
        var grid = markerGraph.ObjectArray("TestMarkerGrid", widgetClass, true);
        var equipment = markerGraph.ObjectArray("TestMarkerEquipment", widgetClass, true);
        var markerWidgets = markerGraph.ObjectArray("TestMarkerWidgets", widgetClass);
        MarkerWidgetCollection.Emit(markerGraph, markerWidgets, markerGraph.Object("TestMarkerWidget", widgetClass),
            markerGraph.Integer("TestMarkerLoop"), markerGraph.I(grid), markerGraph.I(equipment), widgetClass);
        markerGraph.Finish(markerGraph.L(markerWidgets));
        var allEquipment = Enumerable.Range(0, 6).Select(_ => (object?)new Dictionary<string, object?>()).ToList();
        var gridWidgets = new List<object?> { widget, allEquipment[0] };
        state[grid.ObjectName.ToString()] = gridWidgets; state[equipment.ObjectName.ToString()] = allEquipment;
        var combined = (List<object?>)vm.Run(markerGraph.Function)!;
        Check(combined.Count == 7 && allEquipment.All(combined.Contains), "marker collection includes all six equipped widgets");
        Check(combined.Count(x => ReferenceEquals(x, allEquipment[0])) == 1, "grid/equipment overlap creates no duplicate marker owner");
        Check(gridWidgets.Count == 2 && allEquipment.Count == 6, "marker collection never mutates native grid/equipment arrays");
        state[equipment.ObjectName.ToString()] = new List<object?> { allEquipment[0], null, allEquipment[1] };
        combined = (List<object?>)vm.Run(markerGraph.Function)!;
        Check(combined.Count == 3 && !combined.Contains(null), "missing equipment widget is skipped safely");
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
                case EX_IntConst x: return x.Value;
                case EX_ByteConst x: return x.Value;
                case EX_DynamicCast x: return Eval(x.Target, target); // fixture widgets have the expected class
                case EX_ArrayGetByRef x: return ((List<object?>)Eval(x.ArrayVariable, target)!)[(int)Eval(x.ArrayIndex, target)!];
                case EX_LocalVariable: case EX_LocalOutVariable: case EX_InstanceVariable: return Get(e, target);
                case EX_Context x:
                    var obj = Eval(x.ObjectExpression, target);
                    return obj == null ? null : Eval(x.ContextExpression, obj);
                case EX_LetObj x: Put(x.VariableExpression, Eval(x.AssignmentExpression, target)); return null;
                case EX_LetBool x: Put(x.VariableExpression, Eval(x.AssignmentExpression, target)); return null;
                case EX_Let x:
                    var assigned = Eval(x.Expression, target);
                    Put(x.Variable, assigned is List<object?> list ? new List<object?>(list) : assigned); return null;
                case EX_FinalFunction x:
                    // UE evaluates function arguments in Stack.Object (the caller),
                    // not the function's EX_Context receiver such as a library CDO.
                    var args = x.Parameters.Select(p => Eval(p, instance)).ToArray();
                    if (Name(x.StackNode) == "GetVisibility") return ((Dictionary<string, object?>)target!)["Visibility"];
                    if (Name(x.StackNode) == "SetVisibility") {
                        var values = (Dictionary<string, object?>)target!;
                        values["Visibility"] = args[0]; values["VisibilityWrites"] = (int)values["VisibilityWrites"]! + 1;
                        return null;
                    }
                    if (Name(x.StackNode) == "Array_AddUnique") {
                        var array = (List<object?>)args[0]!; var found = array.IndexOf(args[1]);
                        if (found >= 0) return found; array.Add(args[1]); return array.Count - 1;
                    }
                    return Name(x.StackNode) switch {
                        "IsValid" => args[0] != null,
                        "BooleanAND" => (bool)args[0]! && (bool)args[1]!,
                        "Array_Contains" => ((List<object?>)args[0]!).Contains(args[1]),
                        "EqualEqual_ObjectObject" => ReferenceEquals(args[0], args[1]),
                        "Not_PreBool" => !(bool)args[0]!,
                        "Array_Length" => ((List<object?>)args[0]!).Count,
                        "Greater_IntInt" => (int)args[0]! > (int)args[1]!,
                        "Less_IntInt" => (int)args[0]! < (int)args[1]!,
                        "Add_IntInt" => (int)args[0]! + (int)args[1]!,
                        "EqualEqual_IntInt" => (int)args[0]! == (int)args[1]!,
                        "NotEqual_ByteByte" => (byte)args[0]! != (byte)args[1]!,
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
            for (var steps = 0; steps < 1000; steps++) {
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

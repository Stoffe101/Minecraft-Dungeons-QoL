using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

if (args.Length is < 2 or > 4 || args.Skip(2).Any(x => x is not "--enable-salvage" and not "--favorites-only"))
{
    Console.Error.WriteLine("Usage: CookedInventoryFeatures <original Dungeons/Content directory> <output Dungeons/Content directory> [--enable-salvage] [--favorites-only]"); return 2;
}
var source = Path.GetFullPath(args[0]); var output = Path.GetFullPath(args[1]);
if (source == output || output.StartsWith(source + Path.DirectorySeparatorChar)) throw new ArgumentException("Use a separate output directory.");
var favoritesOnly = args.Contains("--favorites-only");
var destructive = args.Contains("--enable-salvage");
if (destructive && favoritesOnly) throw new ArgumentException("Favorites-only cannot enable batch salvage.");
var inspectorPath = "UI/Inventory/Inspector2/UMG_InventoryItemInspector.uasset";
var hudPath = "UI/Inventory/UMG_InventoryHUD.uasset";
var inspector = new UAsset(Path.Combine(source, inspectorPath), EngineVersion.VER_UE4_22);
var hud = new UAsset(Path.Combine(source, hudPath), EngineVersion.VER_UE4_22);
var originalCounts = new Dictionary<UAsset, int> { [inspector] = inspector.Exports.Count, [hud] = hud.Exports.Count };

// Guard the game's own eligibility and mutation functions. Protection is per physical item,
// scoped to this inspector's lifetime; no name/power matching or claimed persistent identity.
var guard = new Graph(inspector, "MCDQoL_IsFavorite");
var itemClass = guard.Existing("InventoryItem", "Class"); var slotClass = guard.Existing("InventoryItemSlot", "Class");
var favorites = guard.ObjectArray("MCDQoL_Favorites", itemClass, true);
var guardItem = guard.Object("Item", itemClass, flags: EPropertyFlags.CPF_Parm);
var guardReturn = guard.Boolean("ReturnValue", flags: EPropertyFlags.CPF_Parm | EPropertyFlags.CPF_OutParm | EPropertyFlags.CPF_ReturnParm);
guard.Set(guardReturn, guard.Array("Array_Contains", guard.I(favorites), guard.L(guardItem)));
guard.Finish(guard.L(guardReturn));
var can = inspector.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "CanSalavage");
var canOut = guard.Field("CanSalvage", "CanSalavage"); var inspected = guard.Field("InspectedItem");
var deny = new EX_LetBool { VariableExpression = guard.L(canOut), AssignmentExpression = new EX_False() };
var prefix = new KismetExpression[] {
    new EX_JumpIfNot { BooleanExpression = guard.Local(guard.Function, guard.I(inspected)) },
    deny, new EX_Return { ReturnExpression = new EX_Nothing() }
};
((EX_JumpIfNot)prefix[0]).CodeOffset = (uint)prefix.Sum(guard.Size);
Prepend(guard, can, prefix);
var vanillaSalvage = inspector.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "SalvageSlot");
var vanillaSlot = guard.Field("slot", "SalvageSlot");
var widgetSlotMember = guard.Existing("InventoryItemSlot", "ObjectProperty");
var itemMember = guard.Member(slotClass, "ObjectProperty", "Item");
var slotItem = guard.C(guard.C(guard.L(vanillaSlot), guard.V(widgetSlotMember), widgetSlotMember), guard.V(itemMember), itemMember);
var mutationPrefix = new KismetExpression[] {
    new EX_JumpIfNot { BooleanExpression = guard.Local(guard.Function, slotItem) },
    new EX_Return { ReturnExpression = new EX_Nothing() }
};
((EX_JumpIfNot)mutationPrefix[0]).CodeOffset = (uint)mutationPrefix.Sum(guard.Size);
Prepend(guard, vanillaSalvage, mutationPrefix);

// A guarded native-slot path preserves the game's success/undo delegate, including items
// whose widget is currently filtered out. The caller also rechecks all six equipment slots.
var batch = new Graph(inspector, "MCDQoL_SalvageQueued");
var batchSlot = batch.Object("Slot", slotClass, flags: EPropertyFlags.CPF_Parm);
var expected = batch.Object("ExpectedItem", itemClass, flags: EPropertyFlags.CPF_Parm);
var success = batch.Boolean("Success", flags: EPropertyFlags.CPF_Parm | EPropertyFlags.CPF_OutParm);
var actual = batch.Object("ActualItem", itemClass); var eligible = batch.Boolean("Eligible");
var owning = batch.Object("OwningPlayer", batch.Class("/Script/Engine", "PlayerController"));
var stash = batch.Object("Stash", batch.Existing("ItemStashComponent", "Class"));
var inventory = batch.ObjectArray("Inventory", slotClass);
var undoDonor = batch.Field("CallFunc_SalvageItemInSlot_ReturnValue", "SalvageSlot");
var undo = batch.Property("Undo", new UStructProperty { Struct = ((UStructProperty)undoDonor.Property).Struct }, "StructProperty");
batch.Bool(success, false);
batch.Obj(owning, batch.C(new EX_Self(), batch.F(batch.Fn(batch.Class("/Script/UMG", "Widget"), "GetOwningPlayer")), batch.Index(owning)));
batch.Branch(batch.Valid(batch.L(owning)), "DONE");
batch.Branch(batch.C(batch.L(owning), batch.F(batch.Fn(batch.Class("/Script/Engine", "Controller"), "IsLocalPlayerController"))), "DONE");
batch.Branch(batch.Valid(batch.L(batchSlot)), "DONE"); batch.Branch(batch.Valid(batch.L(expected)), "DONE");
batch.Obj(actual, batch.C(batch.L(batchSlot), batch.V(itemMember), batch.Index(actual)));
batch.Branch(batch.Math("EqualEqual_ObjectObject", batch.L(actual), batch.L(expected)), "DONE");
batch.Branch(batch.Not(batch.Local(guard.Function, batch.L(actual))), "DONE");
batch.Add(batch.Local(inspector.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "GetItemStash"), batch.L(stash)));
batch.Branch(batch.Valid(batch.L(stash)), "DONE");
batch.Set(inventory, batch.C(batch.L(stash), batch.F(batch.Fn(batch.Existing("ItemStashComponent", "Class"), "GetInventorySlots")), batch.Index(inventory)));
batch.Branch(batch.Array("Array_Contains", batch.L(inventory), batch.L(batchSlot)), "DONE");
batch.Add(batch.Local(inspector.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "SetInspectedItem"), batch.L(expected)));
batch.Add(batch.Local(can, batch.L(eligible))); batch.Branch(batch.L(eligible), "DONE");
if (destructive)
{
    batch.Set(undo, batch.C(batch.L(stash), batch.F(batch.Fn(batch.Existing("ItemStashComponent", "Class"), "SalvageItemInSlot"), batch.L(batchSlot), batch.L(success)), batch.Index(undo)));
    batch.Branch(batch.L(success), "DONE");
    var originalDelegate = vanillaSalvage.ScriptBytecode.OfType<EX_CallMulticastDelegate>().Single();
    batch.Add(new EX_CallMulticastDelegate { StackNode = originalDelegate.StackNode, Delegate = batch.I(batch.Field("OnItemSalvaged")), Parameters = new[] { batch.L(undo) } });
}
batch.Label("DONE"); batch.Finish();

var g = new Graph(hud, "MCDQoL_Tick");
var hudItem = g.Existing("InventoryItem", "Class"); var hudSlot = g.Existing("InventoryItemSlot", "Class");
var widgetSlotClass = g.Existing("UMG_InventorySlotBase_C"); var inspectorClass = g.Existing("UMG_InventoryItemInspector_C");
var hudInspector = g.Field("UMG_InventoryItemInspector"); var favoriteMember = g.Member(inspectorClass, "ArrayProperty", "MCDQoL_Favorites");
KismetExpression FavoriteArray() => g.C(g.I(hudInspector), g.V(favoriteMember), favoriteMember);
var pc = g.Object("Player", g.Class("/Script/Engine", "PlayerController"));
var localStash = g.Object("Stash", g.Existing("ItemStashComponent", "Class"));
var slots = g.ObjectArray("Slots", hudSlot); var equips = g.ObjectArray("Equipment", widgetSlotClass);
var current = g.Object("CurrentSlot", hudSlot); var currentItem = g.Object("CurrentItem", hudItem);
var equipWidget = g.Object("EquipWidget", widgetSlotClass); var equipSlot = g.Object("EquipSlot", hudSlot); var equipItem = g.Object("EquipItem", hudItem);
var selectedSlots = g.ObjectArray("MCDQoL_SelectedSlots", hudSlot, true); var selectedItems = g.ObjectArray("MCDQoL_SelectedItems", hudItem, true);
var snapshotSlots = g.ObjectArray("MCDQoL_BatchSlots", hudSlot, true); var snapshotItems = g.ObjectArray("MCDQoL_BatchItems", hudItem, true);
var armed = g.Boolean("MCDQoL_ConfirmArmed", true); var running = g.Boolean("MCDQoL_Running", true);
var index = g.Integer("MCDQoL_BatchIndex", true); var completed = g.Integer("MCDQoL_Completed", true); var skipped = g.Integer("MCDQoL_Skipped", true);
var loop = g.Integer("Loop"); var equipIndex = g.Integer("EquipIndex"); var queueIndex = g.Integer("QueueIndex");
var expectedItem = g.Object("Expected", hudItem); var didSalvage = g.Boolean("DidSalvage");
var vanillaEligible = g.Boolean("VanillaEligible");
var textClass = g.Class("/Script/UMG", "TextBlock"); var widgetClass = g.Class("/Script/UMG", "Widget");
var text = g.Object("MCDQoL_Text", textClass, true); var canvasSlotClass = g.Class("/Script/UMG", "CanvasPanelSlot");
var canvasSlot = g.Object("CanvasSlot", canvasSlotClass); var status = g.String("Status");
var cachedText = g.String("MCDQoL_CachedText", true);
var mode = g.Boolean("MCDQoL_SelectMode", true);
var clickedSlot = g.Object("MCDQoL_ClickedSlot", hudSlot, true);
var clickedItem = g.Object("MCDQoL_ClickedItem", hudItem, true);
var controls = new MouseControls(g, favoritesOnly, clickedSlot, clickedItem);
var toggleMode = InventoryActions.ToggleMode(g, mode, armed, running);
var click = new Graph(hud, "MCDQoL_SlotClicked");
var sourceWidget = click.Object("Source", widgetSlotClass, flags: EPropertyFlags.CPF_Parm);
click.Branch(click.I(mode), "DONE"); click.Branch(click.Not(click.I(running)), "DONE");
InventoryActions.Capture(click, controls.Select.Request, clickedSlot, clickedItem, click.L(sourceWidget), widgetSlotClass, hudSlot);
click.Label("DONE"); click.Finish();
if (!favoritesOnly) Prepend(g, hud.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "SlotClicked"), new[] { g.Local(click.Function, g.L(g.Field("Source", "SlotClicked"))) });

var key = g.Import("/Script/CoreUObject", "ScriptStruct", "Key", g.Package("/Script/InputCore"));
var cancel = new Graph(hud, "MCDQoL_CancelBatch");
cancel.Bool(armed, false, true); cancel.Bool(running, false, true); cancel.Bool(mode, false, true);
foreach (var control in controls.All) cancel.Bool(control.Request, false, true);
cancel.Obj(clickedSlot, new EX_NoObject(), true);
cancel.Obj(clickedItem, new EX_NoObject(), true);
cancel.Obj(controls.Favorite.TargetSlot!, new EX_NoObject(), true);
cancel.Obj(controls.Favorite.TargetItem!, new EX_NoObject(), true);
foreach (var array in new[] { selectedSlots, selectedItems, snapshotSlots, snapshotItems }) cancel.Add(cancel.Array("Array_Clear", cancel.I(array)));
cancel.Finish();
// Slate may stop ticking a hidden widget. Cancel in the real open/close function too,
// so closing through a mouse button cannot leave an armed confirmation behind.
Prepend(g, hud.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "OpenCloseInventory"), new[] { g.Local(cancel.Function) });
KismetExpression Press(string name) => g.C(g.L(pc), g.F(g.Fn(g.Class("/Script/Engine", "PlayerController"), "WasInputKeyJustPressed"), new EX_StructConst { Struct = key, StructSize = 32, Value = new KismetExpression[] { new EX_NameConst { Value = new FName(hud, name) } } }));
KismetExpression Length(KismetExpression array) => g.Array("Array_Length", array);
KismetExpression IsFavorite() => g.Array("Array_Contains", FavoriteArray(), g.L(currentItem));
KismetExpression IsQueued() => g.Array("Array_Contains", g.I(selectedItems), g.L(currentItem));
KismetExpression SlotItem(KismetExpression slot, PropertyExport result) => g.C(slot, g.V(g.Member(hudSlot, "ObjectProperty", "Item")), g.Index(result));
void Cancel() { g.Bool(armed, false, true); g.Bool(running, false, true); }
void ClearSelection() { Cancel(); g.Add(g.Array("Array_Clear", g.I(selectedSlots))); g.Add(g.Array("Array_Clear", g.I(selectedItems))); }
var addIndex = 0;
void AddCurrent() {
    var done = "ADD_DONE_" + addIndex++;
    g.Branch(g.Not(IsQueued()), done);
    g.Add(g.Array("Array_AddUnique", g.I(selectedSlots), g.L(current))); g.Add(g.Array("Array_AddUnique", g.I(selectedItems), g.L(currentItem)));
    g.Label(done);
}
void RemoveCurrent(string suffix)
{
    g.Set(queueIndex, g.Array("Array_Find", g.I(selectedItems), g.L(currentItem)));
    g.Branch(g.Math("GreaterEqual_IntInt", g.L(queueIndex), g.N(0)), "REMOVE_DONE_" + suffix);
    g.Add(g.Array("Array_Remove", g.I(selectedSlots), g.L(queueIndex))); g.Add(g.Array("Array_Remove", g.I(selectedItems), g.L(queueIndex)));
    g.Label("REMOVE_DONE_" + suffix);
}
// Inline predicate uses real stash membership, the native CanSalvage result, and six known
// equipment widgets. A missing equipment widget or malformed list excludes every candidate.
void Eligible(string reject, string suffix)
{
    g.Branch(g.Valid(g.L(current)), reject); g.Branch(g.Valid(g.L(currentItem)), reject);
    g.Branch(g.Array("Array_Contains", g.L(slots), g.L(current)), reject);
    g.Branch(g.Not(IsFavorite()), reject);
    g.Branch(g.C(g.L(currentItem), g.F(g.Fn(hudItem, "CanSalvage"))), reject);
    g.Branch(g.Math("EqualEqual_IntInt", Length(g.L(equips)), g.N(6)), reject);
    g.Set(equipIndex, g.N(0)); g.Label("EQUIP_" + suffix);
    g.Branch(g.Math("Less_IntInt", g.L(equipIndex), g.N(6)), "EQUIP_DONE_" + suffix);
    g.Obj(equipWidget, g.At(g.L(equips), g.L(equipIndex))); g.Branch(g.Valid(g.L(equipWidget)), reject);
    g.Obj(equipSlot, g.C(g.L(equipWidget), g.V(g.Member(widgetSlotClass, "ObjectProperty", "InventoryItemSlot")), g.Index(equipSlot)));
    g.Branch(g.Valid(g.L(equipSlot)), reject); g.Obj(equipItem, SlotItem(g.L(equipSlot), equipItem));
    g.Branch(g.Math("NotEqual_ObjectObject", g.L(equipItem), g.L(currentItem)), reject);
    g.Set(equipIndex, g.Math("Add_IntInt", g.L(equipIndex), g.N(1))); g.Jump("EQUIP_" + suffix); g.Label("EQUIP_DONE_" + suffix);
}
g.Obj(pc, g.C(new EX_Self(), g.F(g.Fn(g.Class("/Script/UMG", "Widget"), "GetOwningPlayer")), g.Index(pc)));
g.Branch(g.Valid(g.L(pc)), "CANCEL"); g.Branch(g.C(g.L(pc), g.F(g.Fn(g.Class("/Script/Engine", "Controller"), "IsLocalPlayerController"))), "CANCEL");
g.Branch(g.C(new EX_Self(), g.F(g.Fn(widgetClass, "IsVisible"))), "CANCEL"); g.Branch(g.Valid(g.I(hudInspector)), "CANCEL");
g.Obj(localStash, g.C(g.L(pc), g.F(g.Fn(g.Class("/Script/Dungeons", "BasePlayerController"), "GetItemStashComponent")), g.Index(localStash)));
g.Branch(g.Valid(g.L(localStash)), "CANCEL");
g.Set(slots, g.C(g.L(localStash), g.F(g.Fn(g.Existing("ItemStashComponent", "Class"), "GetInventorySlots")), g.Index(slots)));
g.Set(equips, g.I(g.Field("EquipSlots")));
// Cancel/clear works even when no item is highlighted; closing also cancels confirmation.
g.Branch(Press("Escape"), "CHECK_BATCH"); g.Add(g.Local(cancel.Function));
g.Label("CHECK_BATCH");
g.Branch(g.I(controls.Cancel.Request), "CHECK_MODE");
g.Add(g.Local(cancel.Function));
g.Label("CHECK_MODE"); g.Branch(g.Math("BooleanAND", g.I(controls.Mode.Request), g.Not(g.I(running))), "CHECK_RUNNING");
g.Bool(controls.Mode.Request, false, true); g.Add(g.Local(toggleMode));
g.Label("CHECK_RUNNING"); g.Branch(g.I(running), "INPUT");
g.Branch(g.Math("EqualEqual_IntInt", Length(g.I(snapshotSlots)), Length(g.I(snapshotItems))), "CANCEL");
g.Branch(g.Math("Less_IntInt", g.I(index), Length(g.I(snapshotSlots))), "BATCH_DONE");
g.Obj(current, g.At(g.I(snapshotSlots), g.I(index))); g.Obj(expectedItem, g.At(g.I(snapshotItems), g.I(index)));
g.Branch(g.Valid(g.L(current)), "SKIP"); g.Obj(currentItem, SlotItem(g.L(current), currentItem));
g.Branch(g.Math("EqualEqual_ObjectObject", g.L(currentItem), g.L(expectedItem)), "SKIP"); Eligible("SKIP", "BATCH");
g.Bool(didSalvage, false);
g.Add(g.C(g.I(hudInspector), g.Virtual("MCDQoL_SalvageQueued", g.L(current), g.L(expectedItem), g.L(didSalvage))));
g.Branch(g.L(didSalvage), "SKIP"); g.Set(completed, g.Math("Add_IntInt", g.I(completed), g.N(1)), true); g.Jump("ADVANCE");
g.Label("SKIP"); g.Set(skipped, g.Math("Add_IntInt", g.I(skipped), g.N(1)), true);
g.Label("ADVANCE"); g.Set(index, g.Math("Add_IntInt", g.I(index), g.N(1)), true); g.Jump("DISPLAY");
g.Label("BATCH_DONE"); ClearSelection(); g.Add(g.Array("Array_Clear", g.I(snapshotSlots))); g.Add(g.Array("Array_Clear", g.I(snapshotItems))); g.Jump("DISPLAY");
g.Label("INPUT");
g.Obj(current, g.C(g.I(g.Field("SelectedSlot")), g.V(g.Member(widgetSlotClass, "ObjectProperty", "InventoryItemSlot")), g.Index(current)));
g.Obj(currentItem, SlotItem(g.L(current), currentItem));
g.Branch(g.I(controls.Favorite.Request), "SELECT_ONE"); g.Bool(controls.Favorite.Request, false, true);
g.Branch(g.Local(controls.Favorite.Resolver!), "FAVORITE_CLEANUP");
g.Obj(current, g.I(controls.Favorite.TargetSlot!)); g.Obj(currentItem, g.I(controls.Favorite.TargetItem!)); Cancel();
g.Branch(IsFavorite(), "FAVORITE"); g.Add(g.Array("Array_RemoveItem", FavoriteArray(), g.L(currentItem))); g.Jump("REFRESH_BUTTON");
g.Label("FAVORITE"); g.Add(g.Array("Array_AddUnique", FavoriteArray(), g.L(currentItem))); RemoveCurrent("FAVORITE");
g.Label("REFRESH_BUTTON");
g.Add(g.C(g.I(hudInspector), g.Virtual("SetSalvageDialogVisibility", new EX_False())));
g.Label("FAVORITE_CLEANUP"); g.Obj(controls.Favorite.TargetSlot!, new EX_NoObject(), true); g.Obj(controls.Favorite.TargetItem!, new EX_NoObject(), true);
// Refresh vanilla eligibility visibility on the same physical inspected item.
var toggleMember = g.Member(inspectorClass, "ObjectProperty", "UMG_SalvageButtonToggle");
g.Label("SELECT_ONE"); g.Branch(g.I(controls.Select.Request), "SELECT_ALL"); g.Bool(controls.Select.Request, false, true); Cancel();
g.Branch(g.Local(controls.Select.Resolver!), "SELECT_CLEANUP"); g.Obj(current, g.I(clickedSlot)); g.Obj(currentItem, g.I(clickedItem));
g.Branch(IsQueued(), "QUEUE_ONE"); RemoveCurrent("TOGGLE"); g.Jump("SELECT_CLEANUP");
g.Label("QUEUE_ONE"); Eligible("SELECT_CLEANUP", "ONE"); AddCurrent();
g.Label("SELECT_CLEANUP"); g.Obj(clickedSlot, new EX_NoObject(), true); g.Obj(clickedItem, new EX_NoObject(), true);
g.Label("SELECT_ALL"); g.Branch(g.I(controls.AllItems.Request), "CONFIRM"); g.Bool(controls.AllItems.Request, false, true); g.Bool(mode, true, true); ClearSelection(); g.Set(loop, g.N(0));
g.Label("ALL_LOOP"); g.Branch(g.Math("Less_IntInt", g.L(loop), Length(g.L(slots))), "CONFIRM");
g.Obj(current, g.At(g.L(slots), g.L(loop))); g.Branch(g.Valid(g.L(current)), "ALL_NEXT"); g.Obj(currentItem, SlotItem(g.L(current), currentItem));
Eligible("ALL_NEXT", "ALL"); AddCurrent();
g.Label("ALL_NEXT"); g.Set(loop, g.Math("Add_IntInt", g.L(loop), g.N(1))); g.Jump("ALL_LOOP");
g.Label("CONFIRM"); g.Branch(g.I(controls.Review.Request), "DISPLAY"); g.Bool(controls.Review.Request, false, true);
g.Branch(g.Math("Greater_IntInt", Length(g.I(selectedSlots)), g.N(0)), "DISPLAY");
g.Branch(g.Math("EqualEqual_IntInt", Length(g.I(selectedSlots)), Length(g.I(selectedItems))), "CANCEL");
g.Branch(g.I(armed), "ARM");
if (destructive) {
    g.Bool(armed, false, true); g.Bool(running, true, true); g.Set(index, g.N(0), true);
    g.Set(completed, g.N(0), true); g.Set(skipped, g.N(0), true);
    g.Set(snapshotSlots, g.I(selectedSlots), true); g.Set(snapshotItems, g.I(selectedItems), true);
} else { g.Bool(armed, false, true); }
g.Jump("DISPLAY"); g.Label("ARM"); g.Bool(armed, true, true);
g.Label("DISPLAY");
g.Branch(g.Valid(g.I(text)), "CREATE_TEXT"); g.Jump("TEXT_READY");
g.Label("CREATE_TEXT");
g.Obj(text, new EX_DynamicCast { ClassPtr = textClass, Target = g.Static("GameplayStatics", "SpawnObject", g.O(textClass), new EX_Self()) }, true);
g.Branch(g.Valid(g.I(text)), "END");
g.Obj(canvasSlot, g.C(g.I(g.Field("WholeCanvas")), g.F(g.Fn(g.Class("/Script/UMG", "CanvasPanel"), "AddChildToCanvas"), g.I(text)), g.Index(canvasSlot)));
g.Branch(g.Valid(g.L(canvasSlot)), "END");
var vector = g.Import("/Script/CoreUObject", "ScriptStruct", "Vector2D", g.Package("/Script/CoreUObject"));
KismetExpression Vec(float x, float y) => new EX_StructConst { Struct = vector, StructSize = 8, Value = new KismetExpression[] { new EX_FloatConst { Value = x }, new EX_FloatConst { Value = y } } };
var anchors = g.Import("/Script/CoreUObject", "ScriptStruct", "Anchors", g.Package("/Script/Slate"));
g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasSlotClass, "SetAnchors"), new EX_StructConst { Struct = anchors, StructSize = 16, Value = new[] { Vec(.5f, 1f), Vec(.5f, 1f) } })));
g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasSlotClass, "SetAlignment"), Vec(.5f, 1f))));
g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasSlotClass, "SetPosition"), Vec(0, -120))));
g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasSlotClass, "SetSize"), Vec(1100, 115))));
g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasSlotClass, "SetZOrder"), g.N(100))));
g.Label("TEXT_READY"); controls.EnsureCreated(canvasSlot, Vec);
g.Add(g.C(g.I(text), g.F(g.Fn(widgetClass, "SetVisibility"), new EX_ByteConst { Value = 3 })));
g.Add(g.C(g.I(hudInspector), g.Virtual("CanSalavage", g.L(vanillaEligible))));
g.Add(g.C(g.C(g.I(hudInspector), g.V(toggleMember), toggleMember), g.F(g.Fn(widgetClass, "SetIsEnabled"), g.L(vanillaEligible))));
g.Set(status, g.S(destructive ? "MCD QoL (session favorites) | " : "MCD QoL SELECTION TEST (session favorites, batch disabled) | "));
g.Obj(current, g.C(g.I(g.Field("SelectedSlot")), g.V(g.Member(widgetSlotClass, "ObjectProperty", "InventoryItemSlot")), g.Index(current)));
g.Obj(currentItem, SlotItem(g.L(current), currentItem));
g.Branch(g.Valid(g.L(currentItem)), "STATUS");
var displayName = g.TextValue(g.C(g.L(currentItem), g.F(g.Fn(hudItem, "GetDisplayNameText"))), true);
var name = g.TextValue(g.Static("KismetTextLibrary", "Conv_TextToString", displayName));
g.Set(status, g.Concat(g.L(status), name));
g.Branch(IsFavorite(), "SHOW_QUEUED"); g.Set(status, g.Concat(g.L(status), g.S(" [FAVORITE - SALVAGE BLOCKED]")));
g.Label("SHOW_QUEUED"); g.Branch(IsQueued(), "STATUS"); g.Set(status, g.Concat(g.L(status), g.S(" [SELECTED]")));
g.Label("STATUS"); g.Set(status, g.Count(g.Concat(g.L(status), g.S("\nSelected: ")), Length(g.I(selectedItems)), " | Favorites: "));
g.Set(status, g.Count(g.L(status), Length(FavoriteArray()), " | Salvaged: "));
g.Set(status, g.Count(g.L(status), g.I(completed), " | Skipped: ")); g.Set(status, g.Count(g.L(status), g.I(skipped)));
g.Branch(g.I(armed), "NORMAL_HELP");
g.Set(status, g.Concat(g.L(status), g.S(destructive ? "\nClick Review / Confirm again to SALVAGE selected items. Undo restores the last item only. Cancel stops." : "\nPreview only. Batch salvage is disabled in this build. Esc clears."))); g.Jump("WRITE");
g.Label("NORMAL_HELP"); g.Branch(g.I(running), "REGULAR_HELP");
g.Set(status, g.Concat(g.L(status), g.S("\nSalvaging confirmed batch. Esc stops remaining items."))); g.Jump("WRITE");
g.Label("REGULAR_HELP");
g.Branch(g.I(mode), "MODE_OFF");
g.Set(status, g.Concat(g.L(status), g.S("\nMULTI SELECT ACTIVE: click items to toggle selection. Favorites and equipment are excluded."))); g.Jump("HELP_DONE");
g.Label("MODE_OFF"); g.Set(status, g.Concat(g.L(status), g.S(favoritesOnly ? "\nClick Favorite / Lock to toggle protection on the highlighted item." : "\nClick Multi salvage, then click inventory items. Select All excludes favorites and equipment.")));
g.Label("HELP_DONE");
g.Label("WRITE"); controls.UpdateEnabled(currentItem, running, selectedItems); g.Branch(g.Static("KismetStringLibrary", "EqualEqual_StrStr", g.L(status), g.I(cachedText)), "CHANGED"); g.Jump("END");
g.Label("CHANGED"); g.Set(cachedText, g.L(status), true);
var converted = g.TextValue(g.Static("KismetTextLibrary", "Conv_StringToText", g.L(status)), true);
g.Add(g.C(g.I(text), g.F(g.Fn(textClass, "SetText"), converted))); g.Jump("END");
g.Label("CANCEL"); g.Add(g.Local(cancel.Function));
g.Add(g.C(g.I(text), g.F(g.Fn(widgetClass, "SetVisibility"), new EX_ByteConst { Value = 1 })));
g.Label("END"); g.Finish();
var tick = hud.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "Tick");
if (tick.ScriptBytecode.Length != 5 || tick.ScriptBytecode[2] is not EX_LocalFinalFunction) throw new InvalidDataException("Unexpected HUD Tick contract.");
Prepend(g, tick, new[] { g.Local(g.Function) });

Write(inspector, inspectorPath); Write(hud, hudPath);
Console.WriteLine($"[OK] Original inspector and HUD patched; favorites guard both vanilla entry points, physical slot/item selection, native batch enabled={destructive}. Favorites are session-only.");
return 0;

void Write(UAsset asset, string relative)
{
    FeatureValidation.RepairAdded(asset, originalCounts[asset]); FeatureValidation.Validate(asset, destructive);
    var destination = Path.Combine(output, relative); Directory.CreateDirectory(Path.GetDirectoryName(destination)!); asset.Write(destination);
    var reopened = new UAsset(destination, EngineVersion.VER_UE4_22); FeatureValidation.Validate(reopened, destructive);
    FeatureValidation.VerifyPreserved(new UAsset(Path.Combine(source, relative), EngineVersion.VER_UE4_22), reopened);
    foreach (var function in reopened.Exports.OfType<FunctionExport>().Where(f => f.ObjectName.ToString().StartsWith("MCDQoL_")))
        DiagnosticGraphValidator.ValidateReferenceArguments(reopened, function.ScriptBytecode);
}

static void Prepend(Graph graph, FunctionExport function, KismetExpression[] prefix)
{
    var shift = (uint)prefix.Sum(graph.Size);
    foreach (var root in function.ScriptBytecode)
    {
        uint position = 0;
        root.Visit(graph.Asset, ref position, (e, _) => {
            switch (e) {
                case EX_Jump j: j.CodeOffset += shift; break;
                case EX_JumpIfNot j: j.CodeOffset += shift; break;
                case EX_PushExecutionFlow p: p.PushingAddress += shift; break;
                case EX_SwitchValue s:
                    s.EndGotoOffset += shift;
                    for (var i = 0; i < s.Cases.Length; i++) { var c = s.Cases[i]; c.NextOffset += shift; s.Cases[i] = c; }
                    break;
                case EX_ComputedJump: throw new InvalidDataException("Unsupported computed entry relocation in " + function.ObjectName);
            }
        });
    }
    function.ScriptBytecode = prefix.Concat(function.ScriptBytecode).ToArray(); function.ScriptBytecodeRaw = null;
    function.ScriptBytecodeSize = function.ScriptBytecode.Sum(graph.Size);
}

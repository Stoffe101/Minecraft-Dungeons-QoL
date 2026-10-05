using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

static class InventoryTileMarks
{
    // Marks belong to reusable widgets, but their state is always resolved from
    // the CURRENT native slot + physical item. Filtering/recycling cannot transfer a mark.
    public static void Update(Graph g, PropertyExport selectedSlots, PropertyExport selectedItems,
        KismetExpression favorites, FPackageIndex widgetSlotClass, FPackageIndex nativeSlotClass, FPackageIndex itemClass)
    {
        var ui = new InventoryAppearance(g, "Marks");
        var widgets = g.ObjectArray("MarkWidgets", widgetSlotClass);
        var owners = g.ObjectArray("MCDQoL_MarkOwners", widgetSlotClass, true);
        var borderClass = g.Class("/Script/UMG", "Border");
        var marks = Enumerable.Range(0, 5).Select(i => g.ObjectArray("MCDQoL_Marks_" + i, borderClass, true)).ToArray();
        var widget = g.Object("MarkWidget", widgetSlotClass); var root = g.Object("MarkRoot", g.Class("/Script/UMG", "CanvasPanel"));
        var slot = g.Object("MarkNativeSlot", nativeSlotClass); var item = g.Object("MarkItem", itemClass);
        var loop = g.Integer("MarkLoop"); var markIndex = g.Integer("MarkIndex"); var itemIndex = g.Integer("MarkItemIndex");
        var queued = g.Boolean("MarkQueued"); var locked = g.Boolean("MarkLocked");
        g.Set(widgets, g.I(g.Field("InventorySlotsInGrid")));
        // Release detached grid widgets; don't retain every historical grid rebuild.
        g.Set(loop, g.Math("Subtract_IntInt", g.Array("Array_Length", g.I(owners)), g.N(1)));
        g.Label("MARK_PRUNE"); g.Branch(g.Math("GreaterEqual_IntInt", g.L(loop), g.N(0)), "MARK_PRUNE_DONE");
        g.Branch(g.Not(g.Array("Array_Contains", g.L(widgets), g.At(g.I(owners), g.L(loop)))), "MARK_PRUNE_NEXT");
        foreach (var array in marks) {
            g.Add(g.C(g.At(g.I(array), g.L(loop)), g.F(g.Fn(g.Class("/Script/UMG", "Widget"), "RemoveFromParent"))));
            g.Add(g.Array("Array_Remove", g.I(array), g.L(loop)));
        }
        g.Add(g.Array("Array_Remove", g.I(owners), g.L(loop)));
        g.Label("MARK_PRUNE_NEXT"); g.Set(loop, g.Math("Subtract_IntInt", g.L(loop), g.N(1))); g.Jump("MARK_PRUNE");
        g.Label("MARK_PRUNE_DONE"); g.Set(loop, g.N(0));
        g.Label("MARK_LOOP"); g.Branch(g.Math("Less_IntInt", g.L(loop), g.Array("Array_Length", g.L(widgets))), "MARK_DONE");
        g.Obj(widget, g.At(g.L(widgets), g.L(loop))); g.Branch(g.Valid(g.L(widget)), "MARK_NEXT");
        g.Set(markIndex, g.Array("Array_Find", g.I(owners), g.L(widget)));
        g.Branch(g.Math("Less_IntInt", g.L(markIndex), g.N(0)), "MARK_READY");
        var tree = g.Member(g.Class("/Script/UMG", "UserWidget"), "ObjectProperty", "WidgetTree");
        var rootMember = g.Member(g.Class("/Script/UMG", "WidgetTree"), "ObjectProperty", "RootWidget");
        g.Obj(root, new EX_DynamicCast { ClassPtr = g.Class("/Script/UMG", "CanvasPanel"), Target = g.C(g.C(g.L(widget), g.V(tree), tree), g.V(rootMember), rootMember) });
        // Only attach to the observed canvas contract; unsupported roots fail closed.
        g.Branch(g.Valid(g.L(root)), "MARK_NEXT");
        var cyan = ui.Color(.08f, .8f, .95f);
        var edges = new[] {
            ui.Fill("MarkTop", g.L(root), 1, 1, 1, 3, cyan, maxX: 1, instance: false, z: 200),
            ui.Fill("MarkBottom", g.L(root), 1, -4, 1, 3, cyan, ay: 1, maxX: 1, instance: false, z: 200),
            ui.Fill("MarkLeft", g.L(root), 1, 1, 3, 1, cyan, maxY: 1, instance: false, z: 200),
            ui.Fill("MarkRight", g.L(root), -4, 1, 3, 1, cyan, ax: 1, maxY: 1, instance: false, z: 200),
            ui.Fill("MarkFavorite", g.L(root), -13, 5, 8, 8, ui.Color(1, .68f, .12f), ax: 1, instance: false, z: 200)
        };
        // Borders never receive mouse hits. Draw over native rarity/inspection frames.
        foreach (var e in edges) ui.Visibility(g.L(e), 1);
        g.Set(markIndex, g.Array("Array_Add", g.I(owners), g.L(widget)));
        for (var i = 0; i < marks.Length; i++) g.Add(g.Array("Array_Add", g.I(marks[i]), g.L(edges[i])));
        g.Label("MARK_READY"); g.Bool(queued, false); g.Bool(locked, false);
        g.Obj(slot, g.C(g.L(widget), g.V(g.Member(widgetSlotClass, "ObjectProperty", "InventoryItemSlot")), g.Index(slot)));
        g.Branch(g.Valid(g.L(slot)), "MARK_STATE");
        g.Obj(item, g.C(g.L(slot), g.V(g.Member(nativeSlotClass, "ObjectProperty", "Item")), g.Index(item)));
        g.Branch(g.Valid(g.L(item)), "MARK_STATE");
        g.Set(locked, g.Array("Array_Contains", favorites, g.L(item)));
        g.Branch(g.Not(g.L(locked)), "MARK_STATE");
        g.Set(itemIndex, g.Array("Array_Find", g.I(selectedItems), g.L(item)));
        g.Branch(g.Math("GreaterEqual_IntInt", g.L(itemIndex), g.N(0)), "MARK_STATE");
        g.Branch(g.Math("Less_IntInt", g.L(itemIndex), g.Array("Array_Length", g.I(selectedSlots))), "MARK_STATE");
        g.Set(queued, g.Math("EqualEqual_ObjectObject", g.At(g.I(selectedSlots), g.L(itemIndex)), g.L(slot)));
        g.Label("MARK_STATE");
        for (var i = 0; i < marks.Length; i++) {
            ui.VisibilityWhen(g.At(g.I(marks[i]), g.L(markIndex)), g.L(i == 4 ? locked : queued), 3);
        }
        g.Label("MARK_NEXT"); g.Set(loop, g.Math("Add_IntInt", g.L(loop), g.N(1))); g.Jump("MARK_LOOP"); g.Label("MARK_DONE");
    }
}

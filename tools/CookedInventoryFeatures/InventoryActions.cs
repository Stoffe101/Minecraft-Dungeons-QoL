using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

// Capture physical identity at the click, not at the later UI tick. A reused
// slot must never redirect a favorite/select request to its replacement item.
static class InventoryActions
{
    public static void Capture(Graph handler, PropertyExport request, PropertyExport slot,
        PropertyExport item, KismetExpression sourceWidget, FPackageIndex widgetClass, FPackageIndex slotClass)
    {
        handler.Obj(slot, handler.C(sourceWidget,
            handler.V(handler.Member(widgetClass, "ObjectProperty", "InventoryItemSlot")), handler.Index(slot)), true);
        handler.Obj(item, handler.C(handler.I(slot),
            handler.V(handler.Member(slotClass, "ObjectProperty", "Item")), handler.Index(item)), true);
        handler.Bool(request, true, true);
    }

    public static FunctionExport Resolver(Graph context, string name, PropertyExport slot, PropertyExport expected,
        FPackageIndex slotClass, FPackageIndex itemClass)
    {
        var check = new Graph(context.Asset, name);
        var result = check.Boolean("ReturnValue", flags: EPropertyFlags.CPF_Parm | EPropertyFlags.CPF_OutParm | EPropertyFlags.CPF_ReturnParm);
        var actual = check.Object("ActualItem", itemClass);
        check.Bool(result, false);
        check.Branch(check.Valid(check.I(slot)), "DONE");
        check.Branch(check.Valid(check.I(expected)), "DONE");
        check.Obj(actual, check.C(check.I(slot), check.V(check.Member(slotClass, "ObjectProperty", "Item")), check.Index(actual)));
        check.Set(result, check.Math("EqualEqual_ObjectObject", check.L(actual), check.I(expected)));
        check.Label("DONE"); check.Finish(check.L(result));
        return check.Function;
    }

    public static FunctionExport ToggleMode(Graph context, PropertyExport mode, PropertyExport armed, PropertyExport running)
    {
        var toggle = new Graph(context.Asset, "MCDQoL_ToggleSelectionMode");
        toggle.Branch(toggle.Not(toggle.I(running)), "DONE");
        toggle.Bool(armed, false, true);
        toggle.Set(mode, toggle.Not(toggle.I(mode)), true);
        // Leaving selection mode is not Cancel: retain the queue for Review.
        toggle.Label("DONE"); toggle.Finish();
        return toggle.Function;
    }
}

using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

static class MarkerWidgetCollection
{
    // Copy the native grid array, then include each valid equipment widget once.
    public static void Emit(Graph g, PropertyExport widgets, PropertyExport widget, PropertyExport loop,
        KismetExpression grid, KismetExpression equipment, FPackageIndex widgetClass)
    {
        g.Set(widgets, grid); g.Set(loop, g.N(0)); g.Label("MARK_EQUIPMENT_LOOP");
        g.Branch(g.Math("Less_IntInt", g.L(loop), g.Array("Array_Length", equipment)), "MARK_EQUIPMENT_DONE");
        g.Obj(widget, new EX_DynamicCast { ClassPtr = widgetClass, Target = g.At(equipment, g.L(loop)) });
        g.Branch(g.Valid(g.L(widget)), "MARK_EQUIPMENT_NEXT");
        g.Add(g.Array("Array_AddUnique", g.L(widgets), g.L(widget)));
        g.Label("MARK_EQUIPMENT_NEXT"); g.Set(loop, g.Math("Add_IntInt", g.L(loop), g.N(1))); g.Jump("MARK_EQUIPMENT_LOOP");
        g.Label("MARK_EQUIPMENT_DONE");
    }
}

using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;

static class FavoriteBadge
{
    public static void ApplyState(Graph g, InventoryAppearance ui, PropertyExport badge, KismetExpression item, KismetExpression favorites)
        => ui.VisibilityWhen(g.I(badge), g.Math("BooleanAND", g.Valid(item), g.Array("Array_Contains", favorites, item)), 3);
    // Append to the observed native tag row, never overlay names or stats.
    // Resolve from the inspected physical item rather than a pending HUD highlight.
    public static void Update(Graph g, PropertyExport inspector, KismetExpression favorites)
    {
        var ui = new InventoryAppearance(g, "FavoriteBadge");
        var widgetClass = g.Class("/Script/UMG", "Widget");
        var borderClass = g.Class("/Script/UMG", "Border");
        var textClass = g.Class("/Script/UMG", "TextBlock");
        var rowClass = g.Class("/Script/UMG", "HorizontalBox");
        var rowSlotClass = g.Class("/Script/UMG", "HorizontalBoxSlot");
        var infoClass = g.Import("/Script/UMG", "WidgetBlueprintGeneratedClass", "UMG_InventoryItemInspectInfo_C",
            g.Package("/Game/UI/Inventory/Inspector2/UMG_InventoryItemInspectInfo"));
        var tagClass = g.Import("/Script/UMG", "WidgetBlueprintGeneratedClass", "UMG_ItemTagIconName_C",
            g.Package("/Game/UI/Inventory/UMG_ItemTagIconName"));
        var info = g.Object("FavoriteBadgeInfo", infoClass);
        var tag = g.Object("FavoriteBadgeTags", tagClass);
        var row = g.Object("FavoriteBadgeRow", rowClass);
        var owner = g.Object("MCDQoL_FavoriteBadgeOwner", rowClass, true);
        var badge = g.Object("MCDQoL_FavoriteBadge", borderClass, true);
        var label = g.Object("FavoriteBadgeText", textClass);
        var slot = g.Object("FavoriteBadgeRowSlot", rowSlotClass);
        var item = g.Object("FavoriteBadgeItem", g.Existing("InventoryItem", "Class"));
        var inspectorClass = g.Existing("UMG_InventoryItemInspector_C");
        g.Obj(info, g.C(g.I(inspector), g.V(g.Member(inspectorClass, "ObjectProperty", "UMG_InventoryItemInspectInfo")), g.Index(info)));
        g.Branch(g.Valid(g.L(info)), "FAVORITE_BADGE_HIDE");
        g.Obj(tag, g.C(g.L(info), g.V(g.Member(infoClass, "ObjectProperty", "UMG_ItemTagIconName")), g.Index(tag)));
        g.Branch(g.Valid(g.L(tag)), "FAVORITE_BADGE_HIDE");
        var rarity = g.C(g.L(tag), g.V(g.Member(tagClass, "ObjectProperty", "UMG_ItemRarity")));
        g.Obj(row, new EX_DynamicCast { ClassPtr = rowClass,
            Target = g.C(rarity, g.F(g.Fn(widgetClass, "GetParent")), g.Index(row)) });
        g.Branch(g.Valid(g.L(row)), "FAVORITE_BADGE_HIDE");
        // Release/rebuild if the inspector replaces its native tag row.
        g.Branch(g.Valid(g.I(badge)), "FAVORITE_BADGE_CREATE");
        g.Branch(g.Math("EqualEqual_ObjectObject", g.I(owner), g.L(row)), "FAVORITE_BADGE_REPLACE");
        g.Jump("FAVORITE_BADGE_READY");
        g.Label("FAVORITE_BADGE_REPLACE");
        g.Add(g.C(g.I(badge), g.F(g.Fn(widgetClass, "RemoveFromParent"))));
        g.Label("FAVORITE_BADGE_CREATE");
        ui.Spawn(badge, borderClass);
        ui.Spawn(label, textClass, false);
        ui.Font(g.L(label), 16, g.C(g.L(info), g.V(g.Member(infoClass, "ObjectProperty", "ItemName"))));
        ui.Caption(g.L(label), g.S("FAVORITE"));
        ui.Visibility(g.L(label), 3);
        g.Add(g.C(g.I(badge), g.F(g.Fn(borderClass, "SetBrushColor"), ui.Color(.58f, .34f, .045f))));
        var margin = g.Import("/Script/CoreUObject", "ScriptStruct", "Margin", g.Package("/Script/SlateCore"));
        KismetExpression Pad(float left, float top, float right, float bottom) => new EX_StructConst { Struct = margin, StructSize = 16,
            Value = new KismetExpression[] { new EX_FloatConst { Value = left }, new EX_FloatConst { Value = top }, new EX_FloatConst { Value = right }, new EX_FloatConst { Value = bottom } } };
        g.Add(g.C(g.I(badge), g.F(g.Fn(borderClass, "SetPadding"), Pad(6, 1, 6, 1))));
        g.Add(g.C(g.I(badge), g.F(g.Fn(g.Class("/Script/UMG", "ContentWidget"), "SetContent"), g.L(label))));
        g.Obj(slot, g.C(g.L(row), g.F(g.Fn(rowClass, "AddChildToHorizontalBox"), g.I(badge)), g.Index(slot)));
        g.Branch(g.Valid(g.L(slot)), "FAVORITE_BADGE_HIDE");
        g.Add(g.C(g.L(slot), g.F(g.Fn(rowSlotClass, "SetPadding"), Pad(7, 0, 0, 0))));
        g.Add(g.C(g.L(slot), g.F(g.Fn(rowSlotClass, "SetVerticalAlignment"), new EX_ByteConst { Value = 1 })));
        g.Obj(owner, g.L(row), true);
        g.Label("FAVORITE_BADGE_READY");
        g.Obj(item, g.C(g.L(info), g.V(g.Member(infoClass, "ObjectProperty", "InspectedItem")), g.Index(item)));
        ApplyState(g, ui, badge, g.L(item), favorites);
        g.Jump("FAVORITE_BADGE_DONE");
        g.Label("FAVORITE_BADGE_HIDE");
        g.Branch(g.Valid(g.I(badge)), "FAVORITE_BADGE_DONE"); ui.Visibility(g.I(badge), 1);
        g.Label("FAVORITE_BADGE_DONE");
    }
}

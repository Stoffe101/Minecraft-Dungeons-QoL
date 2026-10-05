using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

// UE 4.22 reflected members; copy the retail font into a constructed local,
// rather than inventing FSlateFontInfo's native layout or nesting reference calls.
sealed class InventoryAppearance
{
    readonly Graph g;
    readonly FPackageIndex vector, anchors, margin, color, canvasClass, textClass, widgetClass, borderClass;
    readonly PropertyExport slot, font;
    readonly PropertyExport visibility;
    readonly string prefix;
    int visibilitySerial;
    int serial;
    public InventoryAppearance(Graph graph, string prefix = "Presentation")
    {
        g = graph; this.prefix = prefix;
        vector = g.Import("/Script/CoreUObject", "ScriptStruct", "Vector2D", g.Package("/Script/CoreUObject"));
        anchors = g.Import("/Script/CoreUObject", "ScriptStruct", "Anchors", g.Package("/Script/Slate"));
        margin = g.Import("/Script/CoreUObject", "ScriptStruct", "Margin", g.Package("/Script/SlateCore"));
        color = g.Import("/Script/CoreUObject", "ScriptStruct", "LinearColor", g.Package("/Script/CoreUObject"));
        canvasClass = g.Class("/Script/UMG", "CanvasPanelSlot");
        textClass = g.Class("/Script/UMG", "TextBlock"); widgetClass = g.Class("/Script/UMG", "Widget");
        borderClass = g.Class("/Script/UMG", "Border");
        slot = g.Object(prefix + "Slot", canvasClass);
        visibility = g.Property(prefix + "Visibility", new UByteProperty { Enum = g.Import("/Script/CoreUObject", "Enum", "ESlateVisibility", g.Package("/Script/UMG")) }, "ByteProperty");
        visibility.Property.ElementSize = 1;
        var fontStruct = g.Import("/Script/CoreUObject", "ScriptStruct", "SlateFontInfo", g.Package("/Script/SlateCore"));
        font = g.Property(prefix + "Font", new UStructProperty { Struct = fontStruct }, "StructProperty");
    }
    public KismetExpression Vec(float x, float y) => new EX_StructConst { Struct = vector, StructSize = 8,
        Value = new KismetExpression[] { new EX_FloatConst { Value = x }, new EX_FloatConst { Value = y } } };
    public KismetExpression Color(float r, float green, float b, float a = 1) => new EX_StructConst { Struct = color, StructSize = 16,
        Value = new KismetExpression[] { new EX_FloatConst { Value = r }, new EX_FloatConst { Value = green }, new EX_FloatConst { Value = b }, new EX_FloatConst { Value = a } } };
    public void Visibility(KismetExpression widget, byte value)
    {
        var done = prefix + "_VISIBILITY_UNCHANGED_" + visibilitySerial++;
        g.Set(visibility, g.C(widget, g.F(g.Fn(widgetClass, "GetVisibility")), g.Index(visibility)));
        g.Branch(g.Math("NotEqual_ByteByte", g.L(visibility), new EX_ByteConst { Value = value }), done);
        g.Add(g.C(widget, g.F(g.Fn(widgetClass, "SetVisibility"), new EX_ByteConst { Value = value })));
        g.Label(done);
    }
    public void VisibilityWhen(KismetExpression widget, KismetExpression condition, byte shown = 0)
    {
        var id = prefix + "_VISIBILITY_CHOICE_" + visibilitySerial++;
        g.Branch(condition, id + "_HIDE"); Visibility(widget, shown); g.Jump(id + "_DONE");
        g.Label(id + "_HIDE"); Visibility(widget, 1); g.Label(id + "_DONE");
    }
    public void Spawn(PropertyExport property, FPackageIndex cls, bool instance = true)
    {
        g.Obj(property, new EX_DynamicCast { ClassPtr = cls, Target = g.Static("GameplayStatics", "SpawnObject", g.O(cls), new EX_Self()) }, instance);
        g.Branch(g.Valid(instance ? g.I(property) : g.L(property)), "END");
    }
    public void Attach(KismetExpression parent, KismetExpression child, float x, float y, float width, float height,
        float ax = 0, float ay = 0, float alignX = 0, float alignY = 0, int z = 0, float? maxX = null, float? maxY = null)
    {
        g.Obj(slot, g.C(parent, g.F(g.Fn(g.Class("/Script/UMG", "CanvasPanel"), "AddChildToCanvas"), child), g.Index(slot)));
        g.Branch(g.Valid(g.L(slot)), "END");
        g.Add(g.C(g.L(slot), g.F(g.Fn(canvasClass, "SetAnchors"), new EX_StructConst { Struct = anchors, StructSize = 16, Value = new[] { Vec(ax, ay), Vec(maxX ?? ax, maxY ?? ay) } })));
        g.Add(g.C(g.L(slot), g.F(g.Fn(canvasClass, "SetAlignment"), Vec(alignX, alignY))));
        // Stretch anchors interpret offsets as margins, not a size.
        g.Add(g.C(g.L(slot), g.F(g.Fn(canvasClass, "SetOffsets"), new EX_StructConst { Struct = margin, StructSize = 16,
            Value = new KismetExpression[] { new EX_FloatConst { Value = x }, new EX_FloatConst { Value = y }, new EX_FloatConst { Value = width }, new EX_FloatConst { Value = height } } })));
        g.Add(g.C(g.L(slot), g.F(g.Fn(canvasClass, "SetZOrder"), g.N(z))));
    }
    public void Font(KismetExpression label, int size = 16)
    {
        g.Set(font, g.C(g.I(g.Field("InventorySpaceIndicator")), g.V(g.Member(textClass, "StructProperty", "Font")), g.Index(font)));
        var member = g.Member(((UStructProperty)font.Property).Struct, "IntProperty", "Size");
        g.Add(new EX_Let { Value = g.Ptr(member), Variable = new EX_StructMemberContext { StructMemberExpression = g.Ptr(member), StructExpression = g.L(font) }, Expression = g.N(size) });
        g.Add(g.C(label, g.F(g.Fn(textClass, "SetFont"), g.L(font))));
        g.Add(g.C(label, g.F(g.Fn(textClass, "SetShadowColorAndOpacity"), Color(0, 0, 0, .8f))));
        g.Add(g.C(label, g.F(g.Fn(textClass, "SetShadowOffset"), Vec(0, 1))));
    }
    public void Caption(KismetExpression label, KismetExpression message)
    {
        var converted = g.TextValue(g.Static("KismetTextLibrary", "Conv_StringToText", message), true);
        g.Add(g.C(label, g.F(g.Fn(textClass, "SetText"), converted)));
    }
    public void ReserveFooter()
    {
        // Observed retail ContentMargins uses a CanvasPanelSlot, with Bottom=0,
        // inside the existing 64-unit footer reservation. Preserve other margins.
        var layout = g.Property("FooterMargins", new UStructProperty { Struct = margin }, "StructProperty");
        var member = g.Member(widgetClass, "ObjectProperty", "Slot");
        g.Obj(slot, new EX_DynamicCast { ClassPtr = canvasClass, Target = g.C(g.I(g.Field("ContentMargins")), g.V(member), member) });
        g.Branch(g.Valid(g.L(slot)), "FOOTER_RESERVED");
        g.Set(layout, g.C(g.L(slot), g.F(g.Fn(canvasClass, "GetOffsets")), g.Index(layout)));
        var bottom = g.Member(margin, "FloatProperty", "Bottom");
        g.Add(new EX_Let { Value = g.Ptr(bottom), Variable = new EX_StructMemberContext { StructMemberExpression = g.Ptr(bottom), StructExpression = g.L(layout) }, Expression = new EX_FloatConst { Value = 52 } });
        g.Add(g.C(g.L(slot), g.F(g.Fn(canvasClass, "SetOffsets"), g.L(layout))));
        g.Label("FOOTER_RESERVED");
    }
    public PropertyExport Panel(string name, KismetExpression parent, float x, float y, float width, float height,
        float ax = 0, float ay = 0, float alignX = 0, float alignY = 0, int z = 0, bool stretch = false)
    {
        var p = g.Object(name, g.Class("/Script/UMG", "CanvasPanel"), true);
        Spawn(p, g.Class("/Script/UMG", "CanvasPanel"));
        Attach(parent, g.I(p), x, y, width, height, ax, ay, alignX, alignY, z, stretch ? 1 : null, stretch ? 1 : null);
        return p;
    }
    public PropertyExport Label(string name, KismetExpression parent, string message, float x, float y, float width, float height, int size = 16)
    {
        var p = g.Object(name, textClass, true); Spawn(p, textClass); Font(g.I(p), size); Caption(g.I(p), g.S(message));
        Visibility(g.I(p), 3); Attach(parent, g.I(p), x, y, width, height, z: 2); return p;
    }
    public PropertyExport Fill(string name, KismetExpression parent, float x, float y, float width, float height,
        KismetExpression tint, float ax = 0, float ay = 0, float? maxX = null, float? maxY = null, bool instance = true, int z = 1)
    {
        var p = g.Object(name + serial++, borderClass, instance); Spawn(p, borderClass, instance);
        var obj = instance ? g.I(p) : g.L(p);
        g.Add(g.C(obj, g.F(g.Fn(borderClass, "SetBrushColor"), tint))); Visibility(obj, 3);
        Attach(parent, obj, x, y, width, height, ax, ay, z: z, maxX: maxX, maxY: maxY); return p;
    }
}

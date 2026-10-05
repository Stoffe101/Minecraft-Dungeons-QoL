using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

// Each click records a request on this HUD. Tick consumes requests using its local
// player and current inventory state, never an actor/host-global controller.
sealed class MouseControls
{
    public sealed record Control(PropertyExport Button, PropertyExport Request, string Label, FunctionExport Handler);
    readonly Graph g;
    readonly FPackageIndex buttonClass, textClass, widgetClass, canvasClass;
    readonly PropertyExport label, binding;
    public readonly Control Favorite, Mode, Select, AllItems, Review, Cancel;
    public readonly Control[] All;
    readonly HashSet<Control> visible;
    public MouseControls(Graph graph, bool favoritesOnly)
    {
        g = graph;
        buttonClass = g.Class("/Script/UMG", "Button"); textClass = g.Class("/Script/UMG", "TextBlock");
        widgetClass = g.Class("/Script/UMG", "Widget"); canvasClass = g.Class("/Script/UMG", "CanvasPanelSlot");
        label = g.Object("ButtonLabel", textClass);
        var signature = g.Fn(g.Package("/Script/UMG"), "OnButtonClickedEvent__DelegateSignature");
        binding = g.Property("ButtonClickDelegate", new UDelegateProperty { SignatureFunction = signature }, "DelegateProperty");
        Control Create(string name, string title) {
            var button = g.Object("MCDQoL_Button_" + name, buttonClass, true);
            var request = g.Boolean("MCDQoL_Request_" + name, true);
            var handler = new Graph(g.Asset, "MCDQoL_Click_" + name);
            handler.Bool(request, true, true); handler.Finish();
            return new Control(button, request, title, handler.Function);
        }
        Favorite = Create("Favorite", "Favorite / Lock"); Mode = Create("Mode", "Multi salvage");
        Select = Create("Select", "Select item"); AllItems = Create("All", "Select All");
        Review = Create("Review", "Review / Confirm"); Cancel = Create("Cancel", "Cancel / Clear");
        All = new[] { Favorite, Mode, Select, AllItems, Review, Cancel };
        visible = (favoritesOnly ? new[] { Favorite } : All).ToHashSet();
    }
    public void EnsureCreated(PropertyExport canvasSlot, Func<float, float, KismetExpression> vec)
    {
        var anchors = g.Import("/Script/CoreUObject", "ScriptStruct", "Anchors", g.Package("/Script/Slate"));
        var shown = All.Where(visible.Contains).ToArray();
        for (var i = 0; i < shown.Length; i++)
        {
            var control = shown[i]; var start = "CREATE_BUTTON_" + i; var ready = "BUTTON_READY_" + i;
            g.Branch(g.Valid(g.I(control.Button)), start); g.Jump(ready); g.Label(start);
            g.Obj(control.Button, new EX_DynamicCast { ClassPtr = buttonClass, Target = g.Static("GameplayStatics", "SpawnObject", g.O(buttonClass), new EX_Self()) }, true);
            g.Branch(g.Valid(g.I(control.Button)), "END");
            g.Obj(label, new EX_DynamicCast { ClassPtr = textClass, Target = g.Static("GameplayStatics", "SpawnObject", g.O(textClass), new EX_Self()) });
            g.Branch(g.Valid(g.L(label)), "END");
            var caption = g.TextValue(g.Static("KismetTextLibrary", "Conv_StringToText", g.S(control.Label)), true);
            g.Add(g.C(g.L(label), g.F(g.Fn(textClass, "SetText"), caption)));
            g.Add(g.C(g.I(control.Button), g.F(g.Fn(g.Class("/Script/UMG", "ContentWidget"), "SetContent"), g.L(label))));
            g.Add(new EX_BindDelegate { FunctionName = control.Handler.ObjectName, Delegate = g.L(binding), ObjectTerm = new EX_Self() });
            var onClicked = g.Member(buttonClass, "MulticastDelegateProperty", "OnClicked");
            g.Add(new EX_AddMulticastDelegate { Delegate = g.C(g.I(control.Button), g.V(onClicked), onClicked), DelegateToAdd = g.L(binding) });
            g.Obj(canvasSlot, g.C(g.I(g.Field("WholeCanvas")), g.F(g.Fn(g.Class("/Script/UMG", "CanvasPanel"), "AddChildToCanvas"), g.I(control.Button)), g.Index(canvasSlot)));
            g.Branch(g.Valid(g.L(canvasSlot)), "END");
            g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasClass, "SetAnchors"), new EX_StructConst { Struct = anchors, StructSize = 16, Value = new[] { vec(.5f, 1f), vec(.5f, 1f) } })));
            g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasClass, "SetAlignment"), vec(.5f, 1f))));
            g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasClass, "SetPosition"), vec((i - (shown.Length - 1) / 2f) * 185, -68))));
            g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasClass, "SetSize"), vec(178, 42))));
            g.Add(g.C(g.L(canvasSlot), g.F(g.Fn(canvasClass, "SetZOrder"), g.N(101))));
            g.Label(ready);
        }
    }
    public void UpdateEnabled(PropertyExport item, PropertyExport running, PropertyExport selected)
    {
        void Enable(Control c, KismetExpression condition) {
            if (visible.Contains(c)) g.Add(g.C(g.I(c.Button), g.F(g.Fn(widgetClass, "SetIsEnabled"), condition)));
        }
        var idle = () => g.Not(g.I(running));
        Enable(Favorite, g.Math("BooleanAND", idle(), g.Valid(g.L(item))));
        Enable(Mode, idle()); Enable(Select, g.Math("BooleanAND", idle(), g.Valid(g.L(item))));
        Enable(AllItems, idle());
        Enable(Review, g.Math("BooleanAND", idle(), g.Math("Greater_IntInt", g.Array("Array_Length", g.I(selected)), g.N(0))));
        Enable(Cancel, new EX_True());
    }
}

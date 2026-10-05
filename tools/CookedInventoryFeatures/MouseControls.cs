using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

// Each click records a request on this HUD. Tick consumes requests using its local
// player and current inventory state, never an actor/host-global controller.
sealed class MouseControls
{
    public sealed record Control(PropertyExport Button, PropertyExport Request, string Label, FunctionExport Handler,
        PropertyExport? TargetSlot, PropertyExport? TargetItem, FunctionExport? Resolver);
    readonly Graph g;
    readonly FPackageIndex buttonClass, textClass, widgetClass;
    readonly PropertyExport label, binding;
    readonly InventoryAppearance appearance;
    readonly bool favoritesOnly, destructive;
    PropertyExport? modal, question;
    public readonly Control Favorite, Mode, Select, AllItems, Review, Cancel, Yes, No;
    public readonly Control[] All;
    readonly HashSet<Control> visible;
    public MouseControls(Graph graph, bool favoritesOnly, PropertyExport clickedSlot, PropertyExport clickedItem, bool destructive = false)
    {
        g = graph; this.favoritesOnly = favoritesOnly; this.destructive = destructive; appearance = new InventoryAppearance(g, "Controls");
        buttonClass = g.Class("/Script/UMG", "Button"); textClass = g.Class("/Script/UMG", "TextBlock");
        widgetClass = g.Class("/Script/UMG", "Widget");
        label = g.Object("ButtonLabel", textClass);
        var signature = g.Fn(g.Package("/Script/UMG"), "OnButtonClickedEvent__DelegateSignature");
        binding = g.Property("ButtonClickDelegate", new UDelegateProperty { SignatureFunction = signature }, "DelegateProperty");
        Control Create(string name, string title) {
            var button = g.Object("MCDQoL_Button_" + name, buttonClass, true);
            var request = g.Boolean("MCDQoL_Request_" + name, true);
            var handler = new Graph(g.Asset, "MCDQoL_Click_" + name);
            PropertyExport? slot = null, item = null;
            FunctionExport? resolver = null;
            if (name is "Favorite" or "Select") {
                var slotClass = g.Existing("InventoryItemSlot", "Class");
                var itemClass = g.Existing("InventoryItem", "Class");
                slot = name == "Select" ? clickedSlot : g.Object("MCDQoL_FavoriteTargetSlot", slotClass, true);
                item = name == "Select" ? clickedItem : g.Object("MCDQoL_FavoriteTargetItem", itemClass, true);
                InventoryActions.Capture(handler, request, slot, item, handler.I(handler.Field("SelectedSlot")),
                    g.Existing("UMG_InventorySlotBase_C"), slotClass);
                resolver = InventoryActions.Resolver(g, "MCDQoL_Resolve_" + name, slot, item, slotClass, itemClass);
            } else handler.Bool(request, true, true);
            handler.Finish();
            return new Control(button, request, title, handler.Function, slot, item, resolver);
        }
        Favorite = Create("Favorite", "Favorite"); Mode = Create("Mode", "Select items");
        Select = Create("Select", "Select item"); AllItems = Create("All", "Select all");
        Review = Create("Review", "Salvage"); Cancel = Create("Cancel", "Clear");
        Yes = Create("Yes", "Yes"); No = Create("No", "No");
        All = new[] { Favorite, Mode, Select, AllItems, Review, Cancel, Yes, No };
        visible = (favoritesOnly ? new[] { Favorite } : new[] { Favorite, Mode, AllItems, Review, Cancel, Yes, No }).ToHashSet();
    }
    public void EnsureCreated()
    {
        if (!favoritesOnly) {
            var ready = "MODAL_READY";
            modal = g.Object("MCDQoL_Modal", g.Class("/Script/UMG", "CanvasPanel"), true);
            g.Branch(g.Valid(g.I(modal)), "CREATE_MODAL"); g.Jump(ready); g.Label("CREATE_MODAL");
            appearance.Spawn(modal, g.Class("/Script/UMG", "CanvasPanel"));
            appearance.Attach(g.I(g.Field("WholeCanvas")), g.I(modal), 0, 0, 0, 0, z: 500, maxX: 1, maxY: 1);
            // A full-size native Button consumes background clicks; it has no action.
            var backdrop = g.Object("ModalBackdrop", buttonClass, true); appearance.Spawn(backdrop, buttonClass);
            g.Add(g.C(g.I(backdrop), g.F(g.Fn(buttonClass, "SetBackgroundColor"), appearance.Color(.015f, .02f, .025f, .94f))));
            appearance.Attach(g.I(modal), g.I(backdrop), 0, 0, 0, 0, maxX: 1, maxY: 1);
            var panel = appearance.Panel("ModalCard", g.I(modal), 0, 0, 620, 246, .5f, .5f, .5f, .5f, 1);
            appearance.Fill("ModalSurface", g.I(panel), 0, 0, 0, 0, appearance.Color(.07f, .085f, .1f), maxX: 1, maxY: 1);
            appearance.Fill("ModalAccent", g.I(panel), 0, 0, 0, 3, appearance.Color(.95f, .63f, .12f), maxX: 1);
            appearance.Label("ModalTitle", g.I(panel), "Salvage selected items?", 30, 25, 560, 34, 24);
            question = appearance.Label("MCDQoL_Question", g.I(panel), "", 30, 72, 560, 48, 18);
            appearance.Label("ModalHint", g.I(panel), destructive ? "Favorites and equipped items are excluded." : "Selection preview: no items will be salvaged.", 30, 126, 560, 28, 14);
            appearance.Visibility(g.I(modal), 1); g.Label(ready);
        }
        var shown = All.Where(visible.Contains).ToArray();
        for (var i = 0; i < shown.Length; i++)
        {
            var control = shown[i]; var start = "CREATE_BUTTON_" + i; var ready = "BUTTON_READY_" + i;
            g.Branch(g.Valid(g.I(control.Button)), start); g.Jump(ready); g.Label(start);
            g.Obj(control.Button, new EX_DynamicCast { ClassPtr = buttonClass, Target = g.Static("GameplayStatics", "SpawnObject", g.O(buttonClass), new EX_Self()) }, true);
            g.Branch(g.Valid(g.I(control.Button)), "END");
            g.Obj(label, new EX_DynamicCast { ClassPtr = textClass, Target = g.Static("GameplayStatics", "SpawnObject", g.O(textClass), new EX_Self()) });
            g.Branch(g.Valid(g.L(label)), "END");
            appearance.Font(g.L(label), 14); appearance.Caption(g.L(label), g.S(control.Label));
            g.Add(g.C(g.I(control.Button), g.F(g.Fn(g.Class("/Script/UMG", "ContentWidget"), "SetContent"), g.L(label))));
            g.Add(new EX_BindDelegate { FunctionName = control.Handler.ObjectName, Delegate = g.L(binding), ObjectTerm = new EX_Self() });
            var onClicked = g.Member(buttonClass, "MulticastDelegateProperty", "OnClicked");
            g.Add(new EX_AddMulticastDelegate { Delegate = g.C(g.I(control.Button), g.V(onClicked), onClicked), DelegateToAdd = g.L(binding) });
            var tint = control == Review || control == Yes ? appearance.Color(.65f, .31f, .12f) : appearance.Color(.16f, .22f, .28f);
            g.Add(g.C(g.I(control.Button), g.F(g.Fn(buttonClass, "SetBackgroundColor"), tint)));
            if (control == Yes || control == No)
                appearance.Attach(g.I(modal!), g.I(control.Button), control == No ? -82 : 82, 74, 140, 40, .5f, .5f, .5f, .5f, 3);
            else if (control == Favorite)
                appearance.Attach(g.I(g.Field("WholeCanvas")), g.I(control.Button), -50, -72, 156, 36, 1, 1, 1, 1, 101);
            else {
                var (x, width) = control == Mode ? (50, 124) : control == AllItems ? (186, 104) : control == Review ? (302, 104) : (418, 80);
                appearance.Attach(g.I(g.Field("WholeCanvas")), g.I(control.Button), x, -72, width, 36, 0, 1, 0, 1, 101);
            }
            g.Label(ready);
        }
    }
    public void UpdateEnabled(PropertyExport item, PropertyExport running, PropertyExport selected, PropertyExport armed,
        PropertyExport mode, KismetExpression favorite, PropertyExport snapshot)
    {
        void Enable(Control c, KismetExpression condition) {
            if (visible.Contains(c)) g.Add(g.C(g.I(c.Button), g.F(g.Fn(widgetClass, "SetIsEnabled"), condition)));
        }
        var idle = () => g.Math("BooleanAND", g.Not(g.I(running)), g.Not(g.I(armed)));
        Enable(Favorite, g.Math("BooleanAND", idle(), g.Valid(g.L(item))));
        Enable(Mode, idle()); Enable(Select, g.Math("BooleanAND", idle(), g.Valid(g.L(item))));
        Enable(AllItems, idle());
        Enable(Review, g.Math("BooleanAND", idle(), g.Math("Greater_IntInt", g.Array("Array_Length", g.I(selected)), g.N(0))));
        Enable(Cancel, new EX_True());
        Enable(Yes, g.I(armed)); Enable(No, g.I(armed));
        if (!favoritesOnly) {
            void Show(Control c, KismetExpression condition) {
                var next = "VISIBILITY_" + c.Label.Replace(" ", "_");
                appearance.Visibility(g.I(c.Button), 1); g.Branch(condition, next); appearance.Visibility(g.I(c.Button), 0); g.Label(next);
            }
            Show(AllItems, g.I(mode));
            Show(Review, g.Math("Greater_IntInt", g.Array("Array_Length", g.I(selected)), g.N(0)));
            Show(Cancel, g.Math("BooleanOR", g.I(mode), g.Math("Greater_IntInt", g.Array("Array_Length", g.I(selected)), g.N(0))));
            appearance.Visibility(g.I(modal!), 1); g.Branch(g.I(armed), "MODAL_HIDDEN"); appearance.Visibility(g.I(modal!), 0);
            var message = g.Count(g.S("Are you sure you want to salvage these "), g.Array("Array_Length", g.I(snapshot)), " items?");
            appearance.Caption(g.I(question!), message); g.Label("MODAL_HIDDEN");
            var modeCaption = g.String("ModeCaption"); g.Set(modeCaption, g.S("Select items"));
            g.Branch(g.I(mode), "MODE_CAPTION_READY"); g.Set(modeCaption, g.S("Done")); g.Label("MODE_CAPTION_READY");
            SetCaption(Mode, g.L(modeCaption));
        }
        var title = g.String("FavoriteCaption"); g.Set(title, g.S("Favorite"));
        g.Branch(favorite, "FAVORITE_CAPTION_READY"); g.Set(title, g.S("Unlock favorite")); g.Label("FAVORITE_CAPTION_READY");
        SetCaption(Favorite, g.L(title));
    }
    void SetCaption(Control control, KismetExpression message)
    {
        g.Obj(label, new EX_DynamicCast { ClassPtr = textClass, Target = g.C(g.I(control.Button), g.F(g.Fn(g.Class("/Script/UMG", "ContentWidget"), "GetContent")), g.Index(label)) });
        appearance.Caption(g.L(label), message);
    }
}

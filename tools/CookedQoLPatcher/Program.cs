using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: CookedQoLPatcher <source-actor.uasset> <output-actor.uasset>");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);

var asset = new UAsset(input, EngineVersion.VER_UE4_22);
KismetSerializer.asset = asset;

// ---------- import helpers ----------

FPackageIndex EnsurePackage(string name)
{
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.ClassName.ToString() == "Package" && imp.ObjectName.ToString() == name)
            return FPackageIndex.FromImport(i);
    }
    return asset.AddImport(new Import("/Script/CoreUObject", "Package", new FPackageIndex(0), name, false, asset));
}

FPackageIndex EnsureClass(string packageName, string className)
{
    var pkg = EnsurePackage(packageName);
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == pkg.Index && imp.ClassName.ToString() == "Class" && imp.ObjectName.ToString() == className)
            return FPackageIndex.FromImport(i);
    }
    return asset.AddImport(new Import("/Script/CoreUObject", "Class", pkg, className, false, asset));
}

FPackageIndex EnsureGeneratedClass(string packagePath, string className, bool widget = false)
{
    var pkg = EnsurePackage(packagePath);
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == pkg.Index && imp.ClassName.ToString() == (widget ? "WidgetBlueprintGeneratedClass" : "BlueprintGeneratedClass") && imp.ObjectName.ToString() == className)
            return FPackageIndex.FromImport(i);
    }
    return asset.AddImport(new Import(widget ? "/Script/UMG" : "/Script/Engine", widget ? "WidgetBlueprintGeneratedClass" : "BlueprintGeneratedClass", pkg, className, false, asset));
}

FPackageIndex EnsureDefault(string packageName, string className)
{
    var pkg = EnsurePackage(packageName);
    var objectName = "Default__" + className;
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == pkg.Index && imp.ClassName.ToString() == className && imp.ObjectName.ToString() == objectName)
            return FPackageIndex.FromImport(i);
    }
    return asset.AddImport(new Import(packageName, className, pkg, objectName, false, asset));
}

FPackageIndex EnsureFunction(FPackageIndex ownerClass, string name)
{
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == ownerClass.Index && imp.ClassName.ToString() == "Function" && imp.ObjectName.ToString() == name)
            return FPackageIndex.FromImport(i);
    }
    return asset.AddImport(new Import("/Script/CoreUObject", "Function", ownerClass, name, false, asset));
}

FPackageIndex EnsureMember(FPackageIndex owner, string propertyClassName, string name)
{
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == owner.Index && imp.ClassName.ToString() == propertyClassName && imp.ObjectName.ToString() == name)
            return FPackageIndex.FromImport(i);
    }
    return asset.AddImport(new Import("/Script/CoreUObject", propertyClassName, owner, name, false, asset));
}

FPackageIndex EnsureScriptStruct(string packageName, string name)
{
    var pkg = EnsurePackage(packageName);
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == pkg.Index && imp.ClassName.ToString() == "ScriptStruct" && imp.ObjectName.ToString() == name)
            return FPackageIndex.FromImport(i);
    }
    return asset.AddImport(new Import("/Script/CoreUObject", "ScriptStruct", pkg, name, false, asset));
}

FPackageIndex FindImport(string objectName, string? className = null)
{
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.ObjectName.ToString() == objectName && (className == null || imp.ClassName.ToString() == className))
            return FPackageIndex.FromImport(i);
    }
    throw new InvalidOperationException($"Import not found: {className ?? "*"} {objectName}");
}

// ---------- base exports ----------

var generatedClass = asset.Exports.OfType<ClassExport>().Single(x => x.ObjectName.ToString() == "BP_WASD_Movement_C");
var uber = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "ExecuteUbergraph_BP_WASD_Movement");
var playerControllerLocal = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "CallFunc_GetPlayerController_ReturnValue");
var pawnLocal = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "CallFunc_K2_GetPawn_ReturnValue");
var stashLocal = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "K2Node_DynamicCast_AsCharacter");

var intDonor = asset.Exports.OfType<PropertyExport>().First(x => x.Property is UIntProperty);
var boolDonor = asset.Exports.OfType<PropertyExport>().First(x => x.Property is UBoolProperty);
var objectDonor = asset.Exports.OfType<PropertyExport>().First(x => x.Property is UObjectProperty);

var intPropertyClass = EnsureClass("/Script/CoreUObject", "IntProperty");
var boolPropertyClass = EnsureClass("/Script/CoreUObject", "BoolProperty");
var objectPropertyClass = EnsureClass("/Script/CoreUObject", "ObjectProperty");
var arrayPropertyClass = EnsureClass("/Script/CoreUObject", "ArrayProperty");
var namePropertyClass = EnsureClass("/Script/CoreUObject", "NameProperty");

// ---------- native Dungeons reflection imports ----------

var itemStashClass = EnsureClass("/Script/Dungeons", "ItemStashComponent");
var itemSlotClass = EnsureClass("/Script/Dungeons", "InventoryItemSlot");
var inventoryItemClass = EnsureClass("/Script/Dungeons", "InventoryItem");
var slotItemMember = EnsureMember(itemSlotClass, "ObjectProperty", "Item");
// Contracts recovered from the user's actual UE4.22 UI/controller metadata.
var controllerBpClass = EnsureGeneratedClass("/Game/Actors/Characters/Player/BP_PlayerController", "BP_PlayerController_C");
var sharedUiClass = EnsureGeneratedClass("/Game/Actors/Characters/Player/BP_PlayerControllerSharedUI", "BP_PlayerControllerSharedUI_C");
var inventoryHudClass = EnsureGeneratedClass("/Game/UI/Inventory/UMG_InventoryHUD", "UMG_InventoryHUD_C", true);
var inventorySlotWidgetClass = EnsureGeneratedClass("/Game/UI/Inventory/UMG_InventorySlotBase", "UMG_InventorySlotBase_C", true);
var sharedUiMember = EnsureMember(controllerBpClass, "ObjectProperty", "SharedUI");
var inventoryHudMember = EnsureMember(sharedUiClass, "ObjectProperty", "InventoryHUD");
var inventoryOpenMember = EnsureMember(inventoryHudClass, "BoolProperty", "IsInventoryOpen");
var equipWidgetsMember = EnsureMember(inventoryHudClass, "ArrayProperty", "EquipSlots");
var widgetNativeSlotMember = EnsureMember(inventorySlotWidgetClass, "ObjectProperty", "InventoryItemSlot");
var getStashFn = EnsureFunction(EnsureClass("/Script/Dungeons", "BasePlayerController"), "GetItemStashComponent");
var getInventorySlotsFn = EnsureFunction(itemStashClass, "GetInventorySlots");
var getSalvageInfoFn = EnsureFunction(itemStashClass, "GetSalvageInfo");
var salvageInfoStruct = EnsureScriptStruct("/Script/Dungeons", "ItemSalvageInfo");
var enchantmentPointsMember = EnsureMember(salvageInfoStruct, "IntProperty", "enchantmentPoints");


// Retarget an existing object local to stash.
stashLocal.ObjectName = new FName(asset, "CallFunc_GetItemStashComponent_ReturnValue");
if (stashLocal.Property is not UObjectProperty stashProp)
    throw new InvalidDataException("Expected UObjectProperty donor for stash local.");
stashProp.PropertyClass = itemStashClass;

// ---------- property creation ----------

PropertyExport AddProperty(PropertyExport donor, string name, FPackageIndex outer, FPackageIndex propertyClass, UProperty reflected)
{
    var p = (PropertyExport)donor.Clone();
    p.ObjectName = new FName(asset, name);
    p.OuterIndex = outer;
    p.ClassIndex = propertyClass;
    p.SuperIndex = new FPackageIndex(0);
    p.TemplateIndex = new FPackageIndex(0);
    p.SerialOffset = 0;
    p.SerialSize = 0;
    p.SerializationBeforeSerializationDependencies.Clear();
    p.CreateBeforeSerializationDependencies.Clear();
    p.SerializationBeforeCreateDependencies.Clear();
    p.CreateBeforeCreateDependencies.Clear();
    p.Property = reflected;
    asset.Exports.Add(p);
    return p;
}

UIntProperty NewInt(EPropertyFlags flags = EPropertyFlags.CPF_BlueprintVisible) => new()
{
    ArrayDim = intDonor.Property.ArrayDim,
    ElementSize = 0,
    PropertyFlags = flags,
    RepNotifyFunc = new FName(asset, "None"),
    BlueprintReplicationCondition = ELifetimeCondition.COND_None,
    Next = new FPackageIndex(0)
};

UBoolProperty NewBool(EPropertyFlags flags = EPropertyFlags.CPF_BlueprintVisible) => new()
{
    ArrayDim = boolDonor.Property.ArrayDim,
    ElementSize = 1,
    NativeBool = ((UBoolProperty)boolDonor.Property).NativeBool,
    PropertyFlags = flags,
    RepNotifyFunc = new FName(asset, "None"),
    BlueprintReplicationCondition = ELifetimeCondition.COND_None,
    Next = new FPackageIndex(0)
};

UObjectProperty NewObject(FPackageIndex cls, EPropertyFlags flags = EPropertyFlags.CPF_BlueprintVisible) => new()
{
    ArrayDim = objectDonor.Property.ArrayDim,
    ElementSize = 0,
    PropertyFlags = flags,
    RepNotifyFunc = new FName(asset, "None"),
    BlueprintReplicationCondition = ELifetimeCondition.COND_None,
    Next = new FPackageIndex(0),
    PropertyClass = cls
};

UNameProperty NewName(EPropertyFlags flags = EPropertyFlags.CPF_None) => new()
{
    ArrayDim = intDonor.Property.ArrayDim,
    ElementSize = 0,
    PropertyFlags = flags,
    RepNotifyFunc = new FName(asset, "None"),
    BlueprintReplicationCondition = ELifetimeCondition.COND_None,
    Next = new FPackageIndex(0)
};

PropertyExport AddNameArray(string name, FPackageIndex outer, EPropertyFlags flags)
{
    var array = AddProperty(objectDonor, name, outer, arrayPropertyClass, new UArrayProperty
    {
        ArrayDim = objectDonor.Property.ArrayDim,
        ElementSize = 0,
        PropertyFlags = flags,
        RepNotifyFunc = new FName(asset, "None"),
        BlueprintReplicationCondition = ELifetimeCondition.COND_None,
        Next = new FPackageIndex(0),
        Inner = new FPackageIndex(0)
    });
    var inner = AddProperty(intDonor, name + "_Inner", FPackageIndex.FromExport(asset.Exports.IndexOf(array)), namePropertyClass, NewName());
    ((UArrayProperty)array.Property).Inner = FPackageIndex.FromExport(asset.Exports.IndexOf(inner));
    return array;
}

PropertyExport AddObjectArray(string name, FPackageIndex outer, FPackageIndex objectClass, EPropertyFlags flags)
{
    var array = AddProperty(objectDonor, name, outer, arrayPropertyClass, new UArrayProperty
    {
        ArrayDim = objectDonor.Property.ArrayDim,
        ElementSize = 0,
        PropertyFlags = flags,
        RepNotifyFunc = new FName(asset, "None"),
        BlueprintReplicationCondition = ELifetimeCondition.COND_None,
        Next = new FPackageIndex(0),
        Inner = new FPackageIndex(0)
    });
    var inner = AddProperty(objectDonor, name + "_Inner", FPackageIndex.FromExport(asset.Exports.IndexOf(array)), objectPropertyClass, NewObject(objectClass, EPropertyFlags.CPF_None));
    ((UArrayProperty)array.Property).Inner = FPackageIndex.FromExport(asset.Exports.IndexOf(inner));
    return array;
}

var classOuter = FPackageIndex.FromExport(asset.Exports.IndexOf(generatedClass));
var functionOuter = FPackageIndex.FromExport(asset.Exports.IndexOf(uber));

var cursor = AddProperty(intDonor, "CursorIndex", classOuter, intPropertyClass, NewInt());
var confirmArmed = AddProperty(boolDonor, "ConfirmArmed", classOuter, boolPropertyClass, NewBool());
var salvageRunning = AddProperty(boolDonor, "SalvageRunning", classOuter, boolPropertyClass, NewBool());
var selectionOwner = AddProperty(objectDonor, "SelectionOwner", classOuter, objectPropertyClass, NewObject(itemStashClass));
var batchIndex = AddProperty(intDonor, "BatchIndex", classOuter, intPropertyClass, NewInt());

var saveClass = EnsureGeneratedClass("/Game/Mods/MinecraftDungeonsQoL/SG_MCDQoL", "SG_MCDQoL_C");
var saveState = AddProperty(objectDonor, "SaveState", classOuter, objectPropertyClass, NewObject(saveClass));
var selectedSlots = AddObjectArray("SelectedSlots", classOuter, itemSlotClass, EPropertyFlags.CPF_BlueprintVisible);
var selectedItems = AddObjectArray("SelectedItems", classOuter, inventoryItemClass, EPropertyFlags.CPF_BlueprintVisible);
// ArrayGetByRef must read a reflected array variable, not a nested function temporary.
var inventorySlots = AddObjectArray("InventorySlots", functionOuter, itemSlotClass, EPropertyFlags.CPF_None);

// Function scratch locals.
var currentSlot = AddProperty(objectDonor, "MCDQoL_CurrentSlot", functionOuter, objectPropertyClass, NewObject(itemSlotClass, EPropertyFlags.CPF_None));
var currentItem = AddProperty(objectDonor, "MCDQoL_CurrentItem", functionOuter, objectPropertyClass, NewObject(inventoryItemClass, EPropertyFlags.CPF_None));
var currentKey = AddProperty(intDonor, "MCDQoL_CurrentKey", functionOuter, namePropertyClass, NewName());
var controllerBpLocal = AddProperty(objectDonor, "MCDQoL_Controller", functionOuter, objectPropertyClass, NewObject(controllerBpClass, EPropertyFlags.CPF_None));
var sharedUiLocal = AddProperty(objectDonor, "MCDQoL_SharedUI", functionOuter, objectPropertyClass, NewObject(sharedUiClass, EPropertyFlags.CPF_None));
var inventoryHudLocal = AddProperty(objectDonor, "MCDQoL_InventoryHUD", functionOuter, objectPropertyClass, NewObject(inventoryHudClass, EPropertyFlags.CPF_None));
var equipWidgetsLocal = AddObjectArray("MCDQoL_EquipWidgets", functionOuter, inventorySlotWidgetClass, EPropertyFlags.CPF_None);
var equipIndexLocal = AddProperty(intDonor, "MCDQoL_EquipIndex", functionOuter, intPropertyClass, NewInt(EPropertyFlags.CPF_None));
var equipWidgetLocal = AddProperty(objectDonor, "MCDQoL_EquipWidget", functionOuter, objectPropertyClass, NewObject(inventorySlotWidgetClass, EPropertyFlags.CPF_None));
var equipNativeSlotLocal = AddProperty(objectDonor, "MCDQoL_EquipNativeSlot", functionOuter, objectPropertyClass, NewObject(itemSlotClass, EPropertyFlags.CPF_None));
var salvageInfoLocal = AddProperty(objectDonor, "MCDQoL_SalvageInfo", functionOuter,
    EnsureClass("/Script/CoreUObject", "StructProperty"), new UStructProperty {
        Struct = salvageInfoStruct, ArrayDim = objectDonor.Property.ArrayDim, ElementSize = 0,
        PropertyFlags = EPropertyFlags.CPF_None, RepNotifyFunc = new FName(asset, "None"),
        BlueprintReplicationCondition = ELifetimeCondition.COND_None, Next = new FPackageIndex(0)
    });

// Register reflected fields with their owning structs/classes.
generatedClass.Children = generatedClass.Children
    .Concat(new[] {
        FPackageIndex.FromExport(asset.Exports.IndexOf(cursor)),
        FPackageIndex.FromExport(asset.Exports.IndexOf(confirmArmed)),
        FPackageIndex.FromExport(asset.Exports.IndexOf(salvageRunning)),
        FPackageIndex.FromExport(asset.Exports.IndexOf(batchIndex)),
        FPackageIndex.FromExport(asset.Exports.IndexOf(saveState)),
        FPackageIndex.FromExport(asset.Exports.IndexOf(selectedSlots)),
        Exp(selectionOwner), Exp(selectedItems)
    }).ToArray();

uber.Children = uber.Children
    .Concat(new[] {
        FPackageIndex.FromExport(asset.Exports.IndexOf(currentSlot)),
        FPackageIndex.FromExport(asset.Exports.IndexOf(currentItem)),
        FPackageIndex.FromExport(asset.Exports.IndexOf(currentKey)),
        Exp(inventorySlots), Exp(controllerBpLocal), Exp(sharedUiLocal), Exp(inventoryHudLocal),
        Exp(equipWidgetsLocal), Exp(equipIndexLocal), Exp(equipWidgetLocal), Exp(equipNativeSlotLocal), Exp(salvageInfoLocal)
    }).ToArray();

// External SaveGame Records field.
var recordsMember = EnsureMember(saveClass, "ArrayProperty", "Records");

// ---------- engine helper imports ----------

var gameplayClass = EnsureClass("/Script/Engine", "GameplayStatics");
var gameplayDefault = EnsureDefault("/Script/Engine", "GameplayStatics");
var getPlayerControllerFn = EnsureFunction(gameplayClass, "GetPlayerController");
var doesSaveExistFn = EnsureFunction(gameplayClass, "DoesSaveGameExist");
var loadSaveFn = EnsureFunction(gameplayClass, "LoadGameFromSlot");
var createSaveFn = EnsureFunction(gameplayClass, "CreateSaveGameObject");
var saveToSlotFn = EnsureFunction(gameplayClass, "SaveGameToSlot");

var playerControllerClass = EnsureClass("/Script/Engine", "PlayerController");
var inputFn = EnsureFunction(playerControllerClass, "WasInputKeyJustPressed");
var getPawnFn = EnsureFunction(EnsureClass("/Script/Engine", "Controller"), "K2_GetPawn");

var mathClass = EnsureClass("/Script/Engine", "KismetMathLibrary");
var addIntFn = EnsureFunction(mathClass, "Add_IntInt");
var subIntFn = EnsureFunction(mathClass, "Subtract_IntInt");
var equalIntFn = EnsureFunction(mathClass, "EqualEqual_IntInt");
var greaterEqFn = EnsureFunction(mathClass, "GreaterEqual_IntInt");
var lessFn = EnsureFunction(mathClass, "Less_IntInt");
var equalObjectFn = EnsureFunction(mathClass, "EqualEqual_ObjectObject");
var arrayRemoveIndexFn = EnsureFunction(EnsureClass("/Script/Engine", "KismetArrayLibrary"), "Array_RemoveIndex");
var arrayFindFn = EnsureFunction(EnsureClass("/Script/Engine", "KismetArrayLibrary"), "Array_Find");

var systemClass = EnsureClass("/Script/Engine", "KismetSystemLibrary");
var systemDefault = EnsureDefault("/Script/Engine", "KismetSystemLibrary");
var printStringFn = EnsureFunction(systemClass, "PrintString");
var isValidFn = EnsureFunction(systemClass, "IsValid");

var stringClass = EnsureClass("/Script/Engine", "KismetStringLibrary");
var stringDefault = EnsureDefault("/Script/Engine", "KismetStringLibrary");
var buildStringIntFn = EnsureFunction(stringClass, "BuildString_Int");
var convStringToNameFn = EnsureFunction(stringClass, "Conv_StringToName");

var textClass = EnsureClass("/Script/Engine", "KismetTextLibrary");
var textDefault = EnsureDefault("/Script/Engine", "KismetTextLibrary");
var convTextToStringFn = EnsureFunction(textClass, "Conv_TextToString");

var arrayClass = EnsureClass("/Script/Engine", "KismetArrayLibrary");
var arrayDefault = EnsureDefault("/Script/Engine", "KismetArrayLibrary");
var arrayLengthFn = EnsureFunction(arrayClass, "Array_Length");
var arrayContainsFn = EnsureFunction(arrayClass, "Array_Contains");
var arrayAddFn = EnsureFunction(arrayClass, "Array_Add");
var arrayRemoveFn = EnsureFunction(arrayClass, "Array_Remove");
var arrayClearFn = EnsureFunction(arrayClass, "Array_Clear");

var keyStruct = EnsureScriptStruct("/Script/InputCore", "Key");
var linearColorStruct = EnsureScriptStruct("/Script/CoreUObject", "LinearColor");

// ---------- expression helpers ----------

KismetPropertyPointer Ptr(FPackageIndex i) => new(i);
FPackageIndex Exp(PropertyExport p) => FPackageIndex.FromExport(asset.Exports.IndexOf(p));
EX_LocalVariable Local(PropertyExport p) => new() { Variable = Ptr(Exp(p)) };
EX_InstanceVariable Inst(PropertyExport p) => new() { Variable = Ptr(Exp(p)) };
EX_InstanceVariable ImportedVar(FPackageIndex p) => new() { Variable = Ptr(p) };
EX_IntConst Int(int v) => new() { Value = v };
EX_StringConst Str(string v) => new() { Value = v };
EX_NameConst Name(string v) => new() { Value = new FName(asset, v) };
EX_ObjectConst Obj(FPackageIndex v) => new() { Value = v };
EX_Self Self() => new();
EX_True True() => new();
EX_False False() => new();

int ICode(KismetExpression expr)
{
    using var stream = new MemoryStream();
    using var writer = new AssetBinaryWriter(stream, asset);
    return UAssetAPI.Kismet.Bytecode.ExpressionSerializer.WriteExpression(expr, writer);
}

EX_Context Ctx(KismetExpression obj, KismetExpression expression, KismetPropertyPointer? result = null) => new()
{
    ObjectExpression = obj,
    ContextExpression = expression,
    Offset = (uint)ICode(expression),
    PropertyType = 0,
    RValuePointer = result ?? Ptr(new FPackageIndex(0))
};

EX_FinalFunction Final(FPackageIndex fn, params KismetExpression[] pars) => new() { StackNode = fn, Parameters = pars };
EX_VirtualFunction Virtual(string name, params KismetExpression[] pars) => new() { VirtualFunctionName = new FName(asset, name), Parameters = pars };
EX_CallMath Math(FPackageIndex fn, params KismetExpression[] pars) => new() { StackNode = fn, Parameters = pars };

EX_StructConst Key(string key) => new()
{
    Struct = keyStruct,
    StructSize = 32,
    Value = new KismetExpression[] { Name(key) }
};

EX_StructConst Yellow() => new()
{
    Struct = linearColorStruct,
    StructSize = 16,
    Value = new KismetExpression[]
    {
        new EX_FloatConst { Value = 1f },
        new EX_FloatConst { Value = 0.85f },
        new EX_FloatConst { Value = 0.1f },
        new EX_FloatConst { Value = 1f }
    }
};

KismetExpression Static(FPackageIndex defaultObject, FPackageIndex fn, params KismetExpression[] pars) =>
    Ctx(Obj(defaultObject), Final(fn, pars));

KismetExpression IsValid(KismetExpression obj) => Static(systemDefault, isValidFn, obj);
KismetExpression InventorySize() => ArrayLength(Local(inventorySlots));
KismetExpression KeyPressed(string key) => Ctx(Local(playerControllerLocal), Final(inputFn, Key(key)));
KismetExpression ArrayLength(KismetExpression array) => Static(arrayDefault, arrayLengthFn, array);
KismetExpression ArrayContains(KismetExpression array, KismetExpression value) => Static(arrayDefault, arrayContainsFn, array, value);
KismetExpression Records() => Ctx(Inst(saveState), ImportedVar(recordsMember), Ptr(recordsMember));

KismetExpression CurrentSlotExpr() => new EX_ArrayGetByRef
{
    ArrayVariable = Local(inventorySlots),
    ArrayIndex = Inst(cursor)
};

KismetExpression SlotItem(KismetExpression slot) => Ctx(slot, ImportedVar(slotItemMember), Ptr(slotItemMember));

KismetExpression Fingerprint()
{
    var item = Local(currentItem);
    var displayText = Ctx(item, Virtual("GetDisplayNameText"));
    var displayString = Static(textDefault, convTextToStringFn, displayText);
    var withPower = Static(stringDefault, buildStringIntFn,
        displayString,
        Str("|P"),
        Ctx(item, Virtual("GetDisplayItemPowerInt")),
        Str(""));
    var withEnchant = Static(stringDefault, buildStringIntFn,
        withPower,
        Str("|E"),
        new EX_StructMemberContext { StructMemberExpression = Ptr(enchantmentPointsMember), StructExpression = Local(salvageInfoLocal) },
        Str(""));
    return Static(stringDefault, convStringToNameFn, withEnchant);
}

KismetExpression SlotText() => Static(stringDefault, buildStringIntFn,
    Str("MCD QoL | slot "),
    Str(""),
    Inst(cursor),
    Str(" | F5 clear | F6/F7 browse | F8 protect | F9 select | F10 PREVIEW")
);

KismetExpression Print(KismetExpression text) => Static(systemDefault, printStringFn,
    Self(), text, True(), False(), Yellow(), new EX_FloatConst { Value = 2.0f });

KismetExpression SaveSidecar() => Static(gameplayDefault, saveToSlotFn,
    Inst(saveState), Str("MinecraftDungeonsQoL_v1"), Int(0));

// ---------- assembler ----------

var code = new List<KismetExpression> { DiagnosticGraphValidator.TickDispatch(asset) };
var labels = new Dictionary<string,int>(StringComparer.Ordinal);
var jumps = new List<(KismetExpression Jump,string Target)>();

void Label(string name) => labels[name] = code.Count;
void Add(KismetExpression expr) => code.Add(expr);
void JumpIfNot(KismetExpression cond, string target)
{
    var j = new EX_JumpIfNot { CodeOffset = 0, BooleanExpression = cond };
    jumps.Add((j,target));
    Add(j);
}
void Jump(string target)
{
    var j = new EX_Jump { CodeOffset = 0 };
    jumps.Add((j,target));
    Add(j);
}
void SetInt(PropertyExport p, KismetExpression value) => Add(new EX_Let { Value = Ptr(Exp(p)), Variable = Inst(p), Expression = value });
void SetBool(PropertyExport p, KismetExpression value) => Add(new EX_LetBool { VariableExpression = Inst(p), AssignmentExpression = value });
void SetObj(PropertyExport p, KismetExpression value) => Add(new EX_LetObj { VariableExpression = Inst(p), AssignmentExpression = value });
void SetLocalObj(PropertyExport p, KismetExpression value) => Add(new EX_LetObj { VariableExpression = Local(p), AssignmentExpression = value });
void SetLocalName(PropertyExport p, KismetExpression value) => Add(new EX_Let { Value = Ptr(Exp(p)), Variable = Local(p), Expression = value });

void ReadFingerprint()
{
    // GetTotalInvestedEnchantmentPoints is not an InventoryItem instance call.
    // The vanilla inspector obtains the same refund field through GetSalvageInfo(Item).
    Add(new EX_Let { Value = Ptr(Exp(salvageInfoLocal)), Variable = Local(salvageInfoLocal),
        Expression = Ctx(Local(stashLocal), Final(getSalvageInfoFn, Local(currentItem)), Ptr(Exp(salvageInfoLocal))) });
    SetLocalName(currentKey, Fingerprint());
}
void RejectEquipped(string reject, string prefix)
{
    // The HUD initializes this array with exactly armor/melee/ranged and three artifacts.
    // Missing UI/native slots are unresolved equipment state: fail closed.
    JumpIfNot(Math(equalIntFn, ArrayLength(Local(equipWidgetsLocal)), Int(6)), reject);
    Add(new EX_Let { Value = Ptr(Exp(equipIndexLocal)), Variable = Local(equipIndexLocal), Expression = Int(0) });
    Label(prefix + "_LOOP");
    JumpIfNot(Math(lessFn, Local(equipIndexLocal), ArrayLength(Local(equipWidgetsLocal))), prefix + "_DONE");
    SetLocalObj(equipWidgetLocal, new EX_ArrayGetByRef { ArrayVariable = Local(equipWidgetsLocal), ArrayIndex = Local(equipIndexLocal) });
    JumpIfNot(IsValid(Local(equipWidgetLocal)), reject);
    SetLocalObj(equipNativeSlotLocal, Ctx(Local(equipWidgetLocal), ImportedVar(widgetNativeSlotMember), Ptr(widgetNativeSlotMember)));
    JumpIfNot(IsValid(Local(equipNativeSlotLocal)), reject);
    JumpIfNot(Math(equalObjectFn, Local(currentItem), SlotItem(Local(equipNativeSlotLocal))), prefix + "_NEXT");
    Jump(reject);
    Label(prefix + "_NEXT");
    Add(new EX_Let { Value = Ptr(Exp(equipIndexLocal)), Variable = Local(equipIndexLocal),
        Expression = Math(addIntFn, Local(equipIndexLocal), Int(1)) });
    Jump(prefix + "_LOOP");
    Label(prefix + "_DONE");
}

void ClearSelection()
{
    Add(Static(arrayDefault, arrayClearFn, Inst(selectedSlots)));
    Add(Static(arrayDefault, arrayClearFn, Inst(selectedItems)));
    SetBool(salvageRunning, False());
    SetBool(confirmArmed, False());
}
void SaveAndReport(string message)
{
    JumpIfNot(SaveSidecar(), "SAVE_FAILED");
    Add(Print(Str(message)));
}

// Resolve local player + stash; never keep a selection across pawn/hero changes.
SetLocalObj(playerControllerLocal, Static(gameplayDefault, getPlayerControllerFn, Self(), Int(0)));
JumpIfNot(IsValid(Local(playerControllerLocal)), "INVALID_CONTEXT");
SetLocalObj(controllerBpLocal, new EX_DynamicCast { ClassPtr = controllerBpClass, Target = Local(playerControllerLocal) });
JumpIfNot(IsValid(Local(controllerBpLocal)), "INVALID_CONTEXT");
SetLocalObj(sharedUiLocal, Ctx(Local(controllerBpLocal), ImportedVar(sharedUiMember), Ptr(sharedUiMember)));
JumpIfNot(IsValid(Local(sharedUiLocal)), "INVALID_CONTEXT");
SetLocalObj(inventoryHudLocal, new EX_DynamicCast { ClassPtr = inventoryHudClass,
    Target = Ctx(Local(sharedUiLocal), ImportedVar(inventoryHudMember), Ptr(inventoryHudMember)) });
JumpIfNot(IsValid(Local(inventoryHudLocal)), "INVALID_CONTEXT");
JumpIfNot(Ctx(Local(inventoryHudLocal), ImportedVar(inventoryOpenMember), Ptr(inventoryOpenMember)), "INVALID_CONTEXT");
Add(new EX_Let { Value = Ptr(Exp(equipWidgetsLocal)), Variable = Local(equipWidgetsLocal),
    Expression = Ctx(Local(inventoryHudLocal), ImportedVar(equipWidgetsMember), Ptr(equipWidgetsMember)) });

SetLocalObj(pawnLocal, Ctx(Local(playerControllerLocal), Final(getPawnFn), Ptr(Exp(pawnLocal))));
JumpIfNot(IsValid(Local(pawnLocal)), "INVALID_CONTEXT");
SetLocalObj(stashLocal, Ctx(Local(controllerBpLocal), Final(getStashFn), Ptr(Exp(stashLocal))));

JumpIfNot(IsValid(Local(stashLocal)), "INVALID_CONTEXT");
JumpIfNot(Math(equalObjectFn, Inst(selectionOwner), Local(stashLocal)), "CHANGE_OWNER");
Jump("OWNER_READY");
Label("CHANGE_OWNER");
ClearSelection();
SetObj(selectionOwner, Local(stashLocal));
SetInt(cursor, Int(0));
Label("OWNER_READY");
Add(new EX_Let { Value = Ptr(Exp(inventorySlots)), Variable = Local(inventorySlots),
    Expression = Ctx(Local(stashLocal), Final(getInventorySlotsFn), Ptr(Exp(inventorySlots))) });
// F5 cancels both confirmation and preview; it also clears the selected snapshots.
JumpIfNot(KeyPressed("F5"), "AFTER_CANCEL");
ClearSelection();
Add(Print(Str("MCD QoL | selection cleared")));
Jump("END");
Label("AFTER_CANCEL");

// Load/create our separate sidecar save once.
JumpIfNot(IsValid(Inst(saveState)), "INIT_SAVE");
Jump("AFTER_SAVE");
Label("INIT_SAVE");
JumpIfNot(Static(gameplayDefault, doesSaveExistFn, Str("MinecraftDungeonsQoL_v1"), Int(0)), "CREATE_SAVE");
SetObj(saveState, new EX_DynamicCast { ClassPtr = saveClass, Target = Static(gameplayDefault, loadSaveFn, Str("MinecraftDungeonsQoL_v1"), Int(0)) });
Jump("AFTER_SAVE");
Label("CREATE_SAVE");
SetObj(saveState, Static(gameplayDefault, createSaveFn, Obj(saveClass)));
JumpIfNot(SaveSidecar(), "SAVE_FAILED");
Label("AFTER_SAVE");
JumpIfNot(IsValid(Inst(saveState)), "SAVE_FAILED");

// Diagnostic preview only. No destructive native function is emitted until the
// hero identity, equipment exclusion, loadout protection and ABI gates are verified.
JumpIfNot(Inst(salvageRunning), "NORMAL_MODE");
JumpIfNot(Math(greaterEqFn, Inst(batchIndex), Int(0)), "FINISH_BATCH");

JumpIfNot(Math(lessFn, Inst(batchIndex), ArrayLength(Inst(selectedSlots))), "INVALID_CONTEXT");
JumpIfNot(Math(lessFn, Inst(batchIndex), ArrayLength(Inst(selectedItems))), "INVALID_CONTEXT");
SetLocalObj(currentSlot, new EX_ArrayGetByRef { ArrayVariable = Inst(selectedSlots), ArrayIndex = Inst(batchIndex) });
JumpIfNot(IsValid(Local(currentSlot)), "BATCH_NEXT");
JumpIfNot(ArrayContains(Local(inventorySlots), Local(currentSlot)), "BATCH_NEXT");
SetLocalObj(currentItem, SlotItem(Local(currentSlot)));
JumpIfNot(Math(equalObjectFn, Local(currentItem), new EX_ArrayGetByRef {
    ArrayVariable = Inst(selectedItems), ArrayIndex = Inst(batchIndex) }), "BATCH_NEXT");
JumpIfNot(Ctx(Local(currentSlot), Virtual("IsLocked")), "BATCH_UNLOCKED");
Jump("BATCH_NEXT");
Label("BATCH_UNLOCKED");
JumpIfNot(IsValid(Local(currentItem)), "BATCH_NEXT");
RejectEquipped("BATCH_NEXT", "BATCH_EQUIP");
ReadFingerprint();
JumpIfNot(ArrayContains(Records(), Local(currentKey)), "BATCH_CAN_SALVAGE");
Jump("BATCH_NEXT");

Label("BATCH_CAN_SALVAGE");
JumpIfNot(Ctx(Local(currentItem), Virtual("CanSalvage")), "BATCH_NEXT");
Add(Print(Str("MCD QoL | preview candidate only; NO item salvaged")));

Label("BATCH_NEXT");
SetInt(batchIndex, Math(subIntFn, Inst(batchIndex), Int(1)));
Jump("END");

Label("FINISH_BATCH");
ClearSelection();
Add(Print(Str("MCD QoL | preview finished; NO items salvaged")));
Jump("END");

Label("NORMAL_MODE");
// Do not touch inventory arrays until the native stash exists and contains at least one slot.
JumpIfNot(IsValid(Local(stashLocal)), "END");
JumpIfNot(Math(greaterEqFn, InventorySize(), Int(1)), "END");

// Keep cursor in range if the game changed inventory size.
JumpIfNot(Math(greaterEqFn, Inst(cursor), InventorySize()), "CURSOR_OK");
SetInt(cursor, Int(0));
Label("CURSOR_OK");

// F7 next, cancels destructive confirmation.
JumpIfNot(KeyPressed("F7"), "PREV");
SetBool(confirmArmed, False());
SetInt(cursor, Math(addIntFn, Inst(cursor), Int(1)));
JumpIfNot(Math(greaterEqFn, Inst(cursor), InventorySize()), "NEXT_PRINT");
SetInt(cursor, Int(0));
Label("NEXT_PRINT");
Add(Print(SlotText()));

Label("PREV");
JumpIfNot(KeyPressed("F6"), "RESOLVE_CURRENT");
SetBool(confirmArmed, False());
SetInt(cursor, Math(subIntFn, Inst(cursor), Int(1)));
JumpIfNot(Math(lessFn, Inst(cursor), Int(0)), "PREV_PRINT");
SetInt(cursor, Math(subIntFn, InventorySize(), Int(1)));
Label("PREV_PRINT");
Add(Print(SlotText()));

Label("RESOLVE_CURRENT");
// Resolve native slot/item for lock/select actions.
SetLocalObj(currentSlot, CurrentSlotExpr());
JumpIfNot(IsValid(Local(currentSlot)), "END");
SetLocalObj(currentItem, SlotItem(Local(currentSlot)));

// F8 persistent lock toggle.
JumpIfNot(KeyPressed("F8"), "SELECT");
SetBool(confirmArmed, False());
JumpIfNot(IsValid(Local(currentItem)), "LOCK_EMPTY");
ReadFingerprint();
JumpIfNot(ArrayContains(Records(), Local(currentKey)), "LOCK_ADD");
Add(Static(arrayDefault, arrayRemoveFn, Records(), Local(currentKey)));
SaveAndReport("MCD QoL | fingerprint protection removed (all matching items)");
Jump("SELECT");
Label("LOCK_ADD");
Add(Static(arrayDefault, arrayAddFn, Records(), Local(currentKey)));
SaveAndReport("MCD QoL | fingerprint protected (all matching items; prototype)");
Jump("SELECT");
Label("LOCK_EMPTY");
Add(Print(Str("MCD QoL | empty slot, nothing to lock")));

Label("SELECT");
// F9 in-session multi-select. Locked and unsalvageable items cannot enter the batch.
JumpIfNot(KeyPressed("F9"), "CONFIRM");
SetBool(confirmArmed, False());
JumpIfNot(IsValid(Local(currentItem)), "SELECT_EMPTY");
JumpIfNot(Ctx(Local(currentSlot), Virtual("IsLocked")), "SELECT_SLOT_UNLOCKED");
Jump("SELECT_BLOCKED");
Label("SELECT_SLOT_UNLOCKED");
RejectEquipped("SELECT_BLOCKED", "SELECT_EQUIP");
ReadFingerprint();
JumpIfNot(ArrayContains(Records(), Local(currentKey)), "SELECT_NOT_LOCKED");
Add(Print(Str("MCD QoL | locked item cannot be selected")));
Jump("CONFIRM");
Label("SELECT_NOT_LOCKED");
JumpIfNot(Ctx(Local(currentItem), Virtual("CanSalvage")), "SELECT_BLOCKED");
JumpIfNot(ArrayContains(Inst(selectedSlots), Local(currentSlot)), "SELECT_ADD");
// Remove the paired item before removing the slot so indexes stay aligned.
Add(Static(arrayDefault, arrayRemoveIndexFn, Inst(selectedItems),
    Static(arrayDefault, arrayFindFn, Inst(selectedSlots), Local(currentSlot))));
Add(Static(arrayDefault, arrayRemoveFn, Inst(selectedSlots), Local(currentSlot)));
Add(Print(Str("MCD QoL | item removed from salvage batch")));
Jump("CONFIRM");
Label("SELECT_ADD");
Add(Static(arrayDefault, arrayAddFn, Inst(selectedSlots), Local(currentSlot)));
Add(Static(arrayDefault, arrayAddFn, Inst(selectedItems), Local(currentItem)));
Add(Print(Str("MCD QoL | item added to salvage batch")));
Jump("CONFIRM");
Label("SELECT_BLOCKED");
Add(Print(Str("MCD QoL | equipped, unresolved equipment state, or game blocks salvage")));
Jump("CONFIRM");
Label("SELECT_EMPTY");
Add(Print(Str("MCD QoL | empty slot, nothing to select")));

Label("CONFIRM");
// F10 requires two presses. The second starts one-candidate-per-tick preview.
JumpIfNot(KeyPressed("F10"), "END");
JumpIfNot(Math(greaterEqFn, ArrayLength(Inst(selectedSlots)), Int(1)), "NO_SELECTION");
JumpIfNot(Inst(confirmArmed), "ARM_BATCH");
SetInt(batchIndex, Math(subIntFn, ArrayLength(Inst(selectedSlots)), Int(1)));
SetBool(salvageRunning, True());
SetBool(confirmArmed, False());
Add(Print(Str("MCD QoL | preview started; destruction disabled")));
Jump("END");

Label("ARM_BATCH");
SetBool(confirmArmed, True());
Add(Print(Str("MCD QoL | press F10 again to PREVIEW (no destruction)")));
Jump("END");

Label("NO_SELECTION");
SetBool(confirmArmed, False());
Add(Print(Str("MCD QoL | no items selected")));

Jump("END");
Label("SAVE_FAILED");
ClearSelection();
Add(Print(Str("MCD QoL | sidecar save unavailable; protection cannot be trusted")));
Jump("END");
Label("INVALID_CONTEXT");
ClearSelection();
Label("END");
Add(new EX_Return { ReturnExpression = new EX_Nothing() });
Add(new EX_EndOfScript());

// Resolve absolute iCode jump targets.
var starts = new uint[code.Count + 1];
uint total = 0;
for (var i = 0; i < code.Count; i++)
{
    starts[i] = total;
    total += (uint)ICode(code[i]);
}
starts[code.Count] = total;
foreach (var (jump,target) in jumps)
{
    if (!labels.TryGetValue(target, out var idx))
        throw new InvalidOperationException($"Unknown assembler label: {target}");
    var targetOffset = starts[idx];
    if (jump is EX_JumpIfNot jn) jn.CodeOffset = targetOffset;
    else if (jump is EX_Jump j) j.CodeOffset = targetOffset;
}

uber.ScriptBytecode = code.ToArray();
uber.ScriptBytecodeRaw = Array.Empty<byte>();
uber.ScriptBytecodeSize = (int)total;

DiagnosticGraphValidator.ConfigureInventoryTick(asset);
CookedDependencyGraph.Repair(asset);
asset.Write(output);

// ---------- structural re-open validation ----------

var reopened = new UAsset(output, EngineVersion.VER_UE4_22);
KismetSerializer.asset = reopened;
CookedDependencyGraph.Validate(reopened);
var outClass = reopened.Exports.OfType<ClassExport>().Single(x => x.ObjectName.ToString() == "BP_WASD_Movement_C");
foreach (var required in new[] { "CursorIndex", "ConfirmArmed", "SalvageRunning", "BatchIndex", "SaveState", "SelectedSlots", "SelectedItems", "SelectionOwner" })
{
    if (!outClass.Children.Any(x => x.IsExport() && x.ToExport(reopened).ObjectName.ToString() == required))
        throw new InvalidDataException($"Missing class field after re-open: {required}");
}

var outUber = reopened.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "ExecuteUbergraph_BP_WASD_Movement");
if (outUber.ScriptBytecode is not { Length: > 20 })
    throw new InvalidDataException("QoL Kismet graph did not survive serialization.");

var keys = new HashSet<string>();
var virtualCalls = new HashSet<string>();
foreach (var root in outUber.ScriptBytecode)
{
    uint o = 0;
    root.Visit(reopened, ref o, (expr, _) =>
    {
        if (expr is EX_NameConst nc && nc.Value != null) keys.Add(nc.Value.ToString());
        if (expr is EX_VirtualFunction vf && vf.VirtualFunctionName != null) virtualCalls.Add(vf.VirtualFunctionName.ToString());
    });
}
foreach (var key in new[] { "F5", "F6", "F7", "F8", "F9", "F10" })
    if (!keys.Contains(key)) throw new InvalidDataException($"Missing input hotkey after re-open: {key}");

foreach (var call in new[] { "IsLocked", "CanSalvage" })
    if (!virtualCalls.Contains(call)) throw new InvalidDataException($"Missing native runtime call after re-open: {call}");

var validatedSize = DiagnosticGraphValidator.Validate(reopened, outUber.ScriptBytecode);
if (validatedSize != total) throw new InvalidDataException("Re-opened iCode size differs from assembly.");
Console.WriteLine("[OK] Diagnostic protection/select/preview graph survived write/re-open; no destruction.");
Console.WriteLine($"     iCode={total}, statements={code.Count}, imports={reopened.Imports.Count}, exports={reopened.Exports.Count}");
return 0;

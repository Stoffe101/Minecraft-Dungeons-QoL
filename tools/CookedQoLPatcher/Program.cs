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

Import FindPackage(string name) =>
    asset.Imports.First(i => i.ClassName.ToString() == "Package" && i.ObjectName.ToString() == name);

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

FPackageIndex FindClassPropertyImport(string objectName)
{
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.ClassName.ToString() == "Class" && imp.ObjectName.ToString() == objectName)
            return FPackageIndex.FromImport(i);
    }
    return EnsureClass("/Script/CoreUObject", objectName);
}

FPackageIndex FindFunction(string name)
{
    for (var i = 0; i < asset.Imports.Count; i++)
        if (asset.Imports[i].ClassName.ToString() == "Function" && asset.Imports[i].ObjectName.ToString() == name)
            return FPackageIndex.FromImport(i);
    throw new InvalidOperationException($"Function import not found: {name}");
}

var generatedClass = asset.Exports.OfType<ClassExport>().Single(x => x.ObjectName.ToString() == "BP_WASD_Movement_C");
var uber = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "ExecuteUbergraph_BP_WASD_Movement");
var playerControllerLocal = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "CallFunc_GetPlayerController_ReturnValue");
var pawnLocal = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "CallFunc_K2_GetPawn_ReturnValue");
var stashLocal = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "K2Node_DynamicCast_AsCharacter");

var dungeonsPkg = EnsurePackage("/Script/Dungeons");
var itemStashClass = EnsureClass("/Script/Dungeons", "ItemStashComponent");

// Retarget an existing object local to the stash return value.
stashLocal.ObjectName = new FName(asset, "CallFunc_GetComponentByClass_ReturnValue");
if (stashLocal.Property is not UObjectProperty stashProp)
    throw new InvalidDataException("Expected object property donor for stash local.");
stashProp.PropertyClass = itemStashClass;

// Add a real class-owned cursor property.
var intPropertyClass = FindClassPropertyImport("IntProperty");
var intDonor = asset.Exports.OfType<PropertyExport>().First(x => x.Property is UIntProperty);
var cursor = (PropertyExport)intDonor.Clone();
cursor.ObjectName = new FName(asset, "CursorIndex");
cursor.OuterIndex = FPackageIndex.FromExport(asset.Exports.IndexOf(generatedClass));
cursor.ClassIndex = intPropertyClass;
cursor.SuperIndex = new FPackageIndex(0);
cursor.TemplateIndex = new FPackageIndex(0);
cursor.SerialOffset = 0;
cursor.SerialSize = 0;
cursor.SerializationBeforeSerializationDependencies.Clear();
cursor.CreateBeforeSerializationDependencies.Clear();
cursor.SerializationBeforeCreateDependencies.Clear();
cursor.CreateBeforeCreateDependencies.Clear();
cursor.Property = new UIntProperty
{
    ArrayDim = intDonor.Property.ArrayDim,
    ElementSize = 0,
    PropertyFlags = EPropertyFlags.CPF_BlueprintVisible,
    RepNotifyFunc = new FName(asset, "None"),
    BlueprintReplicationCondition = ELifetimeCondition.COND_None,
    Next = new FPackageIndex(0)
};
asset.Exports.Add(cursor);
var cursorIndex = FPackageIndex.FromExport(asset.Exports.IndexOf(cursor));
generatedClass.Children = generatedClass.Children.Concat(new[] { cursorIndex }).ToArray();

// Input function has the same FKey -> bool signature as IsInputKeyDown.
var inputFn = FindFunction("IsInputKeyDown");
inputFn.ToImport(asset).ObjectName = new FName(asset, "WasInputKeyJustPressed");

// Existing engine helpers.
var gameplayDefault = asset.Imports.Select((x,i)=>(x,i)).First(x => x.x.ObjectName.ToString() == "Default__GameplayStatics").i;
var gameplayDefaultIndex = FPackageIndex.FromImport(gameplayDefault);
var getPlayerControllerFn = FindFunction("GetPlayerController");
var getPawnFn = FindFunction("K2_GetPawn");
var keyStruct = asset.Imports.Select((x,i)=>(x,i)).First(x => x.x.ObjectName.ToString() == "Key" && x.x.ClassName.ToString() == "ScriptStruct").i;
var keyStructIndex = FPackageIndex.FromImport(keyStruct);

// Math helpers.
var mathClass = EnsureClass("/Script/Engine", "KismetMathLibrary");
var addIntFn = EnsureFunction(mathClass, "Add_IntInt");
var subIntFn = EnsureFunction(mathClass, "Subtract_IntInt");
var greaterEqFn = EnsureFunction(mathClass, "GreaterEqual_IntInt");
var lessFn = EnsureFunction(mathClass, "Less_IntInt");

// Print helper.
var systemClass = EnsureClass("/Script/Engine", "KismetSystemLibrary");
var systemDefault = EnsureDefault("/Script/Engine", "KismetSystemLibrary");
var printStringFn = EnsureFunction(systemClass, "PrintString");
var stringClass = EnsureClass("/Script/Engine", "KismetStringLibrary");
var buildStringIntFn = EnsureFunction(stringClass, "BuildString_Int");
var linearColorStruct = EnsureScriptStruct("/Script/CoreUObject", "LinearColor");

KismetPropertyPointer Ptr(FPackageIndex i) => new(i);
EX_LocalVariable Local(PropertyExport p) => new() { Variable = Ptr(FPackageIndex.FromExport(asset.Exports.IndexOf(p))) };
EX_InstanceVariable Inst(PropertyExport p) => new() { Variable = Ptr(FPackageIndex.FromExport(asset.Exports.IndexOf(p))) };
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

EX_Context Ctx(KismetExpression obj, KismetExpression expression, KismetPropertyPointer? result = null)
{
    return new EX_Context
    {
        ObjectExpression = obj,
        ContextExpression = expression,
        Offset = (uint)ICode(expression),
        PropertyType = 0,
        RValuePointer = result ?? Ptr(new FPackageIndex(0))
    };
}

EX_FinalFunction Final(FPackageIndex fn, params KismetExpression[] pars) => new() { StackNode = fn, Parameters = pars };
EX_VirtualFunction Virtual(string name, params KismetExpression[] pars) => new() { VirtualFunctionName = new FName(asset, name), Parameters = pars };
EX_CallMath Math(FPackageIndex fn, params KismetExpression[] pars) => new() { StackNode = fn, Parameters = pars };

EX_StructConst Key(string key) => new()
{
    Struct = keyStructIndex,
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

var cursorVar = Inst(cursor);
KismetExpression InventorySize() => Ctx(Local(stashLocal), Virtual("InventorySize"));
KismetExpression KeyPressed(string key) => Ctx(Local(playerControllerLocal), Final(inputFn, Key(key)));

KismetExpression SlotText() => Ctx(
    Obj(EnsureDefault("/Script/Engine", "KismetStringLibrary")),
    Final(buildStringIntFn,
        Str("MCD QoL | inventory slot "),
        Str(""),
        Inst(cursor),
        Str(" | F6 prev, F7 next")
    )
);

KismetExpression Print(KismetExpression text) => Ctx(
    Obj(systemDefault),
    Final(printStringFn,
        Self(),
        text,
        True(),
        False(),
        Yellow(),
        new EX_FloatConst { Value = 1.75f }
    )
);

var code = new List<KismetExpression>();
var labels = new Dictionary<string,int>(StringComparer.Ordinal);
var jumps = new List<(KismetExpression jump,string target)>();

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

// Resolve local player and stash every tick. Context calls fail harmlessly when their object is null.
Add(new EX_LetObj
{
    VariableExpression = Local(playerControllerLocal),
    AssignmentExpression = Ctx(Obj(gameplayDefaultIndex), Final(getPlayerControllerFn, Self(), Int(0)), Ptr(FPackageIndex.FromExport(asset.Exports.IndexOf(playerControllerLocal))))
});
Add(new EX_LetObj
{
    VariableExpression = Local(pawnLocal),
    AssignmentExpression = Ctx(Local(playerControllerLocal), Final(getPawnFn), Ptr(FPackageIndex.FromExport(asset.Exports.IndexOf(pawnLocal))))
});
Add(new EX_LetObj
{
    VariableExpression = Local(stashLocal),
    AssignmentExpression = Ctx(Local(pawnLocal), Virtual("GetComponentByClass", Obj(itemStashClass)), Ptr(FPackageIndex.FromExport(asset.Exports.IndexOf(stashLocal))))
});

// No inventory means no array access.
JumpIfNot(Math(greaterEqFn, InventorySize(), Int(1)), "END");

// F7: next.
JumpIfNot(KeyPressed("F7"), "PREV");
Add(new EX_Let
{
    Value = Ptr(cursorIndex),
    Variable = cursorVar,
    Expression = Math(addIntFn, Inst(cursor), Int(1))
});
JumpIfNot(Math(greaterEqFn, Inst(cursor), InventorySize()), "NEXT_PRINT");
Add(new EX_Let { Value = Ptr(cursorIndex), Variable = Inst(cursor), Expression = Int(0) });
Label("NEXT_PRINT");
Add(Print(SlotText()));

Label("PREV");
JumpIfNot(KeyPressed("F6"), "END");
Add(new EX_Let
{
    Value = Ptr(cursorIndex),
    Variable = Inst(cursor),
    Expression = Math(subIntFn, Inst(cursor), Int(1))
});
JumpIfNot(Math(lessFn, Inst(cursor), Int(0)), "PREV_PRINT");
Add(new EX_Let
{
    Value = Ptr(cursorIndex),
    Variable = Inst(cursor),
    Expression = Math(subIntFn, InventorySize(), Int(1))
});
Label("PREV_PRINT");
Add(Print(SlotText()));

Label("END");
Add(new EX_Return { ReturnExpression = new EX_Nothing() });
Add(new EX_EndOfScript());

// Resolve absolute iCode jump offsets.
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
        throw new InvalidOperationException($"Unknown label {target}");
    var targetOffset = starts[idx];
    switch (jump)
    {
        case EX_JumpIfNot jn: jn.CodeOffset = targetOffset; break;
        case EX_Jump j: j.CodeOffset = targetOffset; break;
    }
}

uber.ScriptBytecode = code.ToArray();
uber.ScriptBytecodeRaw = Array.Empty<byte>();
uber.ScriptBytecodeSize = (int)total;

asset.Write(output);

// Re-open and make sure the custom reflected field + key calls survived.
var reopened = new UAsset(output, EngineVersion.VER_UE4_22);
KismetSerializer.asset = reopened;
var outClass = reopened.Exports.OfType<ClassExport>().Single(x => x.ObjectName.ToString() == "BP_WASD_Movement_C");
if (!outClass.Children.Any(x => x.IsExport() && x.ToExport(reopened).ObjectName.ToString() == "CursorIndex"))
    throw new InvalidDataException("CursorIndex class property did not survive.");

var outUber = reopened.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString() == "ExecuteUbergraph_BP_WASD_Movement");
if (outUber.ScriptBytecode is not { Length: > 5 })
    throw new InvalidDataException("Custom QoL bytecode did not survive re-open.");

var keys = new HashSet<string>();
foreach (var root in outUber.ScriptBytecode)
{
    uint o = 0;
    root.Visit(reopened, ref o, (expr, _) =>
    {
        if (expr is EX_NameConst nc && nc.Value != null) keys.Add(nc.Value.ToString());
    });
}
if (!keys.Contains("F6") || !keys.Contains("F7"))
    throw new InvalidDataException("Navigation hotkeys missing after re-open.");

Console.WriteLine("[OK] Cooked QoL runtime navigation patch survived write/re-open.");
Console.WriteLine($"     iCode={total}, statements={code.Count}, imports={reopened.Imports.Count}, exports={reopened.Exports.Count}");
return 0;

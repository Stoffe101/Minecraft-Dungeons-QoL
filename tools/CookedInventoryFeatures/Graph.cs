using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

sealed class Graph
{
    public readonly UAsset Asset;
    public readonly ClassExport Owner;
    public readonly FunctionExport Function;
    public readonly List<KismetExpression> Code = new();
    readonly Dictionary<string, int> labels = new();
    readonly List<(KismetExpression expression, string label)> jumps = new();
    int temporary;
    public Graph(UAsset asset, string functionName)
    {
        Asset = asset; Owner = asset.Exports.OfType<ClassExport>().Single();
        if (asset.Exports.Any(x => x.ObjectName.ToString() == functionName)) throw new InvalidDataException("Already patched: " + functionName);
        Function = (FunctionExport)asset.Exports.OfType<FunctionExport>().First(x => x.Children.Length == 0).Clone();
        Function.ObjectName = new FName(asset, functionName); Function.OuterIndex = Index(Owner);
        Function.SuperIndex = Function.SuperStruct = new FPackageIndex(0);
        Function.Children = System.Array.Empty<FPackageIndex>();
        Function.FunctionFlags = EFunctionFlags.FUNC_Public | EFunctionFlags.FUNC_BlueprintCallable | EFunctionFlags.FUNC_BlueprintEvent;
        ClearDependencies(Function); asset.Exports.Add(Function);
        Owner.Children = Owner.Children.Append(Index(Function)).ToArray(); Owner.FuncMap.Add(Function.ObjectName, Index(Function));
    }
    public FPackageIndex Index(Export e) => FPackageIndex.FromExport(Asset.Exports.IndexOf(e));
    public FPackageIndex Import(string package, string type, string name, FPackageIndex outer)
    {
        var i = Asset.Imports.FindIndex(x => x.ClassPackage.ToString() == package && x.ClassName.ToString() == type && x.ObjectName.ToString() == name && x.OuterIndex.Index == outer.Index);
        return i >= 0 ? FPackageIndex.FromImport(i) : Asset.AddImport(new Import(package, type, outer, name, false, Asset));
    }
    public FPackageIndex Package(string name) => Import("/Script/CoreUObject", "Package", name, new FPackageIndex(0));
    public FPackageIndex Class(string package, string name) => Import("/Script/CoreUObject", "Class", name, Package(package));
    public FPackageIndex Fn(FPackageIndex cls, string name) => Import("/Script/CoreUObject", "Function", name, cls);
    public FPackageIndex Member(FPackageIndex cls, string type, string name) => Import("/Script/CoreUObject", type, name, cls);
    public FPackageIndex Existing(string name, string? type = null) => FPackageIndex.FromImport(Asset.Imports.FindIndex(x => x.ObjectName.ToString() == name && (type == null || x.ClassName.ToString() == type)) is var i && i >= 0 ? i : throw new InvalidDataException("Missing observed import: " + name));
    public PropertyExport Field(string name, string? outer = null) => Asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == name && (outer == null ? x.OuterIndex.Index == Index(Owner).Index : x.OuterIndex.ToExport(Asset).ObjectName.ToString() == outer));
    public PropertyExport Property(string name, UProperty property, string propertyClass, bool instance = false, EPropertyFlags flags = EPropertyFlags.CPF_None)
    {
        var p = (PropertyExport)Asset.Exports.OfType<PropertyExport>().First(x => x.Property is UObjectProperty).Clone();
        p.ObjectName = new FName(Asset, name); p.OuterIndex = Index(instance ? Owner : Function);
        p.ClassIndex = Class("/Script/CoreUObject", propertyClass); p.SuperIndex = p.TemplateIndex = new FPackageIndex(0);
        p.SerialOffset = p.SerialSize = 0; ClearDependencies(p);
        property.ArrayDim = p.Property.ArrayDim; property.PropertyFlags = flags; property.RepNotifyFunc = new FName(Asset, "None"); property.Next = new FPackageIndex(0);
        p.Property = property; Asset.Exports.Add(p);
        if (!instance && flags.HasFlag(EPropertyFlags.CPF_OutParm)) Function.FunctionFlags |= EFunctionFlags.FUNC_HasOutParms;
        var parent = (StructExport)(instance ? Owner : Function); parent.Children = parent.Children.Append(Index(p)).ToArray(); return p;
    }
    public PropertyExport Object(string name, FPackageIndex cls, bool instance = false, EPropertyFlags flags = EPropertyFlags.CPF_None) => Property(name, new UObjectProperty { PropertyClass = cls }, "ObjectProperty", instance, flags);
    public PropertyExport Integer(string name, bool instance = false) => Property(name, new UIntProperty(), "IntProperty", instance);
    public PropertyExport Boolean(string name, bool instance = false, EPropertyFlags flags = EPropertyFlags.CPF_None) => Property(name, new UBoolProperty { ElementSize = 1, NativeBool = ((UBoolProperty)Asset.Exports.OfType<PropertyExport>().First(x => x.Property is UBoolProperty).Property).NativeBool }, "BoolProperty", instance, flags);
    public PropertyExport String(string name, bool instance = false) => Property(name, new UStrProperty(), "StrProperty", instance);
    public PropertyExport ObjectArray(string name, FPackageIndex cls, bool instance = false)
    {
        var p = Property(name, new UArrayProperty(), "ArrayProperty", instance);
        var inner = Object(name + "_Inner", cls); inner.OuterIndex = Index(p);
        Function.Children = Function.Children.Where(x => x.Index != Index(inner).Index).ToArray();
        ((UArrayProperty)p.Property).Inner = Index(inner); return p;
    }
    public static void ClearDependencies(Export e)
    {
        e.SerializationBeforeSerializationDependencies.Clear(); e.SerializationBeforeCreateDependencies.Clear();
        e.CreateBeforeSerializationDependencies.Clear(); e.CreateBeforeCreateDependencies.Clear();
    }
    public KismetPropertyPointer Ptr(FPackageIndex p) => new(p);
    public KismetExpression L(PropertyExport p) => p.Property.PropertyFlags.HasFlag(EPropertyFlags.CPF_OutParm)
        ? new EX_LocalOutVariable { Variable = Ptr(Index(p)) } : new EX_LocalVariable { Variable = Ptr(Index(p)) };
    public KismetExpression I(PropertyExport p) => new EX_InstanceVariable { Variable = Ptr(Index(p)) };
    public KismetExpression V(FPackageIndex p) => new EX_InstanceVariable { Variable = Ptr(p) };
    public KismetExpression O(FPackageIndex p) => new EX_ObjectConst { Value = p };
    public KismetExpression N(int n) => new EX_IntConst { Value = n };
    public KismetExpression S(string s) => new EX_StringConst { Value = s };
    public KismetExpression F(FPackageIndex f, params KismetExpression[] p) => new EX_FinalFunction { StackNode = f, Parameters = p };
    public KismetExpression Local(FunctionExport f, params KismetExpression[] p) => new EX_LocalFinalFunction { StackNode = Index(f), Parameters = p };
    public KismetExpression C(KismetExpression obj, KismetExpression expression, FPackageIndex? result = null) => new EX_Context { ObjectExpression = obj, ContextExpression = expression, Offset = (uint)Size(expression), RValuePointer = Ptr(result ?? new FPackageIndex(0)) };
    public KismetExpression Static(string cls, string fn, params KismetExpression[] p) => C(O(Import("/Script/Engine", cls, "Default__" + cls, Package("/Script/Engine"))), F(Fn(Class("/Script/Engine", cls), fn), p));
    public KismetExpression Math(string fn, params KismetExpression[] p) => new EX_CallMath { StackNode = Fn(Class("/Script/Engine", "KismetMathLibrary"), fn), Parameters = p };
    public KismetExpression Valid(KismetExpression obj) => Static("KismetSystemLibrary", "IsValid", obj);
    public KismetExpression Array(string fn, params KismetExpression[] p) => Static("KismetArrayLibrary", fn, p);
    public KismetExpression At(KismetExpression array, KismetExpression index) => new EX_ArrayGetByRef { ArrayVariable = array, ArrayIndex = index };
    public KismetExpression Not(KismetExpression e) => Math("Not_PreBool", e);
    public void Add(KismetExpression e) => Code.Add(e);
    public void Label(string name) => labels.Add(name, Code.Count);
    public void Branch(KismetExpression condition, string label) { var j = new EX_JumpIfNot { BooleanExpression = condition }; Add(j); jumps.Add((j, label)); }
    public void Jump(string label) { var j = new EX_Jump(); Add(j); jumps.Add((j, label)); }
    public void Obj(PropertyExport p, KismetExpression value, bool instance = false) => Add(new EX_LetObj { VariableExpression = instance ? I(p) : L(p), AssignmentExpression = value });
    public void Set(PropertyExport p, KismetExpression value, bool instance = false)
    {
        if (value is EX_Context context) context.RValuePointer = Ptr(Index(p));
        if (p.Property is UBoolProperty) Add(new EX_LetBool { VariableExpression = instance ? I(p) : L(p), AssignmentExpression = value });
        else Add(new EX_Let { Value = Ptr(Index(p)), Variable = instance ? I(p) : L(p), Expression = value });
    }
    public void Bool(PropertyExport p, bool value, bool instance = false) => Add(new EX_LetBool { VariableExpression = instance ? I(p) : L(p), AssignmentExpression = value ? new EX_True() : new EX_False() });
    public KismetExpression TextValue(KismetExpression value, bool text = false)
    {
        var p = text ? Property("MCDQoL_Text_" + temporary++, new UTextProperty(), "TextProperty") : String("MCDQoL_String_" + temporary++);
        Set(p, value); return L(p);
    }
    public KismetExpression Concat(KismetExpression a, KismetExpression b) => TextValue(Static("KismetStringLibrary", "Concat_StrStr", a, b));
    public KismetExpression Count(KismetExpression prefix, KismetExpression count, string suffix = "") => TextValue(Static("KismetStringLibrary", "BuildString_Int", prefix, S(""), count, S(suffix)));
    public int Size(KismetExpression expression) { using var m = new MemoryStream(); using var w = new AssetBinaryWriter(m, Asset); return ExpressionSerializer.WriteExpression(expression, w); }
    public void Finish(KismetExpression? result = null)
    {
        Add(new EX_Return { ReturnExpression = result ?? new EX_Nothing() }); Add(new EX_EndOfScript());
        uint offset = 0; var offsets = Code.Select(e => { var current = offset; offset += (uint)Size(e); return current; }).ToArray();
        foreach (var (j, label) in jumps) { var target = offsets[labels[label]]; if (j is EX_Jump jump) jump.CodeOffset = target; else ((EX_JumpIfNot)j).CodeOffset = target; }
        Function.ScriptBytecode = Code.ToArray(); Function.ScriptBytecodeRaw = null; Function.ScriptBytecodeSize = (int)offset;
        DiagnosticGraphValidator.ValidateReferenceArguments(Asset, Code);
    }
}

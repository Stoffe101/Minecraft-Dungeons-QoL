using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: CookedSaveGameSynth <source-actor.uasset> <output-savegame.uasset>");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);

var asset = new UAsset(input, EngineVersion.VER_UE4_22);

Import FindPackage(string name) =>
    asset.Imports.First(i => i.ClassName.ToString() == "Package" && i.ObjectName.ToString() == name);

FPackageIndex FindOrAddClassImport(string packageName, string className)
{
    var package = FindPackage(packageName);
    var packageIndex = FPackageIndex.FromImport(asset.Imports.IndexOf(package));
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == packageIndex.Index &&
            imp.ClassName.ToString() == "Class" &&
            imp.ObjectName.ToString() == className)
            return FPackageIndex.FromImport(i);
    }

    return asset.AddImport(new Import("/Script/CoreUObject", "Class", packageIndex, className, false, asset));
}

FPackageIndex FindOrAddDefaultObjectImport(string packageName, string className)
{
    var package = FindPackage(packageName);
    var packageIndex = FPackageIndex.FromImport(asset.Imports.IndexOf(package));
    var objectName = "Default__" + className;
    for (var i = 0; i < asset.Imports.Count; i++)
    {
        var imp = asset.Imports[i];
        if (imp.OuterIndex?.Index == packageIndex.Index &&
            imp.ClassName.ToString() == className &&
            imp.ObjectName.ToString() == objectName)
            return FPackageIndex.FromImport(i);
    }

    return asset.AddImport(new Import(packageName, className, packageIndex, objectName, false, asset));
}

var classExport = asset.Exports.OfType<ClassExport>().Single(x => x.ObjectName.ToString() == "BP_WASD_Movement_C");
var cdo = asset.Exports.OfType<NormalExport>().Single(x => x.ObjectName.ToString() == "Default__BP_WASD_Movement_C");
var donor = asset.Exports.OfType<PropertyExport>().Single(x => x.ObjectName.ToString() == "DefaultSceneRoot");

var saveGameClass = FindOrAddClassImport("/Script/Engine", "SaveGame");
var saveGameDefault = FindOrAddDefaultObjectImport("/Script/Engine", "SaveGame");
var arrayPropertyClass = FindOrAddClassImport("/Script/CoreUObject", "ArrayProperty");
var namePropertyClass = FindOrAddClassImport("/Script/CoreUObject", "NameProperty");

// Reuse the actor's class-owned DefaultSceneRoot export as our one persisted array property.
donor.ObjectName = new FName(asset, "Records");
donor.ClassIndex = arrayPropertyClass;
donor.SuperIndex = new FPackageIndex(0);
donor.TemplateIndex = new FPackageIndex(0);
donor.OuterIndex = FPackageIndex.FromExport(asset.Exports.IndexOf(classExport));
donor.ObjectFlags |= EObjectFlags.RF_Public;

var recordsProperty = new UArrayProperty
{
    ArrayDim = donor.Property.ArrayDim,
    ElementSize = 0,
    PropertyFlags = EPropertyFlags.CPF_Edit | EPropertyFlags.CPF_BlueprintVisible | EPropertyFlags.CPF_SaveGame,
    RepNotifyFunc = new FName(asset, "None"),
    BlueprintReplicationCondition = ELifetimeCondition.COND_None,
    Next = new FPackageIndex(0)
};
donor.Property = recordsProperty;

// Create the NameProperty used as the TArray inner field.
var inner = (PropertyExport)donor.Clone();
inner.ObjectName = new FName(asset, "Records_Inner");
inner.ClassIndex = namePropertyClass;
inner.SuperIndex = new FPackageIndex(0);
inner.TemplateIndex = new FPackageIndex(0);
inner.OuterIndex = FPackageIndex.FromExport(asset.Exports.IndexOf(donor));
inner.SerialOffset = 0;
inner.SerialSize = 0;
inner.SerializationBeforeSerializationDependencies.Clear();
inner.CreateBeforeSerializationDependencies.Clear();
inner.SerializationBeforeCreateDependencies.Clear();
inner.CreateBeforeCreateDependencies.Clear();
inner.Property = new UNameProperty
{
    ArrayDim = donor.Property.ArrayDim,
    ElementSize = 0,
    PropertyFlags = EPropertyFlags.CPF_None,
    RepNotifyFunc = new FName(asset, "None"),
    BlueprintReplicationCondition = ELifetimeCondition.COND_None,
    Next = new FPackageIndex(0)
};
asset.Exports.Add(inner);
recordsProperty.Inner = FPackageIndex.FromExport(asset.Exports.IndexOf(inner));

// Convert the generated class from Actor to SaveGame and strip all actor functions/fields from its reflected surface.
classExport.ObjectName = new FName(asset, "SG_MCDQoL_C");
classExport.SuperIndex = saveGameClass;
classExport.SuperStruct = saveGameClass;
classExport.Children = new[] { FPackageIndex.FromExport(asset.Exports.IndexOf(donor)) };
classExport.LoadedProperties = Array.Empty<FProperty>();
classExport.ScriptBytecode = Array.Empty<KismetExpression>();
classExport.ScriptBytecodeRaw = Array.Empty<byte>();
classExport.ScriptBytecodeSize = 0;
classExport.FuncMap.Clear();

// Keep the existing generated-class flags, but it is no longer placeable by virtue of its SaveGame parent.
cdo.ObjectName = new FName(asset, "Default__SG_MCDQoL_C");
cdo.ClassIndex = FPackageIndex.FromExport(asset.Exports.IndexOf(classExport));
cdo.SuperIndex = new FPackageIndex(0);
cdo.TemplateIndex = saveGameDefault;
cdo.Data = new List<PropertyData>();

asset.Write(output);

// Re-open and validate the exact persisted surface.
var reopened = new UAsset(output, EngineVersion.VER_UE4_22);
var outClass = reopened.Exports.OfType<ClassExport>().Single(x => x.ObjectName.ToString() == "SG_MCDQoL_C");
if (!outClass.SuperStruct.IsImport() || outClass.SuperStruct.ToImport(reopened).ObjectName.ToString() != "SaveGame")
    throw new InvalidDataException("Generated class does not inherit SaveGame after re-open.");

if (outClass.Children.Length != 1)
    throw new InvalidDataException($"Expected one reflected child property, got {outClass.Children.Length}.");

var outRecords = outClass.Children[0].ToExport<PropertyExport>(reopened);
if (outRecords.ObjectName.ToString() != "Records" || outRecords.Property is not UArrayProperty outArray)
    throw new InvalidDataException("Records array property did not survive serialization.");

if (!outRecords.Property.PropertyFlags.HasFlag(EPropertyFlags.CPF_SaveGame))
    throw new InvalidDataException("Records is missing CPF_SaveGame.");

var outInner = outArray.Inner.ToExport<PropertyExport>(reopened);
if (outInner.Property is not UNameProperty)
    throw new InvalidDataException("Records inner field is not NameProperty.");

Console.WriteLine("[OK] Synthesized and re-opened cooked SaveGame class.");
Console.WriteLine($"     Class={outClass.ObjectName}, Parent={outClass.SuperStruct.ToImport(reopened).ObjectName}");
Console.WriteLine($"     Property={outRecords.ObjectName}, Flags={outRecords.Property.PropertyFlags}, Inner={outInner.Property.GetType().Name}");
return 0;

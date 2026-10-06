using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

// Call shapes observed in the retail character/profile evidence. These checks
// certify the generated caller's layout, not GUID persistence or native flags.
static class HeroProfileCallContracts
{
    public static readonly string[] ControllerMethods = {
        "GetRecentSaveDataIndex", "GetNumProfiles", "GetSaveLocalUserNum",
        "GetAvailableSaveDataByIndex", "GetCharacterSlotByIndex"
    };
    public static bool IsProfileMethod(string name) => name == "GetCloudPlayerId" || ControllerMethods.Contains(name);

    public static void Validate(UAsset asset)
    {
        foreach (var function in asset.Exports.OfType<FunctionExport>())
        {
            var calls = new HashSet<EX_FinalFunction>(ReferenceEqualityComparer.Instance);
            var typed = new HashSet<EX_FinalFunction>(ReferenceEqualityComparer.Instance);
            foreach (var root in function.ScriptBytecode ?? [])
            {
                uint offset = 0;
                root.Visit(asset, ref offset, (expression, _) => {
                    if (expression is EX_FinalFunction native && native.StackNode.IsImport()
                        && IsProfileMethod(native.StackNode.ToImport(asset).ObjectName.ToString())) calls.Add(native);
                    if (expression is EX_Context context && context.ContextExpression is EX_FinalFunction call
                        && call.StackNode.IsImport() && IsProfileMethod(call.StackNode.ToImport(asset).ObjectName.ToString()))
                    {
                        ValidateContext(asset, context, call);
                        typed.Add(call);
                    }
                });
            }
            if (calls.Except(typed).Any()) throw new InvalidDataException("Profile call requires a typed object context: " + function.ObjectName);
        }
    }

    public static void ValidateContext(UAsset asset, EX_Context context, EX_FinalFunction call)
    {
        var method = call.StackNode.ToImport(asset).ObjectName.ToString();
        if (!IsProfileMethod(method)) throw new InvalidDataException("Unknown profile call: " + method);
        var count = method == "GetCharacterSlotByIndex" ? 2 : method == "GetAvailableSaveDataByIndex" ? 1 : 0;
        if (call.Parameters.Length != count) throw new InvalidDataException("Wrong profile argument count: " + method);
        if (count > 0 && !ArgumentIs(asset, call.Parameters[0], typeof(UIntProperty), integer: true))
            throw new InvalidDataException("Profile index requires an int32 value: " + method);
        if (count > 1 && !ArgumentIs(asset, call.Parameters[1], typeof(UBoolProperty), integer: false))
            throw new InvalidDataException("Profile slot's second argument requires a bool: " + method);
        var result = context.RValuePointer.Old;
        if (!result.IsExport() || result.ToExport(asset) is not PropertyExport property)
            throw new InvalidDataException("Profile return requires a typed result field: " + method);
        var valid = method switch {
            "GetCloudPlayerId" => property.Property is UStructProperty guid && ImportPath(asset, guid.Struct) == "/Script/CoreUObject.Guid",
            "GetAvailableSaveDataByIndex" => property.Property is UObjectProperty save && ImportPath(asset, save.PropertyClass) == "/Script/Dungeons.CharacterSaveData",
            "GetCharacterSlotByIndex" => property.Property is UObjectProperty slot && ImportPath(asset, slot.PropertyClass) == "/Script/Dungeons.PlayerCharacterSaveSlot",
            _ => property.Property is UIntProperty
        };
        if (!valid) throw new InvalidDataException("Wrong profile return type: " + method);
    }

    static bool ArgumentIs(UAsset asset, UAssetAPI.Kismet.Bytecode.KismetExpression expression, Type type, bool integer)
    {
        if (integer && expression is EX_IntConst or EX_IntZero or EX_IntOne) return true;
        if (!integer && expression is EX_True or EX_False) return true;
        var index = expression switch {
            EX_LocalVariable local => local.Variable.Old,
            EX_LocalOutVariable local => local.Variable.Old,
            EX_InstanceVariable instance => instance.Variable.Old,
            _ => new FPackageIndex(0)
        };
        return index.IsExport() && index.ToExport(asset) is PropertyExport property && property.Property.GetType() == type;
    }

    static string ImportPath(UAsset asset, FPackageIndex index)
    {
        if (!index.IsImport()) return "";
        var import = index.ToImport(asset);
        if (!import.OuterIndex.IsImport()) return "";
        return import.OuterIndex.ToImport(asset).ObjectName + "." + import.ObjectName;
    }
}

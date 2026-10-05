using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

static class FunctionLayoutContracts
{
    public static void Validate(UAsset asset, FunctionExport function)
    {
        var localSeen = false; var outSeen = false; var returns = 0;
        foreach (var child in function.Children)
        {
            if (!child.IsExport() || child.ToExport(asset) is not PropertyExport property)
                throw new InvalidDataException("Invalid function field: " + function.ObjectName);
            var flags = property.Property.PropertyFlags;
            if (flags.HasFlag(EPropertyFlags.CPF_Parm)) {
                // UE 4.22 InitializeDerivedMembers/GetReturnProperty require a
                // contiguous parameter prefix before locals. A parser round-trip
                // alone does not check this runtime signature contract.
                if (localSeen) throw new InvalidDataException("Parameter follows a local: " + function.ObjectName);
                outSeen |= flags.HasFlag(EPropertyFlags.CPF_OutParm);
                if (flags.HasFlag(EPropertyFlags.CPF_ReturnParm)) returns++;
            } else localSeen = true;
        }
        if (returns > 1) throw new InvalidDataException("Multiple return parameters: " + function.ObjectName);
        if (outSeen && !function.FunctionFlags.HasFlag(EFunctionFlags.FUNC_HasOutParms))
            throw new InvalidDataException("Missing out-parameter flag: " + function.ObjectName);
        if (localSeen && !function.FunctionFlags.HasFlag(EFunctionFlags.FUNC_HasDefaults))
            throw new InvalidDataException("Missing local-initialization flag: " + function.ObjectName);
    }
}

using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.UnrealTypes;

public static class CookedDependencyGraph
{
    static void Add(List<FPackageIndex> list, FPackageIndex value)
    {
        if (!value.IsNull() && !list.Any(x => x.Index == value.Index)) list.Add(value);
    }
    public static void Repair(UAsset asset)
    {
        foreach (var e in asset.Exports)
        {
            Add(e.CreateBeforeCreateDependencies, e.OuterIndex);
            if (e is PropertyExport p)
            {
                switch (p.Property)
                {
                    case UArrayProperty a: Add(e.SerializationBeforeSerializationDependencies, a.Inner); break;
                    case UObjectProperty o: Add(e.CreateBeforeSerializationDependencies, o.PropertyClass); break;
                    case UStructProperty s: Add(e.SerializationBeforeSerializationDependencies, s.Struct); break;
                }
            }
            if (e is StructExport st)
                foreach (var child in st.Children)
                    if (!e.CreateBeforeSerializationDependencies.Any(x => x.Index == child.Index))
                        Add(e.SerializationBeforeSerializationDependencies, child);
            if (e is FunctionExport)
                for (var i = 0; i < asset.Imports.Count; i++)
                    Add(e.CreateBeforeSerializationDependencies, FPackageIndex.FromImport(i));
        }
        asset.DependsMap = asset.Exports.Select(e => e.SerializationBeforeSerializationDependencies
            .Concat(e.CreateBeforeSerializationDependencies).Concat(e.SerializationBeforeCreateDependencies)
            .Concat(e.CreateBeforeCreateDependencies).Select(x => x.Index).Distinct().ToArray()).ToList();
        Validate(asset);
    }
    public static void Validate(UAsset asset)
    {
        foreach (var e in asset.Exports)
        {
            foreach (var dependency in e.SerializationBeforeSerializationDependencies.Concat(e.CreateBeforeSerializationDependencies)
                .Concat(e.SerializationBeforeCreateDependencies).Concat(e.CreateBeforeCreateDependencies))
                if (dependency.IsNull() || dependency.Index > asset.Exports.Count || -dependency.Index > asset.Imports.Count)
                    throw new InvalidDataException($"Invalid preload dependency: {e.ObjectName}");
            if (e is PropertyExport && !e.OuterIndex.IsNull()
                && !e.CreateBeforeCreateDependencies.Any(x => x.Index == e.OuterIndex.Index))
                throw new InvalidDataException($"Missing field outer creation dependency: {e.ObjectName}");
            if (e is PropertyExport { Property: UArrayProperty array }
                && !e.SerializationBeforeSerializationDependencies.Any(x => x.Index == array.Inner.Index))
                throw new InvalidDataException($"Missing array inner serialization dependency: {e.ObjectName}");
            if (e is not StructExport st) continue;
            foreach (var child in st.Children)
            {
                if (!child.IsExport() || child.Index > asset.Exports.Count)
                    throw new InvalidDataException($"Invalid child field: {e.ObjectName}");
                var field = child.ToExport(asset);
                if (field.OuterIndex.Index != asset.Exports.IndexOf(e) + 1)
                    throw new InvalidDataException($"Child owner mismatch: {e.ObjectName}/{field.ObjectName}");
                if (!e.SerializationBeforeSerializationDependencies.Concat(e.CreateBeforeSerializationDependencies)
                    .Any(x => x.Index == child.Index))
                    throw new InvalidDataException($"Missing child preload dependency: {e.ObjectName}/{field.ObjectName}");
            }
        }
    }
}

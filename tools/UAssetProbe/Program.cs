using System.Collections;
using System.Reflection;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: UAssetProbe <path-to-uasset>");
    return 2;
}

var path = args[0];
var versions = new[]
{
    EngineVersion.VER_UE4_20,
    EngineVersion.VER_UE4_21,
    EngineVersion.VER_UE4_22,
    EngineVersion.VER_UE4_23,
};

foreach (var version in versions)
{
    Console.WriteLine($"=== {version} ===");
    try
    {
        var asset = new UAsset(path, version);
        Console.WriteLine("SUCCESS");
        Console.WriteLine($"ObjectVersion={asset.ObjectVersion} ({(int)asset.ObjectVersion})");
        Console.WriteLine($"Names={asset.GetNameMapIndexList().Count}");
        Console.WriteLine($"Imports={asset.Imports.Count}");
        Console.WriteLine($"Exports={asset.Exports.Count}");

        Console.WriteLine();
        Console.WriteLine("== IMPORTS ==");
        for (var i = 0; i < asset.Imports.Count; i++)
        {
            var import = asset.Imports[i];
            Console.WriteLine($"I{i + 1}: {import.ClassPackage}.{import.ClassName} {import.ObjectName} outer={import.OuterIndex.Index}");
        }

        Console.WriteLine();
        Console.WriteLine("== FUNCTIONS / KISMET TREE ==");
        foreach (var fn in asset.Exports.OfType<FunctionExport>())
        {
            Console.WriteLine($"### {fn.ObjectName} bytecode={fn.ScriptBytecode?.Length ?? 0}");

            if (fn.ScriptBytecode is not { Length: > 0 })
                continue;

            for (var i = 0; i < fn.ScriptBytecode.Length; i++)
            {
                Console.WriteLine($"  [{i}]");
                DumpValue(fn.ScriptBytecode[i], "    ", 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
            }
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

return 1;

static void DumpValue(object? value, string indent, int depth, HashSet<object> seen)
{
    if (value is null)
    {
        Console.WriteLine(indent + "null");
        return;
    }

    if (depth > 8)
    {
        Console.WriteLine(indent + "<max-depth>");
        return;
    }

    var type = value.GetType();

    if (value is string || type.IsPrimitive || value is decimal || value is Enum)
    {
        Console.WriteLine(indent + $"{type.Name}: {value}");
        return;
    }

    if (value is FName fname)
    {
        Console.WriteLine(indent + $"FName: {fname}");
        return;
    }

    if (value is FPackageIndex packageIndex)
    {
        Console.WriteLine(indent + $"FPackageIndex: {packageIndex.Index}");
        return;
    }

    if (value is IEnumerable enumerable)
    {
        Console.WriteLine(indent + type.Name);
        var index = 0;
        foreach (var item in enumerable)
        {
            Console.WriteLine(indent + $"  [{index++}]");
            DumpValue(item, indent + "    ", depth + 1, seen);
        }
        return;
    }

    if (!type.IsValueType)
    {
        if (!seen.Add(value))
        {
            Console.WriteLine(indent + $"<{type.Name} cycle>");
            return;
        }
    }

    Console.WriteLine(indent + type.FullName);

    foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
    {
        Console.WriteLine(indent + $"  .{field.Name}");
        try
        {
            DumpValue(field.GetValue(value), indent + "    ", depth + 1, seen);
        }
        catch (Exception ex)
        {
            Console.WriteLine(indent + $"    <error: {ex.Message}>");
        }
    }

    foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                 .Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
    {
        if (property.Name is "Token")
        {
            Console.WriteLine(indent + $"  .{property.Name}");
            try
            {
                DumpValue(property.GetValue(value), indent + "    ", depth + 1, seen);
            }
            catch (Exception ex)
            {
                Console.WriteLine(indent + $"    <error: {ex.Message}>");
            }
        }
    }
}

using Newtonsoft.Json;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet;
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
        Console.WriteLine("== EXPORTS ==");
        for (var i = 0; i < asset.Exports.Count; i++)
        {
            var export = asset.Exports[i];
            var outer = export.OuterIndex.Index;
            var cls = export.ClassIndex.Index;
            var super = export.SuperIndex.Index;
            Console.WriteLine($"E{i + 1}: {export.GetType().Name} name={export.ObjectName} outer={outer} class={cls} super={super}");

            if (export is ClassExport ce)
            {
                Console.WriteLine($"    SuperStruct={ce.SuperStruct.Index}");
                Console.WriteLine($"    ClassFlags={ce.ClassFlags}");
                Console.WriteLine($"    ClassWithin={ce.ClassWithin.Index}");
                Console.WriteLine($"    ClassGeneratedBy={ce.ClassGeneratedBy.Index}");
                Console.WriteLine($"    ClassDefaultObject={ce.ClassDefaultObject.Index}");
                Console.WriteLine($"    bCooked={ce.bCooked}");
                Console.WriteLine($"    Children=[{string.Join(",", ce.Children.Select(x => x.Index))}]");
                Console.WriteLine($"    FuncMap=[{string.Join(",", ce.FuncMap.Keys.Select((k,i) => $"{k}:{ce.FuncMap[i].Index}"))}]");
            }

            if (export is NormalExport ne)
            {
                Console.WriteLine($"    DataCount={ne.Data?.Count ?? 0}");
                if (ne.Data != null)
                {
                    foreach (var d in ne.Data)
                    {
                        Console.WriteLine($"    Data {d.Name}: {d.GetType().Name} = {d.RawValue}");
                    }
                }
            }

            if (export is PropertyExport pe && pe.Property != null)
            {
                Console.WriteLine($"    PropertyType={pe.Property.GetType().Name}");
                Console.WriteLine($"    ArrayDim={pe.Property.ArrayDim}");
                Console.WriteLine($"    ElementSize={pe.Property.ElementSize}");
                Console.WriteLine($"    PropertyFlags={pe.Property.PropertyFlags}");
                Console.WriteLine($"    RepNotifyFunc={pe.Property.RepNotifyFunc}");

                var pt = pe.Property.GetType();
                foreach (var propName in new[] { "Next", "Inner", "PropertyClass", "Struct", "Enum", "UnderlyingProp", "KeyProp", "ValueProp" })
                {
                    var p = pt.GetField(propName) ?? (System.Reflection.MemberInfo?)pt.GetProperty(propName);
                    if (p == null) continue;
                    try
                    {
                        object? value = p switch
                        {
                            System.Reflection.FieldInfo fi => fi.GetValue(pe.Property),
                            System.Reflection.PropertyInfo pi => pi.GetValue(pe.Property),
                            _ => null
                        };
                        if (value != null) Console.WriteLine($"    {propName}={value}");
                    }
                    catch {}
                }
            }
        }

        KismetSerializer.asset = asset;

        Console.WriteLine();
        Console.WriteLine("== CONTEXT OFFSET CHECK ==");
        foreach (var fn in asset.Exports.OfType<FunctionExport>())
        {
            if (fn.ScriptBytecode is not { Length: > 0 }) continue;
            foreach (var root in fn.ScriptBytecode)
            {
                uint visitOffset = 0;
                root.Visit(asset, ref visitOffset, (expr, statementOffset) =>
                {
                    if (expr is UAssetAPI.Kismet.Bytecode.Expressions.EX_Context ctx)
                    {
                        using var stream = new MemoryStream();
                        using var writer = new AssetBinaryWriter(stream, asset);
                        var computed = UAssetAPI.Kismet.Bytecode.ExpressionSerializer.WriteExpression(ctx.ContextExpression, writer);
                        Console.WriteLine($"{fn.ObjectName}: statement={statementOffset} storedSkip={ctx.Offset} computedContextICode={computed} storageBytes={stream.Length}");
                    }
                });
            }
        }

        Console.WriteLine();
        Console.WriteLine("== FUNCTIONS / KISMET JSON ==");
        foreach (var fn in asset.Exports.OfType<FunctionExport>())
        {
            Console.WriteLine($"### {fn.ObjectName} bytecode={fn.ScriptBytecode?.Length ?? 0}");
            if (fn.ScriptBytecode is { Length: > 0 })
            {
                try
                {
                    var json = KismetSerializer.SerializeScript(fn.ScriptBytecode);
                    Console.WriteLine(json.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Kismet serialization failed: {ex.GetType().Name}: {ex.Message}");
                }
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

using UAssetAPI;
using UAssetAPI.Kismet;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;

public static class DiagnosticGraphValidator
{
    public static int Validate(UAsset asset, IReadOnlyList<KismetExpression> code)
    {
        KismetSerializer.asset = asset;
        int Size(KismetExpression expr)
        {
            using var stream = new MemoryStream();
            using var writer = new AssetBinaryWriter(stream, asset);
            return ExpressionSerializer.WriteExpression(expr, writer);
        }
        var boundaries = new HashSet<uint>();
        uint position = 0;
        foreach (var root in code)
        {
            boundaries.Add(position);
            position = checked(position + (uint)Size(root));
        }
        var forbidden = new HashSet<string>(StringComparer.Ordinal) {
            "SalvageItemInSlot", "RemoveItem", "SalvageItemUndo", "Swap"
        };
        foreach (var root in code)
        {
            if (root is EX_Jump j && !boundaries.Contains(j.CodeOffset))
                throw new InvalidDataException("Jump does not target a statement boundary.");
            if (root is EX_JumpIfNot jn && !boundaries.Contains(jn.CodeOffset))
                throw new InvalidDataException("Conditional jump does not target a statement boundary.");
            uint offset = 0;
            root.Visit(asset, ref offset, (expr, _) => {
                if (expr is EX_Context ctx && ctx.Offset != (uint)Size(ctx.ContextExpression))
                    throw new InvalidDataException("Invalid context skip offset.");
                string? call = expr switch {
                    EX_VirtualFunction vf => vf.VirtualFunctionName.ToString(),
                    EX_FinalFunction ff when ff.StackNode.IsImport() => ff.StackNode.ToImport(asset).ObjectName.ToString(),
                    _ => null
                };
                if (call != null && forbidden.Contains(call))
                    throw new InvalidDataException($"Diagnostic build contains forbidden inventory mutation: {call}");
            });
        }
        return checked((int)position);
    }
}

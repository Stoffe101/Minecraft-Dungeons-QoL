using UAssetAPI;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

public static class InventoryProbeValidator
{
    public static void Validate(UAsset asset, IReadOnlyList<KismetExpression> code)
    {
        DiagnosticGraphValidator.Validate(asset, code, requireEquipmentGuards: false);
        var forbidden = new HashSet<string> { "CreateSaveGameObject", "LoadGameFromSlot", "SaveGameToSlot", "GetSalvageInfo",
            "IsLocked", "CanSalvage", "SalvageItemInSlot", "Swap", "RemoveItem", "SalvageItemUndo" };
        var calls = new List<string>();
        foreach (var root in code)
        {
            uint offset=0;
            root.Visit(asset,ref offset,(e,_)=> {
                var name=e switch {
                    EX_FinalFunction f when f.StackNode.IsImport()=>f.StackNode.ToImport(asset).ObjectName.ToString(),
                    EX_VirtualFunction v=>v.VirtualFunctionName.ToString(), _=>""
                };
                if(forbidden.Contains(name))throw new InvalidDataException("Read probe contains forbidden call: "+name);
                if(name!="")calls.Add(name);
                if(e is EX_NameConst n && new[]{"F5","F8","F9","F10"}.Contains(n.Value.ToString()))
                    throw new InvalidDataException("Read probe contains feature hotkey");
            });
        }
        foreach(var name in new[]{"GetAllActorsOfClass","SpawnObject","AddChildToCanvas","GetInventorySlots","GetDisplayNameText","GetDisplayItemPowerInt"})
            if(calls.Count(x=>x==name)!=1)throw new InvalidDataException("Missing/duplicate read probe call: "+name);
        if(calls.Contains("PrintString"))throw new InvalidDataException("Read probe relies on development-only output");
    }
}

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
            "SetJustification", "SetAutoWrapText", "IsLocked", "CanSalvage", "SalvageItemInSlot", "Swap", "RemoveItem", "SalvageItemUndo" };
        var calls = new List<string>();
        var uiContracts = new Dictionary<string, (string Owner, int Arity)> {
            ["AddChildToCanvas"]=("CanvasPanel",1), ["SetText"]=("TextBlock",1),
            ["SetVisibility"]=("Widget",1), ["RemoveFromParent"]=("Widget",0),
            ["SetAnchors"]=("CanvasPanelSlot",1), ["SetAlignment"]=("CanvasPanelSlot",1),
            ["SetPosition"]=("CanvasPanelSlot",1), ["SetSize"]=("CanvasPanelSlot",1), ["SetZOrder"]=("CanvasPanelSlot",1)
        };
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
                if(uiContracts.TryGetValue(name,out var contract)) {
                    if(e is not EX_FinalFunction function || !function.StackNode.IsImport())
                        throw new InvalidDataException("Probe UI call must use an explicit native import: "+name);
                    var imported=function.StackNode.ToImport(asset);
                    if(!imported.OuterIndex.IsImport()) throw new InvalidDataException("Wrong probe UI function owner: "+name);
                    var owner=imported.OuterIndex.ToImport(asset);
                    if(owner.ObjectName.ToString()!=contract.Owner || !owner.OuterIndex.IsImport()
                        || owner.OuterIndex.ToImport(asset).ObjectName.ToString()!="/Script/UMG")
                        throw new InvalidDataException("Wrong probe UI function owner: "+name);
                    if(function.Parameters.Length!=contract.Arity)
                        throw new InvalidDataException("Wrong probe UI call arity: "+name);
                }

                if(e is EX_StructConst st && st.Struct.IsImport() && st.Struct.ToImport(asset).ObjectName.ToString()=="Anchors")
                {
                    var type=st.Struct.ToImport(asset);
                    if(!type.OuterIndex.IsImport() || type.OuterIndex.ToImport(asset).ObjectName.ToString()!="/Script/Slate"
                        || st.StructSize!=16)
                        throw new InvalidDataException("Wrong native Anchors struct");
                }
                if(e is EX_NameConst n && new[]{"F5","F8","F9","F10"}.Contains(n.Value.ToString()))
                    throw new InvalidDataException("Read probe contains feature hotkey");
            });
        }
        foreach(var name in new[]{"IsLocalPlayerController","GetAllActorsOfClass","SpawnObject","AddChildToCanvas","GetInventorySlots","GetDisplayNameText","GetDisplayItemPowerInt"})
            if(calls.Count(x=>x==name)!=1)throw new InvalidDataException("Missing/duplicate read probe call: "+name);
        if(calls.Contains("PrintString"))throw new InvalidDataException("Read probe relies on development-only output");
    }
}

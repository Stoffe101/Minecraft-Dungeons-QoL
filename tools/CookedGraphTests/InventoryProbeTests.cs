using UAssetAPI;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;
public static class InventoryProbeTests
{
    public static int Run(UAsset asset, KismetExpression[] code)
    {
        InventoryProbeValidator.Validate(asset,code);
        void Reject(Action mutate,Action restore,string expected) {
            mutate();try {InventoryProbeValidator.Validate(asset,code);throw new Exception("Accepted "+expected);}
            catch(InvalidDataException e){if(!e.Message.Contains(expected))throw;Console.WriteLine("[PASS] probe rejects "+expected);}
            finally{restore();}
        }
        var calls=new List<EX_FinalFunction>();var names=new List<EX_NameConst>();
        foreach(var root in code){uint o=0;root.Visit(asset,ref o,(e,_)=>{if(e is EX_FinalFunction f)calls.Add(f);if(e is EX_NameConst n)names.Add(n);});}
        var spawn=calls.Single(x=>x.StackNode.ToImport(asset).ObjectName.ToString()=="SpawnObject");var oldFn=spawn.StackNode;
        var save=FPackageIndex.FromImport(asset.Imports.FindIndex(x=>x.ObjectName.ToString()=="SaveGameToSlot"));
        Reject(()=>spawn.StackNode=save,()=>spawn.StackNode=oldFn,"Read probe contains forbidden call");
        var hotkey=names.First(x=>x.Value.ToString()=="F6");var oldName=hotkey.Value;
        Reject(()=>hotkey.Value=new FName(asset,"F8"),()=>hotkey.Value=oldName,"Read probe contains feature hotkey");
        var open=asset.Imports.Single(x=>x.ObjectName.ToString()=="IsInventoryOpen");var oldOpen=open.ObjectName;
        Reject(()=>open.ObjectName=new FName(asset,"MissingOpen"),()=>open.ObjectName=oldOpen,"Expected one inventory-open guard");
        bool IsOpen(KismetExpression root){var found=false;uint o=0;root.Visit(asset,ref o,(e,_)=>{if(e is EX_InstanceVariable v&&v.Variable.Old.IsImport()&&v.Variable.Old.ToImport(asset).ObjectName.ToString()=="IsInventoryOpen")found=true;});return found;}
        var gate=code.OfType<EX_JumpIfNot>().Single(x=>IsOpen(x.BooleanExpression));uint fallthrough=0;
        for(int i=0;i<=Array.IndexOf(code,gate);i++){using var m=new MemoryStream();using var w=new AssetBinaryWriter(m,asset);fallthrough+=(uint)ExpressionSerializer.WriteExpression(code[i],w);}
        var target=gate.CodeOffset;
        Reject(()=>gate.CodeOffset=fallthrough,()=>gate.CodeOffset=target,"Inventory input/read reachable without open inventory");
        var jump=code.OfType<EX_Jump>().First();var oldJump=jump.CodeOffset;
        Reject(()=>jump.CodeOffset=1,()=>jump.CodeOffset=oldJump,"Jump does not target a statement boundary");
        var context=(EX_Context)((EX_LetObj)code[0]).AssignmentExpression; // First controller lookup.
        var skip=context.Offset;
        Reject(()=>context.Offset++,()=>context.Offset=skip,"Invalid context skip offset");

        var anchor=asset.Imports.Single(x=>x.ObjectName.ToString()=="Anchors");var oldOuter=anchor.OuterIndex;
        var umg=FPackageIndex.FromImport(asset.Imports.FindIndex(x=>x.ObjectName.ToString()=="/Script/UMG"));
        Reject(()=>anchor.OuterIndex=umg,()=>anchor.OuterIndex=oldOuter,"Wrong native Anchors struct");
        InventoryProbeValidator.Validate(asset,code);Console.WriteLine("[PASS] inventory-read probe remains valid after 7 rejection tests");return 0;
    }
}

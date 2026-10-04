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
        var open=asset.Imports.Single(x=>x.ObjectName.ToString()=="IsVisible");var oldOpen=open.ObjectName;
        Reject(()=>open.ObjectName=new FName(asset,"MissingOpen"),()=>open.ObjectName=oldOpen,"Expected one inventory-open guard");
        var gate=code.OfType<EX_JumpIfNot>().Single(x=>DiagnosticGraphValidator.IsInventoryOpenCondition(asset,x.BooleanExpression));uint fallthrough=0;
        for(int i=0;i<=Array.IndexOf(code,gate);i++){using var m=new MemoryStream();using var w=new AssetBinaryWriter(m,asset);fallthrough+=(uint)ExpressionSerializer.WriteExpression(code[i],w);}
        var target=gate.CodeOffset;
        Reject(()=>gate.CodeOffset=fallthrough,()=>gate.CodeOffset=target,"Inventory input/read reachable without open inventory");
        var jump=code.OfType<EX_Jump>().First();var oldJump=jump.CodeOffset;
        Reject(()=>jump.CodeOffset=1,()=>jump.CodeOffset=oldJump,"Jump does not target a statement boundary");
        var context=(EX_Context)((EX_LetObj)code[1]).AssignmentExpression; // First controller lookup.
        var skip=context.Offset;
        Reject(()=>context.Offset++,()=>context.Offset=skip,"Invalid context skip offset");

        var anchor=asset.Imports.Single(x=>x.ObjectName.ToString()=="Anchors");var oldOuter=anchor.OuterIndex;
        var umg=FPackageIndex.FromImport(asset.Imports.FindIndex(x=>x.ObjectName.ToString()=="/Script/UMG"));
        Reject(()=>anchor.OuterIndex=umg,()=>anchor.OuterIndex=oldOuter,"Wrong native Anchors struct");
        var dispatcher=(EX_ComputedJump)code[0];var entry=dispatcher.CodeOffsetExpression;
        Reject(()=>dispatcher.CodeOffsetExpression=new EX_IntConst {Value=10},()=>dispatcher.CodeOffsetExpression=entry,"Missing tick entry dispatcher");
        var tick=asset.Exports.OfType<UAssetAPI.ExportTypes.FunctionExport>().Single(x=>x.ObjectName.ToString()=="ReceiveTick");
        var call=tick.ScriptBytecode.OfType<EX_LocalFinalFunction>().Single();var targetEntry=(EX_IntConst)call.Parameters.Single();
        Reject(()=>targetEntry.Value=0,()=>targetEntry.Value=10,"ReceiveTick must call the graph body at offset 10");
        var originalTarget=call.StackNode;
        Reject(()=>call.StackNode=UAssetAPI.UnrealTypes.FPackageIndex.FromExport(asset.Exports.IndexOf(tick)),()=>call.StackNode=originalTarget,"ReceiveTick must call the graph body at offset 10");
        var tickDefaults=DiagnosticGraphValidator.InventoryTickDefaults(asset);
        foreach(var flagName in new[]{"bCanEverTick","bStartWithTickEnabled","bTickEvenWhenPaused"}) {
            var flag=tickDefaults.Value.OfType<UAssetAPI.PropertyTypes.Objects.BoolPropertyData>().Single(x=>x.Name.ToString()==flagName);
            Reject(()=>flag.Value=false,()=>flag.Value=true,"Inventory actor tick flag must be enabled: "+flagName);
        }
        var pauseFlag=tickDefaults.Value.Single(x=>x.Name.ToString()=="bTickEvenWhenPaused");var pauseIndex=tickDefaults.Value.IndexOf(pauseFlag);
        Reject(()=>tickDefaults.Value.Remove(pauseFlag),()=>tickDefaults.Value.Insert(pauseIndex,pauseFlag),"Inventory actor tick flag must be enabled: bTickEvenWhenPaused");
        var interval=tickDefaults.Value.OfType<UAssetAPI.PropertyTypes.Objects.FloatPropertyData>().Single(x=>x.Name.ToString()=="TickInterval");
        Reject(()=>interval.Value=0.1f,()=>interval.Value=0f,"Inventory actor tick interval must be zero");
        var visibility=(EX_Context)gate.BooleanExpression;var receiver=(EX_LocalVariable)visibility.ObjectExpression;var originalReceiver=receiver.Variable;
        var wrongReceiver=new KismetPropertyPointer(FPackageIndex.FromExport(asset.Exports.FindIndex(x=>x.ObjectName.ToString()=="ProbeCanvas")));
        Reject(()=>receiver.Variable=wrongReceiver,()=>receiver.Variable=originalReceiver,"Expected one inventory-open guard");
        var visibleOwner=open.OuterIndex;
        var textClass=FPackageIndex.FromImport(asset.Imports.FindIndex(x=>x.ObjectName.ToString()=="TextBlock"));
        Reject(()=>open.OuterIndex=textClass,()=>open.OuterIndex=visibleOwner,"Expected one inventory-open guard");
        var spawnImport=spawn.StackNode.ToImport(asset);var originalSpawnName=spawnImport.ObjectName;
        foreach(var unsafeName in new[]{"SetJustification","SetAutoWrapText"})
            Reject(()=>spawnImport.ObjectName=new FName(asset,unsafeName),()=>spawnImport.ObjectName=originalSpawnName,"Read probe contains forbidden call");
        var anchorsCall=calls.Single(x=>x.StackNode.ToImport(asset).ObjectName.ToString()=="SetAnchors");
        var anchorsFunction=anchorsCall.StackNode.ToImport(asset);var anchorsOwner=anchorsFunction.OuterIndex;
        var canvasClass=FPackageIndex.FromImport(asset.Imports.FindIndex(x=>x.ObjectName.ToString()=="CanvasPanel"));
        Reject(()=>anchorsFunction.OuterIndex=canvasClass,()=>anchorsFunction.OuterIndex=anchorsOwner,"Wrong probe UI function owner");
        var localFunction=asset.Imports.Single(x=>x.ObjectName.ToString()=="IsLocalPlayerController");var localName=localFunction.ObjectName;
        Reject(()=>localFunction.ObjectName=new FName(asset,"MissingLocalPlayer"),()=>localFunction.ObjectName=localName,"Expected one local-player guard");
        var localGate=code.OfType<EX_JumpIfNot>().Single(x=>DiagnosticGraphValidator.IsLocalPlayerCondition(asset,x.BooleanExpression));
        var localTarget=localGate.CodeOffset;uint localFallthrough=0;
        for(int i=0;i<=Array.IndexOf(code,localGate);i++){using var m=new MemoryStream();using var w=new AssetBinaryWriter(m,asset);localFallthrough+=(uint)ExpressionSerializer.WriteExpression(code[i],w);}
        Reject(()=>localGate.CodeOffset=localFallthrough,()=>localGate.CodeOffset=localTarget,"Inventory input/read reachable without local player");
        var actor=asset.Exports.OfType<UAssetAPI.ExportTypes.ClassExport>().Single();
        var cdo=(UAssetAPI.ExportTypes.NormalExport)actor.ClassDefaultObject.ToExport(asset);
        var replicates=cdo.Data.OfType<UAssetAPI.PropertyTypes.Objects.BoolPropertyData>().Single(x=>x.Name.ToString()=="bReplicates");
        Reject(()=>replicates.Value=true,()=>replicates.Value=false,"Inventory actor must not replicate");
        var setText=calls.First(x=>x.StackNode.ToImport(asset).ObjectName.ToString()=="SetText");
        var setTextArgument=setText.Parameters[0];
        var conversions=code.OfType<EX_Let>().Where(x=>x.Expression is EX_Context c && c.ContextExpression is EX_FinalFunction f
            && f.StackNode.ToImport(asset).ObjectName.ToString()=="Conv_StringToText").ToArray();
        Reject(()=>setText.Parameters[0]=conversions[0].Expression,()=>setText.Parameters[0]=setTextArgument,
            "Reference argument must be a typed value: SetText");
        var toString=calls.Single(x=>x.StackNode.ToImport(asset).ObjectName.ToString()=="Conv_TextToString");
        var textArgument=toString.Parameters[0];
        var nameResult=code.OfType<EX_Let>().Single(x=>x.Expression is EX_Context c && c.ContextExpression is EX_FinalFunction f
            && f.StackNode.ToImport(asset).ObjectName.ToString()=="GetDisplayNameText");
        Reject(()=>toString.Parameters[0]=nameResult.Expression,()=>toString.Parameters[0]=textArgument,
            "Reference argument must be a typed value: Conv_TextToString");
        var pending=asset.Exports.FindIndex(x=>x.ObjectName.ToString()=="ProbePendingText");
        Reject(()=>setText.Parameters[0]=new EX_LocalVariable {Variable=new KismetPropertyPointer(FPackageIndex.FromExport(pending))},
            ()=>setText.Parameters[0]=setTextArgument,"Reference argument must be a typed value: SetText");
        var concat=calls.First(x=>x.StackNode.ToImport(asset).ObjectName.ToString()=="Concat_StrStr");
        var concatArgument=concat.Parameters[0];
        var stringResult=code.OfType<EX_Let>().First(x=>x.Expression is EX_Context c && c.ContextExpression is EX_FinalFunction f
            && f.StackNode.ToImport(asset).ObjectName.ToString()=="BuildString_Int");
        Reject(()=>concat.Parameters[0]=stringResult.Expression,()=>concat.Parameters[0]=concatArgument,
            "Reference argument must be a typed value: Concat_StrStr");
        var rvalue=((EX_Context)nameResult.Expression).RValuePointer;
        Reject(()=>((EX_Context)nameResult.Expression).RValuePointer=new KismetPropertyPointer(new FPackageIndex(0)),
            ()=>((EX_Context)nameResult.Expression).RValuePointer=rvalue,"Native scalar return needs a matching typed local");
        InventoryProbeValidator.Validate(asset,code);Console.WriteLine("[PASS] inventory-read probe remains valid after 28 rejection tests");return 0;
    }
}

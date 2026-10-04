using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

if (args.Length != 2) { Console.Error.WriteLine("Usage: CookedInventoryProbe <manager.uasset> <output.uasset>"); return 2; }
var asset = new UAsset(args[0], EngineVersion.VER_UE4_22);
var owner = asset.Exports.OfType<ClassExport>().Single();
var uber = asset.Exports.OfType<FunctionExport>().Single(x => x.ObjectName.ToString().StartsWith("ExecuteUbergraph_"));
FPackageIndex Package(string name) {
    for (int i=0;i<asset.Imports.Count;i++) if(asset.Imports[i].ClassName.ToString()=="Package" && asset.Imports[i].ObjectName.ToString()==name) return FPackageIndex.FromImport(i);
    return asset.AddImport(new Import("/Script/CoreUObject","Package",new FPackageIndex(0),name,false,asset));
}
FPackageIndex Import(string package,string type,string name,FPackageIndex outer) {
    for(int i=0;i<asset.Imports.Count;i++){var x=asset.Imports[i];if(x.ClassPackage.ToString()==package && x.ClassName.ToString()==type && x.ObjectName.ToString()==name && x.OuterIndex.Index==outer.Index)return FPackageIndex.FromImport(i);}
    return asset.AddImport(new Import(package,type,outer,name,false,asset));
}
FPackageIndex Class(string package,string name)=>Import("/Script/CoreUObject","Class",name,Package(package));
FPackageIndex Default(string package,string name)=>Import(package,name,"Default__"+name,Package(package));
FPackageIndex Fn(FPackageIndex cls,string name)=>Import("/Script/CoreUObject","Function",name,cls);
FPackageIndex Member(FPackageIndex cls,string type,string name)=>Import("/Script/CoreUObject",type,name,cls);
PropertyExport Field(string name)=>asset.Exports.OfType<PropertyExport>().Single(x=>x.ObjectName.ToString()==name);
FPackageIndex Existing(string name) { var i=asset.Imports.FindIndex(x=>x.ObjectName.ToString()==name); if(i<0)throw new InvalidDataException("Missing observed import: "+name);return FPackageIndex.FromImport(i); }
FPackageIndex Index(Export e)=>FPackageIndex.FromExport(asset.Exports.IndexOf(e));
PropertyExport NewObject(string name,FPackageIndex cls,bool instance=false) {
    var p=(PropertyExport)Field("MCDQoL_CurrentSlot").Clone(); p.ObjectName=new FName(asset,name);p.OuterIndex=Index(instance?owner:uber);
    p.Property=new UObjectProperty { ArrayDim=Field("MCDQoL_CurrentSlot").Property.ArrayDim,ElementSize=0,PropertyFlags=EPropertyFlags.CPF_None,RepNotifyFunc=new FName(asset,"None"),Next=new FPackageIndex(0),PropertyClass=cls };
    p.SerialOffset=p.SerialSize=0;p.SerializationBeforeSerializationDependencies.Clear();p.CreateBeforeSerializationDependencies.Clear();p.SerializationBeforeCreateDependencies.Clear();p.CreateBeforeCreateDependencies.Clear();asset.Exports.Add(p);
    var st=(StructExport)(instance?owner:uber);st.Children=st.Children.Append(Index(p)).ToArray();return p;
}
var canvasClass=Class("/Script/UMG","CanvasPanel");var textClass=Class("/Script/UMG","TextBlock");var widgetClass=Class("/Script/UMG","Widget");var slotClass=Class("/Script/UMG","CanvasPanelSlot");
var canvas=NewObject("ProbeCanvas",canvasClass);var text=NewObject("ProbeText",textClass,true);var oldCanvas=NewObject("ProbeCanvasOwner",canvasClass,true);var uiSlot=NewObject("ProbeCanvasSlot",slotClass);
var actors=Field("InventorySlots"); // Separate actor array: do not reinterpret inventory slots as actors.
var actorArray=(PropertyExport)actors.Clone();actorArray.ObjectName=new FName(asset,"ProbeManagers");asset.Exports.Add(actorArray);uber.Children=uber.Children.Append(Index(actorArray)).ToArray();
var actorInner=NewObject("ProbeManagers_Inner",Class("/Script/Engine","Actor"));actorInner.OuterIndex=Index(actorArray);uber.Children=uber.Children.Where(x=>x.Index!=Index(actorInner).Index).ToArray();
actorArray.Property=new UArrayProperty {ArrayDim=Field("MCDQoL_CurrentSlot").Property.ArrayDim,ElementSize=0,PropertyFlags=EPropertyFlags.CPF_None,RepNotifyFunc=new FName(asset,"None"),Next=new FPackageIndex(0),Inner=Index(actorInner)};

PropertyExport NewString(string name,bool instance) {
    var p=NewObject(name,textClass,instance);p.ClassIndex=Class("/Script/CoreUObject","StrProperty");
    p.Property=new UStrProperty {ArrayDim=Field("MCDQoL_CurrentSlot").Property.ArrayDim,ElementSize=0,PropertyFlags=EPropertyFlags.CPF_None,RepNotifyFunc=new FName(asset,"None"),Next=new FPackageIndex(0)};return p;
}
var pendingText=NewString("ProbePendingText",false);var cachedText=NewString("ProbeCachedText",true);
var canvasMember=Member(Existing("UMG_InventoryHUD_C"),"ObjectProperty","WholeCanvas");
var gameplay=Class("/Script/Engine","GameplayStatics");var gameplayDefault=Default("/Script/Engine","GameplayStatics");
var system=Class("/Script/Engine","KismetSystemLibrary");var systemDefault=Default("/Script/Engine","KismetSystemLibrary");
var math=Class("/Script/Engine","KismetMathLibrary");var arrayClass=Class("/Script/Engine","KismetArrayLibrary");var arrayDefault=Default("/Script/Engine","KismetArrayLibrary");
var stringClass=Class("/Script/Engine","KismetStringLibrary");var stringDefault=Default("/Script/Engine","KismetStringLibrary");var textLib=Class("/Script/Engine","KismetTextLibrary");var textDefault=Default("/Script/Engine","KismetTextLibrary");
var vector2=Import("/Script/CoreUObject","ScriptStruct","Vector2D",Package("/Script/CoreUObject"));var anchors=Import("/Script/CoreUObject","ScriptStruct","Anchors",Package("/Script/UMG"));
var key=Existing("Key");var pc=Field("CallFunc_GetPlayerController_ReturnValue");var controller=Field("MCDQoL_Controller");var shared=Field("MCDQoL_SharedUI");var hud=Field("MCDQoL_InventoryHUD");var stash=Field("CallFunc_GetItemStashComponent_ReturnValue");var slots=Field("InventorySlots");var cursor=Field("CursorIndex");var currentSlot=Field("MCDQoL_CurrentSlot");var item=Field("MCDQoL_CurrentItem");
KismetPropertyPointer Ptr(FPackageIndex x)=>new(x);
KismetExpression L(PropertyExport p)=>new EX_LocalVariable {Variable=Ptr(Index(p))};
KismetExpression I(PropertyExport p)=>new EX_InstanceVariable {Variable=Ptr(Index(p))};
KismetExpression V(FPackageIndex p)=>new EX_InstanceVariable {Variable=Ptr(p)};
KismetExpression O(FPackageIndex p)=>new EX_ObjectConst {Value=p};KismetExpression N(int n)=>new EX_IntConst{Value=n};KismetExpression S(string s)=>new EX_StringConst{Value=s};
int Size(KismetExpression e){using var m=new MemoryStream();using var w=new AssetBinaryWriter(m,asset);return ExpressionSerializer.WriteExpression(e,w);}
KismetExpression C(KismetExpression obj,KismetExpression e,FPackageIndex? result=null)=>new EX_Context {ObjectExpression=obj,ContextExpression=e,Offset=(uint)Size(e),RValuePointer=Ptr(result??new FPackageIndex(0)),PropertyType=0};
KismetExpression F(FPackageIndex fn,params KismetExpression[] p)=>new EX_FinalFunction {StackNode=fn,Parameters=p};
KismetExpression Static(FPackageIndex obj,FPackageIndex fn,params KismetExpression[] p)=>C(O(obj),F(fn,p));
KismetExpression M(string fn,params KismetExpression[] p)=>new EX_CallMath {StackNode=Fn(math,fn),Parameters=p};
KismetExpression Valid(KismetExpression p)=>Static(systemDefault,Fn(system,"IsValid"),p);
KismetExpression Length(KismetExpression p)=>Static(arrayDefault,Fn(arrayClass,"Array_Length"),p);
KismetExpression Vec(float x,float y)=>new EX_StructConst {Struct=vector2,StructSize=8,Value=new KismetExpression[]{new EX_FloatConst{Value=x},new EX_FloatConst{Value=y}}};
KismetExpression Press(string name)=>C(L(pc),F(Fn(Class("/Script/Engine","PlayerController"),"WasInputKeyJustPressed"),new EX_StructConst {Struct=key,StructSize=32,Value=new KismetExpression[]{new EX_NameConst{Value=new FName(asset,name)}}}));
KismetExpression Build(KismetExpression prefix,KismetExpression number,string suffix)=>Static(stringDefault,Fn(stringClass,"BuildString_Int"),prefix,S(""),number,S(suffix));
var code=new List<KismetExpression>();var labels=new Dictionary<string,int>();var jumps=new List<(KismetExpression,string)>();
void Add(KismetExpression e)=>code.Add(e);void Label(string name)=>labels[name]=code.Count;
void Branch(KismetExpression e,string target){var b=new EX_JumpIfNot {BooleanExpression=e};Add(b);jumps.Add((b,target));}
void Jump(string target){var b=new EX_Jump();Add(b);jumps.Add((b,target));}
void Obj(PropertyExport p,KismetExpression e,bool instance=false)=>Add(new EX_LetObj {VariableExpression=instance?I(p):L(p),AssignmentExpression=e});
void Int(KismetExpression e)=>Add(new EX_Let {Value=Ptr(Index(cursor)),Variable=I(cursor),Expression=e});
void Visibility(byte v)=>Add(C(I(text),F(Fn(widgetClass,"SetVisibility"),new EX_ByteConst{Value=v})));
var reportNumber=0;
void Show(KismetExpression s) {
    Add(new EX_Let {Value=Ptr(Index(pendingText)),Variable=L(pendingText),Expression=s});
    var write="WRITE_TEXT_"+(reportNumber++);
    Branch(Static(stringDefault,Fn(stringClass,"EqualEqual_StrStr"),L(pendingText),I(cachedText)),write);Jump("END");Label(write);
    Add(new EX_Let {Value=Ptr(Index(cachedText)),Variable=I(cachedText),Expression=L(pendingText)});
    Add(C(I(text),F(Fn(textClass,"SetText"),Static(textDefault,Fn(textLib,"Conv_StringToText"),L(pendingText)))));
}

Obj(pc,Static(gameplayDefault,Fn(gameplay,"GetPlayerController"),new EX_Self(),N(0)));Branch(Valid(L(pc)),"HIDE");
Obj(controller,new EX_DynamicCast {ClassPtr=Existing("BP_PlayerController_C"),Target=L(pc)});Branch(Valid(L(controller)),"HIDE");
Obj(shared,C(L(controller),V(Existing("SharedUI")),Index(shared)));Branch(Valid(L(shared)),"HIDE");
Obj(hud,new EX_DynamicCast {ClassPtr=Existing("UMG_InventoryHUD_C"),Target=C(L(shared),V(Existing("InventoryHUD")),Index(hud))});Branch(Valid(L(hud)),"HIDE");
Branch(C(L(hud),V(Existing("IsInventoryOpen"))),"HIDE");
// One manager owns feedback; additional loader instances stay idle. No actor is destroyed.
Add(Static(gameplayDefault,Fn(gameplay,"GetAllActorsOfClass"),new EX_Self(),O(Index(owner)),L(actorArray)));
Branch(M("GreaterEqual_IntInt",Length(L(actorArray)),N(1)),"HIDE");
Branch(M("EqualEqual_ObjectObject",new EX_ArrayGetByRef {ArrayVariable=L(actorArray),ArrayIndex=N(0)},new EX_Self()),"HIDE");
Obj(canvas,C(L(hud),V(canvasMember),Index(canvas)));Branch(Valid(L(canvas)),"HIDE");
Branch(Valid(I(text)),"CREATE");Branch(M("EqualEqual_ObjectObject",L(canvas),I(oldCanvas)),"REMOVE");Jump("READY");
Label("REMOVE");Add(C(I(text),F(Fn(widgetClass,"RemoveFromParent"))));
Label("CREATE");
Add(new EX_Let {Value=Ptr(Index(cachedText)),Variable=I(cachedText),Expression=S("")});
Obj(text,new EX_DynamicCast {ClassPtr=textClass,Target=Static(gameplayDefault,Fn(gameplay,"SpawnObject"),O(textClass),L(hud))},true);Branch(Valid(I(text)),"HIDE");
Obj(uiSlot,C(L(canvas),F(Fn(canvasClass,"AddChildToCanvas"),I(text)),Index(uiSlot)));Branch(Valid(L(uiSlot)),"HIDE");
Obj(oldCanvas,L(canvas),true);
Add(C(L(uiSlot),F(Fn(slotClass,"SetAnchors"),new EX_StructConst {Struct=anchors,StructSize=16,Value=new KismetExpression[]{Vec(0.5f,1f),Vec(0.5f,1f)}})));
Add(C(L(uiSlot),F(Fn(slotClass,"SetAlignment"),Vec(0.5f,1f))));Add(C(L(uiSlot),F(Fn(slotClass,"SetPosition"),Vec(0f,-100f))));
Add(C(L(uiSlot),F(Fn(slotClass,"SetSize"),Vec(1000f,90f))));Add(C(L(uiSlot),F(Fn(slotClass,"SetZOrder"),N(100))));
Add(C(I(text),F(Fn(Class("/Script/UMG","TextLayoutWidget"),"SetJustification"),new EX_ByteConst{Value=1})));
Add(C(I(text),F(Fn(Class("/Script/UMG","TextLayoutWidget"),"SetAutoWrapText"),new EX_True())));
Label("READY");Visibility(3); // HitTestInvisible: display only, no input capture.
Obj(stash,C(L(controller),F(Existing("GetItemStashComponent")),Index(stash)));Branch(Valid(L(stash)),"NO_STASH");
Add(new EX_Let {Value=Ptr(Index(slots)),Variable=L(slots),Expression=C(L(stash),F(Existing("GetInventorySlots")),Index(slots))});
Branch(Press("F7"),"PREVIOUS");Int(M("Add_IntInt",I(cursor),N(1)));
Label("PREVIOUS");Branch(Press("F6"),"BOUNDS");Int(M("Subtract_IntInt",I(cursor),N(1)));
Label("BOUNDS");Branch(M("GreaterEqual_IntInt",Length(L(slots)),N(1)),"EMPTY");
Branch(M("Less_IntInt",I(cursor),N(0)),"HIGH");Int(M("Subtract_IntInt",Length(L(slots)),N(1)));
Label("HIGH");Branch(M("GreaterEqual_IntInt",I(cursor),Length(L(slots))),"ITEM");Int(N(0));
Label("ITEM");Obj(currentSlot,new EX_ArrayGetByRef {ArrayVariable=L(slots),ArrayIndex=I(cursor)});Branch(Valid(L(currentSlot)),"EMPTY_SLOT");
Obj(item,C(L(currentSlot),V(Existing("Item")),Index(item)));Branch(Valid(L(item)),"EMPTY_SLOT");
var display=C(L(item),new EX_VirtualFunction {VirtualFunctionName=new FName(asset,"GetDisplayNameText"),Parameters=Array.Empty<KismetExpression>()});
var name=Static(textDefault,Fn(textLib,"Conv_TextToString"),display);
var description=Build(Build(S("MCD QoL READ-ONLY | "),Length(L(slots))," slots | F6/F7 browse\nSlot "),I(cursor)," | ");
var joined=Static(stringDefault,Fn(stringClass,"Concat_StrStr"),description,name);
Show(Build(Static(stringDefault,Fn(stringClass,"Concat_StrStr"),joined,S(" | power ")),C(L(item),new EX_VirtualFunction {VirtualFunctionName=new FName(asset,"GetDisplayItemPowerInt"),Parameters=Array.Empty<KismetExpression>()})," | no item changes"));Jump("END");
Label("EMPTY_SLOT");Show(Build(S("MCD QoL READ-ONLY | slot "),I(cursor)," is empty | F6/F7 browse"));Jump("END");
Label("EMPTY");Int(N(0));Show(S("MCD QoL READ-ONLY | inventory has no slots"));Jump("END");
Label("NO_STASH");Show(S("MCD QoL READ-ONLY | inventory UI found; stash unavailable"));Jump("END");
Label("HIDE");Visibility(1);
Label("END");Add(new EX_Return {ReturnExpression=new EX_Nothing()});Add(new EX_EndOfScript());
uint offset=0;var offsets=new List<uint>();foreach(var e in code){offsets.Add(offset);offset+=(uint)Size(e);}
foreach(var (e,target) in jumps){var n=offsets[labels[target]];if(e is EX_Jump j)j.CodeOffset=n;else ((EX_JumpIfNot)e).CodeOffset=n;}
uber.ScriptBytecode=code.ToArray();uber.ScriptBytecodeRaw=null;uber.ScriptBytecodeSize=(int)offset;
CookedDependencyGraph.Repair(asset);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);asset.Write(args[1]);
var reopened=new UAsset(args[1],EngineVersion.VER_UE4_22);var fn=reopened.Exports.OfType<FunctionExport>().Single(x=>x.ObjectName.ToString().StartsWith("ExecuteUbergraph_"));
InventoryProbeValidator.Validate(reopened,fn.ScriptBytecode);
Console.WriteLine($"[OK] Visible inventory-read probe: {code.Count} statements, {reopened.Exports.Count} exports; no item/save operations.");
return 0;

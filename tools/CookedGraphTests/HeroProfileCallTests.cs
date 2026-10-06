using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

static class HeroProfileCallTests
{
    public static void Run(string fixture)
    {
        void Check(string method, string variant, bool accepted)
        {
            var asset = new UAsset(fixture, EngineVersion.VER_UE4_22);
            var donor = (FunctionExport)asset.Exports.OfType<FunctionExport>().First().Clone();
            donor.Children = []; asset.Exports.Add(donor);
            var graph = new Graph(asset, "MCDQoL_ProfileContractTest");
            var owner = graph.Class("/Script/Dungeons", method == "GetCloudPlayerId" ? "PlayerCharacterSaveSlot" : "PlayerControllerBase");
            var target = graph.Object("Target", owner);
            var result = method switch {
                "GetCloudPlayerId" => graph.Property("Result", new UStructProperty {
                    Struct = graph.Import("/Script/CoreUObject", "ScriptStruct", "Guid", graph.Package("/Script/CoreUObject"))
                }, "StructProperty"),
                "GetAvailableSaveDataByIndex" => graph.Object("Result", graph.Class("/Script/Dungeons", "CharacterSaveData")),
                "GetCharacterSlotByIndex" => graph.Object("Result", graph.Class("/Script/Dungeons", "PlayerCharacterSaveSlot")),
                _ => graph.Integer("Result")
            };
            KismetExpression[] parameters = method switch {
                "GetCharacterSlotByIndex" => [graph.N(0), new EX_False()],
                "GetAvailableSaveDataByIndex" => [graph.N(0)],
                _ => []
            };
            if (variant == "missing argument") parameters = [];
            if (variant == "extra argument") parameters = parameters.Append(graph.N(0)).ToArray();
            if (variant == "string index") parameters[0] = graph.S("0");
            if (variant == "integer bool") parameters[1] = graph.N(0);
            if (variant == "string result") result = graph.String("WrongResult");
            if (variant == "wrong object result") result = graph.Object("WrongResult", graph.Class("/Script/Dungeons", "InventoryItem"));
            if (variant == "wrong struct result") result = graph.Property("WrongResult", new UStructProperty {
                Struct = graph.Import("/Script/CoreUObject", "ScriptStruct", "Vector", graph.Package("/Script/CoreUObject"))
            }, "StructProperty");
            var call = (EX_FinalFunction)graph.F(graph.Fn(owner, method), parameters);
            var context = (EX_Context)graph.C(graph.L(target), call, graph.Index(result));
            if (variant == "untyped result") context.RValuePointer = new KismetPropertyPointer(new FPackageIndex(0));
            graph.Function.ScriptBytecode = variant == "bare call" ? [call] : [context];
            try {
                FunctionImportContracts.Validate(asset);
                HeroProfileCallContracts.Validate(asset);
                if (!accepted) throw new Exception("Accepted " + method + " " + variant);
            } catch (InvalidDataException) { if (accepted) throw; }
            Console.WriteLine($"[PASS] profile call {(accepted ? "accepts" : "rejects")} {method}: {variant}");
        }
        foreach (var method in HeroProfileCallContracts.ControllerMethods.Append("GetCloudPlayerId")) {
            Check(method, "observed contract", true);
            Check(method, "extra argument", false);
            Check(method, "string result", false);
            Check(method, "untyped result", false);
            Check(method, "bare call", false);
        }
        foreach (var method in new[] { "GetAvailableSaveDataByIndex", "GetCharacterSlotByIndex" }) {
            Check(method, "missing argument", false);
            Check(method, "string index", false);
            Check(method, "wrong object result", false);
        }
        Check("GetCharacterSlotByIndex", "integer bool", false);
        Check("GetCloudPlayerId", "wrong struct result", false);
    }
}

using UAssetAPI;
using UAssetAPI.UnrealTypes;

static class FunctionOwnerTests
{
    public static void Run(string fixture)
    {
        void Check(string method, string owner, string package, bool accepted)
        {
            var asset = new UAsset(fixture, EngineVersion.VER_UE4_22);
            var p = asset.AddImport(new Import("/Script/CoreUObject", "Package", new FPackageIndex(0), package, false, asset));
            var cls = asset.AddImport(new Import("/Script/CoreUObject", "Class", p, owner, false, asset));
            asset.AddImport(new Import("/Script/CoreUObject", "Function", cls, method, false, asset));
            try {
                FunctionImportContracts.Validate(asset);
                if (!accepted) throw new Exception("Accepted wrong function owner: " + owner + "." + method);
            } catch (InvalidDataException) { if (accepted) throw; }
            Console.WriteLine($"[PASS] {(accepted ? "accepts" : "rejects")} {package}.{owner}.{method}");
        }
        Check("GetOwningPlayer", "Widget", "/Script/UMG", true);
        Check("GetOwningPlayer", "UserWidget", "/Script/UMG", false);
        Check("GetOwningPlayer", "Widget", "/Script/Engine", false);
        Check("SetIsEnabled", "Widget", "/Script/UMG", true);
        Check("SetIsEnabled", "UserWidget", "/Script/UMG", false);
        Check("SetContent", "ContentWidget", "/Script/UMG", true);
        Check("SetContent", "Button", "/Script/UMG", false);
    }
}

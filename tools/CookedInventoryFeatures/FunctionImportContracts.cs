using UAssetAPI;

static class FunctionImportContracts
{
    // Reflected declaring owners, from the pinned UE 4.22.3 headers. A valid
    // C++ inherited method is not necessarily a valid UFunction import on a subclass.
    static readonly Dictionary<string, (string package, string owner)> Owners = new() {
        ["GetOwningPlayer"] = ("/Script/UMG", "Widget"),
        ["SetIsEnabled"] = ("/Script/UMG", "Widget"),
        ["GetVisibility"] = ("/Script/UMG", "Widget"),
        ["SetVisibility"] = ("/Script/UMG", "Widget"),
        ["SetContent"] = ("/Script/UMG", "ContentWidget"),
        ["GetContent"] = ("/Script/UMG", "ContentWidget"),
        ["SetFont"] = ("/Script/UMG", "TextBlock"),
        ["SetBrushColor"] = ("/Script/UMG", "Border"),
        ["SetBackgroundColor"] = ("/Script/UMG", "Button"),
        ["SetOffsets"] = ("/Script/UMG", "CanvasPanelSlot"),
        ["SalvageItemInSlot"] = ("/Script/Dungeons", "ItemStashComponent"),
    };
    public static void Validate(UAsset asset)
    {
        foreach (var import in asset.Imports.Where(x => x.ClassName.ToString() == "Function"))
        {
            if (!Owners.TryGetValue(import.ObjectName.ToString(), out var expected)) continue;
            if (!import.OuterIndex.IsImport()) throw new InvalidDataException("Unverified function owner: " + import.ObjectName);
            var owner = import.OuterIndex.ToImport(asset);
            if (owner.ObjectName.ToString() != expected.owner || !owner.OuterIndex.IsImport()
                || owner.OuterIndex.ToImport(asset).ObjectName.ToString() != expected.package)
                throw new InvalidDataException("Wrong reflected declaring owner for " + import.ObjectName + ": expected " + expected.package + "." + expected.owner);
        }
    }
}

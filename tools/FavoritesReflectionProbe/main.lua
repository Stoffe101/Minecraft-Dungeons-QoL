-- Read reflection metadata only. No item setters, save calls, game hooks or
-- forced asset loading. UE4SS itself still hooks the engine during startup.
local capturing = false
RegisterKeyBindAsync(Key.H, {ModifierKey.CONTROL}, function()
    if capturing then return end
    capturing = true
    print("[MCDQoLReflection] Capture started\n")
    local ok, err = pcall(function()
        GenerateSDK()
        DumpAllObjects()
    end)
    if ok then
        print("[MCDQoLReflection] Capture completed\n")
    else
        print("[MCDQoLReflection] Capture failed: " .. tostring(err) .. "\n")
    end
    capturing = false
end)
print("[MCDQoLReflection] Ready. In camp, open inventory and press Ctrl+H once.\n")

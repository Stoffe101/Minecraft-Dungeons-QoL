# Mod Source Assets

Project-owned Unreal source assets belong under:

`mod/Content/`

During development, `scripts/Sync-Assets.ps1` copies this tree into the cloned Dungeons Mod Kit's:

`.tools/Dungeons-Mod-Kit/UE4Project/Content/`

## Planned layout

```text
mod/Content/
  BPLoader/
    Lobby/
      MCDQoL_Lobby.umap
    Ingame/
      MCDQoL_Ingame.umap
  Mods/
    MinecraftDungeonsQoL/
      BP_MCDQoL_Manager.uasset
      BP_MCDQoL_LockService.uasset
      BP_MCDQoL_SalvageService.uasset
      BP_MCDQoL_GearSetService.uasset
      SG_MCDQoL.uasset
      UI/
        WBP_MCDQoL_Overlay.uasset
        WBP_MCDQoL_SalvageReview.uasset
```

Editable project runtime/UI Blueprints do not exist under this source tree yet. Cooked diagnostic assets are instead generated from the permitted template by `scripts/Build-Diagnostic.ps1`, using the C# tools. `Build.ps1` remains the future editor authoring route. Do not copy cooked diagnostic packages into this editable source tree as though they were uncooked assets.

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

The binary Unreal assets do not exist yet. Their exact implementation depends on current-build inventory API research.

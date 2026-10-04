# Research Log

## 2026-10-04 — Project bootstrap

### Goal

Establish a safe development foundation for a Minecraft Dungeons 1 QoL mod containing:

- gear locking
- gear manager/loadouts
- multi-select mass salvage

### Findings

- The Dokucraft Dungeons Mod Kit uses UE 4.22 and is MIT licensed.
- Blueprint Loader can inject content at Menu, Lobby, and Ingame triggers.
- A new 2026 Dungeons 1 mod, LetMeMove!, reports that the old Dungeons 1 Blueprint Loader still works on the latest game build.
- LetMeMove is MIT licensed and confirms a minimal current Blueprint Loader content structure.
- Modern Xbox app / Microsoft Store installs expose the Paks directory under the normal game installation, commonly beneath `C:\XboxGames`.
- Vanilla salvage returns emeralds and invested enchantment points and has a limited undo state.
- Therefore mass salvage should invoke the native salvage path rather than reimplementing the economy.
- Blueprint Loader's Nexus permissions mean it must remain a separately installed dependency.

### Decisions

- Target Microsoft Store / Xbox app first.
- Use a separate project-owned SaveGame slot for lock/loadout metadata.
- Do not mutate hero save files.
- Prefer overlay UI and event/function hooks over wholesale replacement of the vanilla inventory widget.
- Native vanilla salvage must be the destructive backend.
- Lock protection must eventually guard vanilla salvage as well as project-owned bulk salvage.

### Implementation completed

- repository documentation structure
- Mod Kit bootstrap tooling
- environment/path detection
- asset sync/build/install tooling
- initial feature/architecture/test specifications
- offline inventory asset research helper

### Not yet tested

PowerShell tooling must be verified on a real Windows development machine.

## 2026-10-04 — Save identity investigation

### Source

CutFlame/MCDSaveEdit:
https://github.com/CutFlame/MCDSaveEdit

### Findings

The save model contains:

- profile `playerId`
- profile `uniqueSaveId`
- item `inventoryIndex`
- item `equipmentSlot`
- type, rarity, power, enchantments, gilded/netherite enchant data and other useful fingerprint fields

The editor's item-list logic:

- sorts inventory by `InventoryIndex`
- assigns newly-added items `max(existing index) + 1`
- keeps equipped items in the same Items collection, distinguished by `EquipmentSlot`

Storage transfer adds the item to the target collection, which assigns the target collection's next index. Therefore `InventoryIndex` is not a cross-storage permanent ID. A deleted highest index can also be reused by a future item.

### Result

`uniqueSaveId + inventoryIndex` is a strong provisional locator, not a guaranteed permanent identity. Prefer a native runtime GUID if one exists. If no GUID exists, pair the index with sanity/fingerprint data and fail closed when data disagrees.

### Additional research tooling

DungeonsModding/Useful-things confirms community extraction data for encrypted Dungeons assets exists. The project research helper should accept an AES key parameter rather than embedding one.

### Next research

Current runtime Blueprint discovery: inventory widget, selected item fields, native item ID/GUID, salvage/equip functions, and vanilla salvage-button guard point.

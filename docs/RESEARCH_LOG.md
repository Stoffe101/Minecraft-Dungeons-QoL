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
- Mod Kit bootstrap/tooling design
- environment/path detection design
- asset sync/build/install tooling design
- initial feature/architecture/test specifications

### Not yet tested

PowerShell tooling must be verified on a real Windows development machine after it is committed.

### Next research

Inventory runtime discovery: widget, selected item, stable item ID, salvage/equip functions, and vanilla salvage-button guard point.

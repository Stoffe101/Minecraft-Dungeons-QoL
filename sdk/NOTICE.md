# SDK mirror attribution

The editor-only Minecraft Dungeons reflection declarations under `sdk/modkit/Source/Dungeons` are adapted from technical class/function research in:

https://github.com/Minecraforever/MCD-PE

MCD-PE is licensed under Apache-2.0.

Only a minimal subset needed for Minecraft Dungeons QoL is mirrored. The project does not copy the game's gameplay implementations. Stub C++ bodies are project-created harmless editor implementations used only to let Unreal Engine 4.22 author/cook Blueprint references to `/Script/Dungeons` types.

At runtime, Minecraft Dungeons supplies the real native implementations.

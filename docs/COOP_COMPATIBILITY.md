# Online and co-op compatibility

Online play with friends is required. Compatibility is not established. The user reached camp with PR #12 but crashed opening inventory while online. The replacement probe is a candidate crash repair, not a multiplayer release.

## Local execution

The manager is non-replicated and gates inventory reads, input and UI creation on native IsLocalPlayerController. Its own cursor browses the local inventory; it does not change vanilla selection or perform item/save mutations. Friends should not need the diagnostic mod, but this must be tested. Existing full-feature code remains unverified.

## Joining-client blocker

Inspection of official Blueprint Loader file 3385182 shows its trigger selection depends on GetGameMode and Menu/Lobby/Ingame GameMode casts. Unreal documents GameMode as unavailable on ordinary joining clients. Therefore the external loader likely cannot initialize the mod when joining a friend. Actual Dungeons client behavior has not been observed. Hosting an online session does not validate this path.

The dependency cannot be silently forked or bundled: its author requires permission for modifications/asset reuse. Implement a project-owned startup path that runs from a local client UI/controller lifecycle and does not require server GameMode, then test it in retail. No compatible cooked startup asset has yet been produced; the Linux workspace has neither the game nor an Unreal 4.22 cooking environment.

## Acceptance

Use the session matrix in TEST_PLAN.md. Require camp and mission startup, own inventory reads, overlay/input behavior, mission transitions, friend join/leave and rejoin. No remote inventories, replicated helper actors or gameplay RPCs should be introduced for the local QoL UI. Production actions require separate local ownership and game API validation.

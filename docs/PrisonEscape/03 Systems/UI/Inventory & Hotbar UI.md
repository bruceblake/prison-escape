# Inventory & Hotbar UI

The 6-slot bag and its always-on hotbar mirror. Backed by `PlayerInventory` ([[Inventory & Items]]).

## Current state (7/14/2026)

Hotbar sits above the bottom edge with `1`–`6` key hints, stronger selected-slot border, and fades via `UIMenuFocus` while bag/notebook/pause are open. *(Screenshot `HUD in-game 2026-07-14.png` referenced in sibling docs — re-capture and commit to `03 Systems/UI/` when playtesting.)*

## Widgets

- `HotbarUI` — 6 slots, keys 1–6 + scroll wheel, auto-hides while the bag is open
- `InventoryUI` (E) — bag with drag-swap + notebook-style crafting tab (3 requirement slots)
- `InventorySlotUI` — shared slot prefab logic: icon, quantity, contraband tint/outline, drag ghost, tooltip hover
- `ItemTooltipUI` — hover card · `HeldItemDisplay` — equipped item viewmodel

## Polish backlog

- [x] **Lift the hotbar off the screen edge** — slots are clipped at the bottom; keep a comfortable margin.
- [x] **Slot key hints** — small `1`–`6` labels in the slot corner so the bindings are discoverable.
- [x] **Selected-slot emphasis** — the selection highlight is barely visible on empty slots; give the selected slot a clear border even when empty.
- [ ] Empty slots could read more like a prison uniform's pockets (stitched outline) than flat boxes — art pass, low priority.

## To add

- Nothing structural. Confiscation on capture already empties the bag via `PlayerInventory.ClearAllSlots()` ([[Escape Completion System]]); pillow stash contents survive by design.

## Key files

`Assets/Scripts/Shared/UI/HotbarUI.cs` · `InventoryUI.cs` · `InventorySlotUI.cs` · `ItemTooltipUI.cs` · `HeldItemDisplay.cs` · `Assets/Scripts/Multiplayer/Player/PlayerInventory.cs`

Related: [[UI & HUD]] · [[Inventory & Items]] · [[Notebook & Crafting UI]] · [[UI Theme & Style Guide]]

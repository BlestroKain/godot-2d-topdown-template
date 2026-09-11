# MMO UI architecture

This folder contains the editable client UI for the Godot MMO client.

## Core rule

**Do not flatten a complete window into one PNG.** The concept images define art direction and layout only. Runtime interfaces are composed from reusable Godot controls so designers can resize, restyle, reorder and bind them without recreating a full image.

The intended workflow mirrors the useful part of Intersect's UI approach:

- `.tscn` owns hierarchy, anchors, containers, spacing and editable layout.
- C# owns behavior and runtime data binding.
- `Theme` / shared textures own the skin.
- Item, spell, reward and equipment cells use the same reusable slot component.
- Decorative artwork must not carry gameplay information.

## Shared pieces

- `theme/mmo_ui_theme.tres` — current technomagic base skin.
- `components/MmoWindow.cs` — shared open/close/toggle + draggable-header behavior.
- `components/ui_slot.tscn` + `UiSlot.cs` — reusable slot with icon, quantity, tooltip and selection state.
- `components/UiSlotGrid.cs` — editable slot-grid generator; change `GridColumns` and `SlotCount` in the inspector.
- `UiHotkeyRouter.cs` — UI-only shortcuts that should not be coupled to combat input.

## Windows

Implemented as separate scenes:

- Character
- Inventory
- Quest Journal
- Techniques
- Shop
- Bank
- Mail
- Community
- Dialogue
- Profession / crafting
- Escape menu

HUD components are also separate scenes: combat bar, chat, quest tracker and minimap.

`GameHud` is the composition/binding layer. Interaction systems can open contextual windows through:

```csharp
gameHud.OpenWindow("shop");
gameHud.OpenWindow("bank");
gameHud.OpenWindow("mail");
gameHud.OpenWindow("community");
gameHud.OpenWindow("dialogue");
gameHud.OpenWindow("profession");
```

Stable IDs currently exposed: `character`, `inventory`, `quests`, `techniques`, `shop`, `bank`, `mail`, `community`, `dialogue`, `profession`, `escape`.

## Art skinning

When the final ornamental art is produced, split it into shared assets instead of exporting finished windows. Recommended pieces:

- window frame / panel 9-patch
- inner panel 9-patch
- normal / hover / pressed / disabled button 9-patches
- normal / selected tab 9-patches
- normal / selected / rarity slot frames
- scrollbar track + thumb
- separator / divider ornaments
- header crest holders
- cyan Malden light accents
- shared icons

Use `NinePatchRect`, `StyleBoxTexture` or Theme resources so the same artwork stretches safely and all text/content remains real Godot controls.

## Shortcut policy

- `I` Inventory
- `C` Character
- `L` Quest Journal
- `K` Techniques
- `Esc` menu

`J` remains reserved by the current combat input for attack, so quests deliberately use `L`.

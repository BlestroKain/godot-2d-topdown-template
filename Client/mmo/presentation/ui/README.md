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

- `theme/mmo_ui_theme.tres` — production technomagic master skin.
- `skin/*.svg` — scalable, editable source textures used as 9-patch-style `StyleBoxTexture` assets.
- `components/MmoWindow.cs` — shared open/close/toggle + draggable-header behavior; applies the outer window skin automatically.
- `components/ui_slot.tscn` + `UiSlot.cs` — reusable slot with icon, quantity, tooltip and selection state.
- `components/UiSlotGrid.cs` — editable slot-grid generator; change `GridColumns` and `SlotCount` in the inspector.
- `components/UiTabBar.cs` — exclusive editable tab behavior and tab skin assignment.
- `UiHotkeyRouter.cs` — UI-only shortcuts that should not be coupled to combat input.

## Master skin

The skin is intentionally split into small reusable assets instead of exporting finished windows:

- `window_frame.svg` — outer window frame.
- `inner_panel.svg` — embedded content panels.
- `button_normal.svg`, `button_hover.svg`, `button_pressed.svg`, `button_disabled.svg`.
- `slot_normal.svg`, `slot_selected.svg`.
- `tab_normal.svg`, `tab_active.svg`.
- `field.svg` — search / input fields.
- `scroll_track.svg`, `scroll_thumb.svg`.
- `separator_h.svg`.
- `progress_bg.svg`, `health_fill.svg`, `mana_fill.svg`.

The theme exposes reusable variations:

- `UiWindowPanel`
- `UiSlotButton`
- `UiTabButton`
- `HealthBar`
- `ManaBar`

The normal `PanelContainer` style is the inset panel. `MmoWindow` automatically switches the root window to `UiWindowPanel`, so nested panels keep a distinct visual hierarchy without hand-styling every node.

### Art direction tokens

- Background: blue-black / petroleum blue.
- Structural metal: aged copper / bronze.
- Deep outline: near-black brown.
- Malden accent: restrained cyan / turquoise.
- Primary text: warm ivory.
- HP: muted crimson.
- PM: cyan-blue.

Keep the visual rule close to **80% functional / 20% world decoration**. The skin should frame content, not compete with items, characters or the map.

## Preview scene

Open and run:

`res://mmo/presentation/ui/dev/ui_skin_preview.tscn`

This is the fast visual testbed for the master skin. It shows the outer frame, nested panel, button states, tabs, input field, reusable slots, separators and HP/PM bars in one scene. Tune shared assets/theme here first; production windows should inherit the result automatically.

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

## Adding new art

Do not replace a complete `.tscn` with an image. Add or replace only the relevant shared texture and keep content as Godot controls.

Use `StyleBoxTexture`, Theme variations or `NinePatchRect` where appropriate. Future ornamental additions should remain separate, for example:

- rarity slot borders
- header crest holders
- warning / destructive buttons
- profession-specific ornaments
- faction emblems
- Malden animated accents
- shared semantic icons

## Shortcut policy

- `I` Inventory
- `C` Character
- `L` Quest Journal
- `K` Techniques
- `Esc` menu

`J` remains reserved by the current combat input for attack, so quests deliberately use `L`.
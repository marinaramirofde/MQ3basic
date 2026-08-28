# MQ3basic — Developer Guide

This document records the custom modules added to the project, their locations,
their scene dependencies, and the Inspector wiring required to reuse them.

## Project conventions

- Custom runtime code lives under `Assets/MRF/Scripts` and is grouped by feature.
- SDK/package source is never modified. All custom behavior is implemented in `Assets/MRF`.
- Scene references are assigned explicitly in the Inspector. Runtime searches by name or type
  are avoided in new modules because a scene may contain several menus or rigs.
- Public methods are small entry points that can be connected to UnityEvents.
- Code comments and XML documentation are written in descriptive English.
- Update this guide whenever a custom module or Inspector connection changes.

## Current scene

- Scene: `Assets/MRF/Scenes/003HandMenuTest.unity`
- The scene was moved from `Assets/MRF/Scenes/HandMenuUI`; its `.meta` file moved with it.
- `PassthroughGridLayout` contains three Toggle controls named `Virtual`, `Passthrough`,
  and `Focus`. They share an exclusive `ToggleGroup` and call the visualization controller
  directly; their former `UIThemeManager.ApplyTheme` callbacks have been removed.

## 002SettingUI dropdowns

Scene: `Assets/MRF/Scenes/SettingUI/002SettingUI.unity`

The Themes and Visualization Mode button grids were replaced visually by dropdowns cloned
from the local Seated Mode control. The former `ThemeGridLayout` and `PassthroughGridLayout`
remain inactive in the scene as recoverable references; they do not participate in layout or
receive input.

No additional runtime adapter is required in this scene. Each `DropDownGroup` owns an explicit,
ordered Toggle list and forwards its selected integer through one visible Inspector event:

| Dropdown | Explicit option order | Inspector event target |
|---|---|---|
| `ThemeDropdownGridLayout` | Dark Theme, Light Theme, Custom Theme 1, Custom Theme 2 | `ContentRoot > UIThemeManager.ApplyTheme(int)` |
| `VisualizationModeDropdownGridLayout` | Virtual, Passthrough, Focus | `[BuildingBlock] Passthrough > VisualizationModeController.SetMode(int)` |

Both events use the dynamic `Int32` value emitted by `DropDownGroup`; no constant argument is
stored. Each cloned dropdown has its own `ToggleGroup`, so selections in Themes, Visualization
Mode, or Seated Mode cannot affect one another. The Toggle arrays are serialized explicitly,
which avoids hierarchy searches and ambiguity when additional menus are added.

Design responsibilities remain separated:

- `DropDownGroup` owns presentation, expansion, option ordering, and exclusive selection.
- `UIThemeManager` owns theme application.
- `VisualizationModeController` owns the complete Virtual/Passthrough/Focus state.
- UnityEvents connect UI to behavior visibly in the Inspector.

## Visualization module

### Files

```text
Assets/MRF/Scripts/Visualization/
├── VisualizationMode.cs
├── VisualizationModeController.cs
└── VisualizationModeDropdownAdapter.cs

Assets/MRF/Shaders/
└── PassthroughFader.shader
```

`VisualizationModeController` replaced the earlier `VirtualPassthroughFader`. Its `.meta`
GUID was preserved, so the existing scene component remains attached.

### Exclusive modes

| Dropdown index | Enum value | Passthrough | Skybox and floor | Camera background |
|---:|---|---|---|---|
| `0` | `VisualizationMode.Virtual` | Off | On | Skybox |
| `1` | `VisualizationMode.Passthrough` | On | Off | Transparent / real environment |
| `2` | `VisualizationMode.Focus` | Off | Off | Opaque black |

Only the renderers listed in `Virtual Environment Renderers` are hidden. Their GameObjects,
colliders, and locomotion surfaces remain active. Hands, menus, and other digital objects remain
visible in Passthrough and Focus unless their renderers are explicitly added to that list.

`SetMode(VisualizationMode)` is the central state change. It always applies one complete
state, so Passthrough and Focus cannot remain active together. These independent entry
points are also available for normal buttons:

```csharp
controller.SetMode(VisualizationMode.Virtual);
controller.SetMode(VisualizationMode.Passthrough);
controller.SetMode(VisualizationMode.Focus);

controller.SetVirtual();
controller.SetPassthrough();
controller.SetFocus();
```

The Virtual/Passthrough transition uses `PassthroughFader.shader` and follows the sphere
fade pattern used by Unity MR Motifs. Focus is applied immediately: Passthrough is disabled,
the virtual environment is hidden, and the XR camera clears to black.

### Controller references already assigned in the scene

Select `[BuildingBlock] Passthrough` and inspect `Visualization Mode Controller`:

| Inspector field | Scene reference |
|---|---|
| `Passthrough Layer` | `OVRPassthroughLayer` on `[BuildingBlock] Passthrough` |
| `Xr Camera` | `CenterEyeAnchor` camera |
| `Fader Shader` | `Assets/MRF/Shaders/PassthroughFader.shader` |
| `Virtual Environment Renderers[0]` | `MeshRenderer` on `Floor` |
| `Initial Mode` | `Virtual` |
| `Fade Duration` | `0.75` seconds |

Add only renderers that belong exclusively to the virtual environment to `Virtual Environment
Renderers`. Never deactivate a floor GameObject that also owns collision. The current `Floor`
remains active in every mode: its MeshRenderer is hidden in Passthrough and Focus, while its
BoxCollider continues supporting locomotion. Do not add hand, controller, menu, or persistent
digital-content renderers to this list.

## Current three-toggle selector

`PassthroughGridLayout` owns a `ToggleGroup` with `Allow Switch Off` disabled. Its persistent
`On Value Changed (Boolean)` connections are visible in the Inspector:

| Toggle | Controller method |
|---|---|
| `Virtual` | `VisualizationModeController.SetVirtual(bool)` |
| `Passthrough` | `VisualizationModeController.SetPassthrough(bool)` |
| `Focus` | `VisualizationModeController.SetFocus(bool)` |

The Boolean overloads react only when their Toggle becomes `true`. The `false` notification
emitted for the previously selected option is intentionally ignored. `Virtual` is selected
initially, and the shared ToggleGroup guarantees that exactly one option remains selected.

## Alternative: creating the visualization dropdown

Use the existing Seated Mode dropdown only as a visual prefab/template. Do not reuse its
component or its options for both settings.

1. Duplicate the complete Seated dropdown object under `PassthroughGridLayout`.
2. Rename the duplicate to `VisualizationModeDropdown`.
3. Keep exactly three option toggles and rename their GameObjects and labels in this order:
   `Virtual`, `Passthrough`, `Focus`.
4. Verify that all three options reference the same `Toggle Group`, and disable
   `Allow Switch Off`. This provides exclusive visual selection.
5. Add `VisualizationModeDropdownAdapter` to `VisualizationModeDropdown`.
6. Assign these references explicitly:

| Adapter field | Assign |
|---|---|
| `Drop Down Group` | The `DropDownGroup` on this visualization dropdown |
| `Mode Controller` | `VisualizationModeController` on `[BuildingBlock] Passthrough` |
| `Virtual Toggle` | The `Virtual` option Toggle |
| `Passthrough Toggle` | The `Passthrough` option Toggle |
| `Focus Toggle` | The `Focus` option Toggle |

7. In the same `DropDownGroup`, add one visible persistent event under
   `When Selection Changed (Int32)`:

```text
VisualizationModeDropdown
└── VisualizationModeDropdownAdapter.SetMode(int)
```

Use the dynamic `SetMode(Int32)` entry so the emitted dropdown index is passed through.
Do not enter a constant number in the event. No `On Value Changed` event is needed on the
three individual toggles.

At startup, the adapter injects the three explicit toggles in enum order and applies the
currently selected index. Afterwards, the visible `When Selection Changed` event performs
all changes. There are no automatic scene searches, so another menu cannot be selected by
mistake.

## Seated Mode dropdown

- Adapter: `Assets/MRF/Scripts/SeatedModeDropdownAdapter.cs`
- Meta setting controlled: `Oculus.Interaction.Locomotion.StandingSetting`
- Mapping: `0 = Standing`, `1 = Seating`

This is separate from visualization. Seated mode changes the user's stance/height handling;
it does not change the skybox, floor, Passthrough, or camera background.

## Locomotion selector

- Script: `Assets/MRF/Scripts/LocomotionModeSelector.cs`
- Modes: Teleport and Continuous/Slide.
- The two UI toggles are placed in one `ToggleGroup` with `Allow Switch Off` disabled.
- The selector updates Meta `MovingSetting` instances and enables teleport interactors only
  while Teleport is selected.

This module predates the explicit-reference rule and still discovers Meta locomotion settings
and teleport interactors in the scene. Refactor it separately before supporting multiple rigs.

## Change log

### 2026-08-28 — 002SettingUI dropdown conversion

- Replaced the active Themes and Visualization Mode button grids with independent dropdowns.
- Serialized all option Toggle references in enum/theme order.
- Connected visible dynamic-Int32 events directly to the existing domain controllers.
- Kept the former button grids inactive for safe visual comparison or recovery.

### 2026-08-28 — Visualization modes

- Added the `VisualizationMode` enum with Virtual, Passthrough, and Focus values.
- Replaced `VirtualPassthroughFader` with the modular `VisualizationModeController`.
- Preserved explicit Passthrough, camera, shader, and floor references in the scene.
- Added an explicit-reference dropdown adapter with one visible Inspector event.
- Defined Focus as digital content over an opaque black background.
- Added this developer guide.
- Renamed the Passthrough section title and its three visible option labels to match the
  `VisualizationMode` enum; hid the unused fourth copied option.
- Replaced the three copied `ApplyTheme` events with explicit Boolean calls to
  `SetVirtual`, `SetPassthrough`, and `SetFocus`; added an exclusive ToggleGroup.
- Changed virtual-environment control from whole GameObjects to explicit Renderers, keeping
  the Floor BoxCollider active in Passthrough and Focus so locomotion cannot fall through it.

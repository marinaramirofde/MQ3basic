# MQ3basic — Developer Guide

This document records the custom modules added to the project, their locations,
their scene dependencies, and the Inspector wiring required to reuse them.

## Project conventions

- Custom runtime code lives under `Assets/MRF/Scripts` and is grouped by feature.
- SDK/package source is never modified. All custom behavior is implemented in `Assets/MRF`.
- Prefer explicit `[SerializeField]` references assigned in the Inspector. Avoid runtime searches
  by name or type because a scene may contain several menus or rigs.
- Keep UI-to-behavior connections visible through `OnClick`, `OnValueChanged`, or another
  serialized UnityEvent. Public event handlers are small and named after the user action.
- Automate repetitive setup only when it reduces error without hiding ownership. Any runtime
  discovery or generated object must be isolated in a clearly named method and documented here.
- Keep classes focused and methods short. A feature should have one obvious controller instead
  of behavior distributed across unrelated objects.
- Comments and XML documentation are written in English. They explain intent, constraints, or
  non-obvious SDK behavior; they do not narrate self-explanatory lines.
- Log only important state changes and actionable problems. Use `Debug.Log` for a successful
  user-visible mode change, `Debug.LogWarning` for a recoverable fallback, and `Debug.LogError`
  for missing required setup. Never log every frame or every routine callback.
- Update this guide whenever a custom module, Inspector reference, UnityEvent, important log,
  or intentional automation changes.

### Current exceptions to explicit Inspector wiring

- `SeatedModeDropdownAdapter.ConfigureOptions()` finds `Standing` and `Seating` toggles below
  its dropdown. Replace this with serialized Toggle references when that module is next edited.
- `LocomotionModeSelector` searches for every `MovingSetting` and `TeleportInteractor` because
  the current Meta hand and controller rigs may each own an instance. This exception is isolated
  in `ResolveMovingSettings()` and `SetTeleportInteractorsActive()`; revisit it before supporting
  multiple independent player rigs.

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

## Player Stance dropdown

Scene: `Assets/MRF/Scenes/SettingUI/004SettingUI.unity`

- Adapter: `Assets/MRF/Scripts/SeatedModeDropdownAdapter.cs`
- Meta setting controlled: `Oculus.Interaction.Locomotion.StandingSetting`
- Mapping: `0 = Standing` (default), `1 = Seating`
- The dropdown header starts as `Standing`.
- `DropDownGroup` receives the options in Standing/Seating order. The adapter currently finds
  those toggles by name under its own dropdown; this is a documented exception to the preferred
  explicit-reference convention.

This is separate from visualization. Seated mode changes the user's stance/height handling;
it does not change the skybox, floor, Passthrough, or camera background.

## Locomotion Mode dropdown

Scene: `Assets/MRF/Scenes/SettingUI/004SettingUI.unity`

- Heading: `Locomotion Mode`.
- Options: `0 = Teleport`, `1 = Continuous`.
- Dropdown: `LocomotionModeDropdownGridLayout`.
- `DropDownGroup.WhenSelectionChanged(Int32)` calls
  `WristPanelMenuSystem > LocomotionModeSelector.SetMode(int)`.
- The option array and both Toggle references are assigned explicitly.
- `Teleport` is the initial selection.

### Locomotion selector

- Script: `Assets/MRF/Scripts/LocomotionModeSelector.cs`
- Modes: Teleport and Continuous/Slide.
- The two dropdown options use one `ToggleGroup` with `Allow Switch Off` disabled.
- The selector updates Meta `MovingSetting` instances and enables teleport interactors only
  while Teleport is selected.

This module still discovers Meta locomotion settings and teleport interactors because hand and
controller rigs can each own an instance. Refactor that SDK-object discovery separately before
supporting multiple independent player rigs.

## Language localization

Scene: `Assets/MRF/Scenes/SettingUI/004SettingUI.unity`

- Uses Unity Localization `1.5.9` and its Addressables dependency.
- Locales and the `Settings UI` String Table are under `Assets/MRF/Localization`.
- Dropdown order: `0 = English`, `1 = Español`; English is the serialized default and its visible dynamic event calls `LanguageDropdownAdapter.SetLanguage(int)`.
- All dropdown, Locale and Toggle references are assigned explicitly in the Inspector.
- Startup priority is saved PlayerPrefs locale, Quest/system locale, then English fallback; the adapter writes the `selected-locale` key on each user change. This follows Meta's recommended behavior for unsupported device languages.
- Add future translations by creating another Locale/table column, then add its explicit dropdown option and adapter reference.

### Localization ownership

The reusable template owns localization for settings and common system UI: settings titles and
options, shared buttons, confirmations, loading states, permission prompts, generic errors, and
basic interaction or accessibility instructions. Add `LocalizeStringEvent` explicitly to each
template-owned user-facing text, preferably on its source prefab.

Each project owns its scene-specific content: narrative, dialogue, subtitles, objectives,
tutorials, object names and descriptions, and localized voice or media. Keep that content in
separate project-specific String or Asset Table Collections instead of adding it to `Settings UI`.

## Change log

### 2026-08-31  Player Stance and Locomotion dropdown

- Renamed the visible `Seated Mode` section to `Player Stance`.
- Added the two-option `Locomotion Mode` dropdown to `004SettingUI`; `002SettingUI` remains unchanged.
- Connected its dynamic integer event to `LocomotionModeSelector.SetMode(int)`.
- Assigned the dropdown toggles explicitly to the selector.
- Set Player Stance to `Standing` by default in `004SettingUI` and serialized its dropdown references.


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

## 004HandMenu: dominant hand (2026-09-08)

Scene: `Assets/MRF/Scenes/HandMenuUI/004HandMenu.unity`.
Both `SettingsMenuWorldRoot_TwoColumns` and `SettingsMenuWorldRoot_OneColumn`
contain `DominantHandSection` and `DominantHandDropdown`. The two-column control
fills the third row of ColumnLeft. The one-column control follows Language; its
existing layout group and content-size fitter accommodate the added controls.
The inactive alternative menu remains inactive until explicitly selected.

### Ownership and Inspector wiring

`Assets/MRF/Scripts/Settings/DominantHandWristSelector.cs` is the single owner of
this preference, attached to the always-active `WatchWristSystem`. Its explicit
references are WatchVisual, LeftWristAnchor/LeftWatchMount, and
RightWristAnchor/RightWatchMount. Default Hand is Left. The public enum and
integer events use stable values: 0 = Left, 1 = Right. Do not reorder these values;
they are also the persisted representation under PlayerPrefs key `dominant-hand`.
New installations use Left; existing saved Left/Right preferences remain unchanged.
Invalid saved values fall back to the Inspector default. The scene also places
WatchVisual under LeftWatchMount and selects Left in both dropdowns in edit mode.
This is an application preference, independent of the headset's system menu hand.

The selector restores the preference in Awake and reparents WatchVisual to the
selected mount. It resets local position/rotation and preserves the authored
visual scale. Calibrate each wrist separately on its WatchMount in the Inspector;
keep model-specific adjustments on the visual's children. The existing Meta
HandJoint components continue driving the wrist anchors. No custom Update loop,
scene search, rig reparenting, or SDK source modification is involved.

Each `DominantHandDropdownAdapter` explicitly references the shared selector,
its own DropDownGroup, Left/Right Toggles, and their Title text components.
The dropdown's serialized dynamic `WhenSelectionChanged(Int32)` event calls that
adapter's `SetDominantHand(int)`. Its Toggle array and ToggleGroup are also
serialized. The adapter injects the same ordered options automatically and
subscribes/unsubscribes to preference and localization events with its lifecycle.
It synchronizes when enabled, so reopening the inactive alternative does not
replace the saved preference. UI refreshes do not write PlayerPrefs or recursively
change the setting. Actual user changes persist immediately.

### Translation and future integrations

The Settings UI collection now includes DOMINANT_HAND, LEFT, and RIGHT in English
and Spanish: Dominant Hand / Mano dominante, Left / Izquierda, Right / Derecha.
The heading uses an explicit LocalizeStringEvent. The adapter's serialized
LocalizedString references update both option titles and the closed header when
the locale changes; Meta's dropdown normally copies the header only on selection.
Add future locale columns to the same table, without changing handedness code.

Future tools, weapons, and menu launchers can subscribe to `HandChanged` and read
`CurrentHand`, or use the selector's visible `When Hand Changed (Int32)` event.
That Inspector event also publishes the initial value in Start. Integrations
activated later should read CurrentHand when enabled. This change moves the watch;
it does not implement the future watch menu button or remap unrelated grab/input
bindings. Keep the selector on an active rig/service object, outside either menu.

### Validation and Meta publication

The two scripts compile with Unity 6000.3.21f1 Roslyn and the project's installed
Unity/Meta/Localization assembly references. Static scene checks passed for unique
file IDs, new local component references, parent-child links, both dynamic
handedness callbacks, and unique matching translation entry IDs. These checks do
not substitute for importing the scene and exercising it in Play Mode/on Quest.

Before release, test both menu variants with English and Spanish, switch Left to
Right and back, reopen the menu, restart the app, and confirm saved selection,
header text, and watch placement. Check both calibrated mounts while rotating the
wrists, opening dropdowns near panel edges, changing themes, and changing stance.
Test tracking loss/recovery, hands/controllers switching, and the system menu on
the actual headset. Existing HandJoint tracking owns anchor updates; this feature
does not add a controller fallback or tracking-loss visibility policy. Validate
those behaviors against the input modes declared by the application.

Meta's VRC overview is the release baseline:
https://developers.meta.com/horizon/resources/publish-quest-req/
In particular, verify declared input support (Tracking.2), hand pose/orientation
(Input.5 guidance), and hand/controller switching (Input.7):
https://developers.meta.com/horizon/resources/vrc-quest-input-5/
https://developers.meta.com/horizon/resources/vrc-quest-input-7/
The code does not intercept Meta system gestures or modify platform permissions.
Publication compliance has not been certified: device interaction, performance,
and the remaining app-wide VRC checks still require release validation.

## Dropdown placement within the interaction panel (2026-09-08)

The installed Meta UI Set Dropdown1LineTextOnly variant inherits its popup setup
from the icon/text dropdown prefabs. The popup uses an override-sorting Canvas,
GraphicRaycaster, CanvasGroupAlphaToggle, and DisableRaycaster. DropDownGroup
owns selection and closes the header after choosing an option. These installed
components do not implement automatic edge placement or enlarge the parent
ClippedPlaneSurface. A visually overflowing list can therefore leave the finite
ray interaction area even though its own Canvas still renders it.

Meta's design guidance recommends fitting dropdown options in the visible area
and avoiding scrolling where possible. This is design guidance, not evidence
that every Horizon OS system menu uses the same Unity implementation:
https://developers.meta.com/horizon/design/dropdowns/
https://developers.meta.com/horizon/design/dropdowns_implementation/

`Assets/MRF/Scripts/Settings/DropdownPanelPlacement.cs` is a project extension
attached to all twelve settings dropdown groups in 004HandMenu. The header
Toggle's explicit dynamic Boolean event calls SetOpen after Meta's existing
visibility event. Inspector references identify the popup RectTransform, header,
and the actual ISDK_RayInteraction/Surface RectTransform of its settings panel.
Margin defaults to six UI units. No runtime hierarchy discovery is used.

On opening, the component resolves layout once and measures world corners in
interaction-surface coordinates. It places the popup below its header when it
fits, otherwise above, and clamps it inside the panel. Option order, model depth,
selection events, localization, and Meta's visual/interaction components remain
unchanged. Existing popup LayoutElements ignore the parent layout, so the parent
does not overwrite this position. There is no new Update loop and no permanently
enlarged invisible ray surface. Larger future lists need a scrollable design or
fewer options if they exceed the panel; the component logs an actionable warning.

Validation: compiled with Unity Roslyn and installed project assemblies; verified
all twelve serialized callbacks/references, unique scene IDs, and popup layout
exclusion. Device and Play Mode interaction have not been exercised here. Before
release, open each dropdown in both layouts, select every option using ray and
poke, close/reopen, switch languages, and verify bottom-row lists remain fully
inside the surface. Repeat after changing panel size or adding options.

## Watch XR feedback

`Assets/MRF/Scenes/Watch/001Watch.unity` uses `WatchController` and the explicitly
assigned `MRF/WatchIndicator` shader. The imported `Screen` is resolved beneath
this watch when its Inspector reference is empty. `CreateScreenGlowAndTiles`
creates a circular full-face inner glow and four collider-free 3D power tiles.
The glow is unlit and additive, so the ON state does not require camera bloom.
The flat ring and four-square symbol stay fixed on the face. Four pre-instantiated cubes are visible only during a transition: they rise without orbiting, rotate around their own axes, and settle over the symbol before hiding.
Both ON and OFF transitions last four seconds. OFF settles on a black face with blue graphics; ON settles green with the full-face glow;
`liftHeight` is relative to face diameter. Disable restores tile positions, and
destruction releases generated objects/materials. Meta poke/ray interaction is unchanged.
Validate in Play Mode and Quest: toggle ON/OFF, inspect both eyes and oblique angles,
and disable/re-enable during the lift to check restoration and input subscription.

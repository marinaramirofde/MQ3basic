# Watch XR technical notes

## Responsibilities

`WatchController` owns the watch state and visual transition. Its normalized progress drives the screen, glow and four cube instances. A new press changes the target immediately, so an ON or OFF transition can reverse without waiting for the current animation.

`WatchShortcutMenuController` owns only the shortcut panel references, anchoring, physical scale, materials and button bindings. It subscribes to `WatchController.WhenPowerChanged` and calls one immediate state change:

- ON activates `ShortcutMenuWorldRoot`, keeps alpha at one and enables input.
- OFF disables input and deactivates the same GameObject.

The panel does not run a coroutine and does not calculate an animated pose. Its saved Transform is always the visible pose.

`InspectorGameObjectToggle` toggles one serialized scene target. `Shortcut 1 > On Invoked` calls its public `OnToggle()` method. After changing the target state, the component invokes its serialized `On Toggle (Boolean)` event with the resulting value.

## Serialized objects

The panel exists as a connected `WatchShortcutMenu.prefab` instance under `ShortcutMenuAttachment`. Its Canvas, graphics, text and buttons can be edited in Prefab Mode or overridden in `002Watch`.

The settings destination is the existing scene object `SettingsMenuWorldRoot_TwoColumns`. It is assigned to `InspectorGameObjectToggle.Target` on `WatchVisual` because a prefab asset cannot hold a reference to a scene object.

No runtime factory creates UI. Runtime code only binds existing buttons, clones the two shared materials to avoid asset mutation, and changes active/interactable state.

## Pose and size

`WristMenuAnchor` follows the watch surface. The prefab instance stores the final front-facing perpendicular pose. The default physical panel width is 0.12 metres and its authored separation is 0.08 metres.

When `SetAnchorMode` selects the wrist, left controller or right controller, the same attachment is reparented to the serialized anchor and receives the configured position and rotation offsets. Parent-scale compensation keeps the requested real-world width.

## Meta UI dependencies

The shortcut Canvas uses:

- Unity UI: `Canvas`, `CanvasGroup`, `GraphicRaycaster`, `Button` and `Image`.
- TextMesh Pro: `TMP_Text`.
- Meta XR Interaction SDK: `PointableCanvas`, `PlaneSurface`, `BoundsClipper`, `ClippedPlaneSurface`, `RectTransformBoundsClipperDriver`, `PokeInteractable` and `RayInteractable`.
- The scene's existing `PointableCanvasModule` and hand/controller interactors.
- Meta UI prefab `TextTileButton_IconAndLabel_Regular_UnityUIButton`.
- Shader `MRF/WatchShortcutPanel` and the two Watch material assets.

No Meta package source is modified.

## Inspector event flow

```text
Meta UI Button.onClick
    -> WatchShortcutDefinition.On Invoked
        -> InspectorGameObjectToggle.OnToggle()
            -> SettingsMenuWorldRoot_TwoColumns.SetActive(nextState)
            -> InspectorGameObjectToggle.On Toggle (Boolean)
```

The first connection is made inside `WatchShortcutDefinition.Bind()`. The connection from `On Invoked` to `OnToggle()` is persistent scene data, so it is visible and replaceable in the Inspector. Additional behavior can be added to either UnityEvent without changing runtime code.

## Editor builder

`WatchShortcutMenuSceneBuilder` creates and repairs the serialized hierarchy in `002Watch`. It also assigns all three button references, adds the settings toggle component, assigns its scene target and ensures Shortcut 1 has the persistent `OnToggle` listener.

The rebuild command is:

`Tools > MRF > Watch > Rebuild Shortcut Menu in 002Watch`

Normal content and pose changes should be made through the Hierarchy, Prefab Mode and Inspector.

## Validation checklist

1. `ShortcutMenuAttachment` is a connected prefab instance.
2. `WatchShortcutMenuController` has three non-null button, label and description references.
3. `InspectorGameObjectToggle.Target` is `SettingsMenuWorldRoot_TwoColumns`.
4. Shortcut 1 has one persistent `OnToggle` listener.
5. The settings root begins inactive.
6. The shortcut panel appears and disappears immediately with no fade or positional animation.
7. Watch ON/OFF and cube motion remain interruptible.
8. Poke, hand ray and controller ray each invoke a shortcut once.
9. `001Watch.unity` remains unchanged.

# Getting started with wrist customization

Open **003Watch**, select **SceneModules**, and choose a **Device**. Its **Appearance** and **Interaction** settings appear
inside that model section. The separate **Menu** section chooses what it opens.
SceneModuleManager owns both configuration and coordination. Save the scene to retain
changes. You do not need internal IDs, saved selections, or Tools commands.

## Add another option

Outside Play Mode, drag an object into the relevant **Add...** field and click
**Add**. The option is registered and selected. Ctrl+Z undoes registration.

| Field | Accepted input | Result |
|---|---|---|
| Add model | Prepared prefab with WristDevice on its root, or a scene device already under the slot | Prefab is instantiated under the slot; device is registered and selected. |
| Add style | Configured WristStyle component inside the selected device | Style is registered and selected for that device. |
| Add menu | Complete prefab with DeviceAnchoredMenuPresenter on its root, or a menu service in this scene | Prefab is instantiated at the scene root; menu is registered and selected. Existing scene menus keep their location outside SceneModules and the device slot. |

Existing scene objects are not moved automatically. Invalid options are rejected
with an explanation, and partial registration is rolled back. Reusing the same
reference does not create another entry; copied options receive distinct internal
identities where needed.

A 3D model alone does not define input surfaces or visual responses. A standalone
Canvas does not define menu attachment and visibility behavior. Prepare these
references once when creating an option, then reuse the completed prefab.
The older WatchShortcutMenu prefab contains UI only; it requires a configured
DeviceAnchoredMenuPresenter service before it can be registered as a complete menu option.

## Understand the hierarchy

```text
LeftWristAnchor                   Tracks the left wrist
  LeftWatchMount                  Shared calibration for that hand
    WristDeviceSlot               Container moved when the selected hand changes
      WatchDevice                 Complete interchangeable device prefab
        WatchVisual               Imported model
        WristMenuAnchor           Menu attachment point
        ActivationSurface         Stable press surface
        Styles
          LedCubes                Light effects and cube animation
          BlackAndWhite           Black when off; white when on
RightWristAnchor                  Tracks the right wrist
  RightWatchMount                 Shared calibration for the other hand
```

Selecting the right hand moves the **same WristDeviceSlot** under RightWatchMount.
It does not create a second watch. Multiple device options may be registered under
the slot; the module activates only the selected device during runtime. Styles
are presentation components, not complete copies of the watch.

| Change | Location |
|---|---|
| Select a device, style, or menu for a client | SceneModules Inspector |
| Calibrate shared placement for each hand | LeftWatchMount / RightWatchMount |
| Change geometry, model scale, or device-specific fit | Device prefab |
| Adjust where the device can be pressed | Its ActivationSurface |
| Adjust the device's menu attachment point | Its WristMenuAnchor |
| Edit exposed colors, animation or menu content | Appearance and Menu sections in SceneModules |
| Modify hand tracking | XR rig implementation |

Keep the slot at unit scale with no device-specific offsets. Per-hand calibration
belongs to the mounts; model-specific adjustments belong to the device prefab.
This keeps device replacement separate from rig calibration.

## Why keep the slot?

The slot is a grouping Transform, with no tracking logic or Update loop of its own.
It separates hand selection from device selection. This is useful for a template
supporting multiple models and either hand; a fixed-model, fixed-hand project could
use a simpler hierarchy.

WatchDevice defines the prefab boundary. WatchVisual isolates imported geometry
from input and styles. Styles is an organizational group, not a runtime requirement.
None of these hierarchy levels needs another global manager.

This is a modular design choice, not a measured performance claim. Validate each
new device on Quest: placement in both hands, press surface, menu position, tracking
loss and recovery, and hand/controller input.

## Optional saved selections

A saved selection remembers model, style, menu and checkbox choices only. It does
not capture edited colors, dimensions or content. Those remain in the scene or
prefab. You can customize and save a scene without creating any selection assets.

## Names and style references

Rename the device, style or menu GameObject to change its selector label. No separate
Display Name field is required. Internal IDs remain unchanged by renaming.

The device Inspector lists styles by name. Prefab wiring contains their component
references for device authors. These are configured instances, not script files.
Different behavior requires an implementation; changing exposed colors or timings
does not require writing a new script.

### Scene organization

`SceneModules` is the global configuration object and has no menu children.
`Shortcuts` is an independent scene root containing `ShortcutMenuAttachment`.
The manager references available menus, so moving a menu into your own UI folder
keeps its registration intact. Keep that folder outside SceneModules and the device slot.
Only the attachment follows the selected device anchor at runtime.

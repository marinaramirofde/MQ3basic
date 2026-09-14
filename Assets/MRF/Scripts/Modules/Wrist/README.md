# Modular wrist devices

Start with [QUICK_START.md](QUICK_START.md). The concise technical summary is in
the project-root `README_DEV.md`.

Open `003Watch` and select **SceneModules**. SceneModuleManager owns the runtime
coordination and the only customization panel. Appearance and interaction are
grouped under the selected model, using its local styles and input source.

| Section | Configuration |
|---|---|
| Device | Choose a model and show or hide it on the wrist. |
| Appearance | Choose a compatible style and edit its exposed settings inline. |
| Interaction | Enable press activation and configure power-change events. |
| Menu | Choose a menu, adjust its placement and edit supported content. |

Use the **Add model**, **Add style**, and **Add menu** fields next to the selectors
to register prepared options. IDs are generated internally. Registration supports
Undo and rolls back changes if validation fails.

**Available options in this scene** contains installation references. **Save or
restore selection (optional)** stores only device/style/menu choices and checkboxes;
colors, dimensions and content remain in the scene or prefab.

Changes made in Edit Mode are saved with the scene and take effect on entering
Play Mode. Play Mode changes are temporary. **Test activation** tests the selected
configuration without a physical press; headset interaction still needs validation.

## Development utilities

The one-time `Configure 003Watch` migration has already run. Normal customization
does not require Tools commands. Binding and Play Mode check commands remain
available for development, with reports under `Temp/WristModuleValidation`.

Legacy watch controllers are retained for earlier scenes. New implementations use
the device, input, style and menu contracts in this folder; no SDK source is changed.

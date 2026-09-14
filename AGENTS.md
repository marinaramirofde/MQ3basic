# MQ3basic collaboration and modularity rules

- Confirm the target scene from the user's request or established conversation. If it is missing, ask before scene edits; do not choose a scene based only on recency. Current modular wrist work targets `Assets/MRF/Scenes/Watch/003Watch.unity`.
- Client customization is a first-class requirement. Provide simple named selectors and checkboxes on a global scene manager. Keep technical component bindings in a setup foldout and inside the relevant prefab.
- Global managers live on dedicated root Empty GameObjects. Device visuals, tracking rigs, and menus must not own the global manager.
- Separate device identity (watch, band, bracelet), activation/input, visual style/animation, and menu/content. A style receives state and owns its own materials and animation; it must not own input or menu references.
- Device prefabs expose only device-local bindings: input surface, compatible style catalogue, and attachment points. Scene services bind independent menus and client-specific actions.
- Client presets store stable IDs and options, never scene object references. Loading a preset copies configuration; editing a scene must not silently modify a shared preset asset.
- Use explicit serialized references and visible UnityEvents. Runtime hierarchy/name searches, singleton discovery, and cross-module hidden dependencies require a concrete justification. Editor-only migration may resolve legacy references once and serialize them.
- Adding a new style or device should extend its catalogue without changing existing activation or menu implementations. Avoid enums or switch statements that enumerate concrete visual styles in the core.
- Turning a feature off must stop its input, close owned UI, release transient resources, and permit a clean restart. Switching styles must restore shared-renderer state before the next style captures it.
- Preserve calibrated tracking anchors and unaffected scenes. Do not change SDK/package source. Keep custom code under `Assets/MRF`.
- Explain architectural choices and tradeoffs as the work evolves. Document setup, extension points, validation performed and remaining headset checks. Use English for all authored text, including responses, Inspector labels, help, messages, comments, and documentation. Keep the technical explanation concise in README_DEV.md.
- Validate lifecycle and configuration behavior when changing module composition, not just compilation. Never claim Unity/Quest checks ran when only a standalone compiler or static inspection was used.

- Normal template/client customization must work in the SceneModules Inspector of an already configured scene. Do not require Tools commands, migration scripts or rebuild steps for routine option changes. Keep one-time setup and developer diagnostics separate from the daily workflow.

- Never expose editable internal IDs in routine Inspectors, including configuration assets and implementation components. Use named options. Scene personalization must present one coherent panel with inline selected-style and menu settings; internal component separation must not require scattered user workflows. Saved selections are optional and must state exactly what they capture.

- SceneModules is the sole scene customization Inspector. Internal runtime controllers must show compact read-only status only, never duplicate that panel. Present style and input settings under the selected device, with styles sourced only from its local catalogue.

- Avoid redundant wrappers and coordination components. SceneModuleManager directly owns wrist activation coordination. Add a layer only for a concrete independent responsibility; do not duplicate customization panels or retain empty hierarchy levels without a purpose.

- Keep menu and content prefabs outside the SceneModules hierarchy. Reference independent scene objects explicitly; registering a menu prefab places it at the target scene root.

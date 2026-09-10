# Watch prototype — 001Watch

## Integration
- Scene: Assets/MRF/Scenes/Watch/001Watch.unity.
- WatchController is serialized on WatchVisual. The existing DominantHandWristSelector still owns wrist placement.
- At Awake the controller resolves the imported child named Screen and its Renderer, so no manual assignment is required. These references and the generated indicator become visible in the Inspector during Play Mode.
- Inspector defaults: duration 0.6 s, cyan OFF, green ON, emission 1.8, indicator diameter 50% of Screen, local surface offset 0.003 model units. The procedural shader reference is serialized in the scene, ensuring build inclusion.
- Runtime creates a fixed sibling surface with PlaneSurface, CircleSurface, PokeInteractable and RayInteractable. Meta ISDK supplies pointer Select/Unselect/Cancel events. No physics collider is needed for these analytical surfaces. Existing hand poke and ray interactors are reused; no rig or global settings change.
- Screen rotates 360 degrees around its local Y axis and mesh centre. The original model's surface is in XZ. Interaction stays fixed during the spin. This controller assumes that same orientation when reused with another mesh.
- A single press toggles once; further selections during the transition are ignored, and held pointers must release before the next press. Pointer events do not connect to any menu.
- Screen materials are cloned at runtime, preserving their textures. Color/emission supports glTFast baseColorFactor/emissiveFactor as well as common Unity property names. Original materials are restored and clones released on destruction.
- The indicator uses an owned procedural quad and an additive URP shader with stereo instancing support. It draws a circular outline, four square pixels and a soft halo without textures, postprocessing, bloom or realtime lights. It is self illuminated; it does not cast light onto other objects.
- Disabling the component completes the current visual state, restores the resting transform and disables its interactables. Re-enabling preserves the selected state; a fresh Play session begins OFF without a log or animation.

## Verification
The runtime controller was compiled against this project's Unity 6000.3.21f1 and Meta Interaction SDK assemblies. Scene references and local SDK injection methods were checked. Visual shader compilation, Play Mode and headset interaction still require validation in Unity; they have not been asserted as tested.

1. Open/reload 001Watch from disk after Unity imports the files. Enter Play: blue screen and indicator, no spin and no Watch ON/OFF log.
2. Touch Screen with the opposite hand's poke interactor: one Watch ON log, smooth 360-degree spin and transition to green.
3. Release, then press again: one Watch OFF log, another spin and a brief blue pulse.
4. Repeat with the existing controller ray/select input. Holding the press or pressing rapidly during the spin must not cause repeated toggles.
5. Use WatchController's component context menu, Toggle Watch (Play Mode), to exercise feedback without a headset. This tests feedback only, not XR input.
6. Switch wrist using the existing selection and verify placement and interaction. Check both eyes on Quest for indicator alignment and halo.

No menu, navigation, audio, package or Project Settings changes are part of this prototype.

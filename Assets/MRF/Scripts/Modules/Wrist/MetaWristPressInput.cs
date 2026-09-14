using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
namespace MRF.Modules.Wrist
{
    public sealed class MetaWristPressInput : WristInputSource
    {
        [Tooltip("Stable surface pose. Keep outside animated visual children. Local forward faces out of the device.")]
        [SerializeField] private Transform surface;
        [SerializeField, Min(0.0001f)] private float radius = 0.02f;
        private GameObject interactionRoot;
        private PokeInteractable poke;
        private RayInteractable ray;
        private bool accepting;
        private readonly HashSet<int> pointers = new HashSet<int>();

        public override bool SetInputEnabled(bool value)
        {
            accepting = value && isActiveAndEnabled;
            pointers.Clear();
            if (accepting && interactionRoot == null)
            {
                if (surface == null || radius <= 0)
                {
                    accepting = false;
                    Debug.LogError("Wrist input requires an explicit surface and positive radius.", this);
                    return false;
                }
                CreateInteractionSurface();
            }
            if (interactionRoot != null) interactionRoot.SetActive(accepting);
            return !value || accepting;
        }

        private void CreateInteractionSurface()
        {
            interactionRoot = new GameObject("Meta Press Surface (Runtime)");
            interactionRoot.SetActive(false);
            interactionRoot.transform.SetParent(surface, false);
            var plane = interactionRoot.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Forward, false);
            var circle = interactionRoot.AddComponent<CircleSurface>();
            circle.InjectAllCircleSurface(plane);
            circle.InjectOptionalRadius(radius);
            poke = interactionRoot.AddComponent<PokeInteractable>();
            poke.InjectAllPokeInteractable(circle);
            ray = interactionRoot.AddComponent<RayInteractable>();
            ray.InjectAllRayInteractable(circle);
            poke.WhenPointerEventRaised += OnPointer;
            ray.WhenPointerEventRaised += OnPointer;
        }

        private void OnPointer(PointerEvent evt)
        {
            if (!accepting) return;
            if (evt.Type == PointerEventType.Select)
            {
                bool released = pointers.Count == 0;
                if (pointers.Add(evt.Identifier) && released) RaisePressed();
            }
            else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
                pointers.Remove(evt.Identifier);
        }
        private void OnDisable() => SetInputEnabled(false);
        private void OnDestroy()
        {
            if (poke != null) poke.WhenPointerEventRaised -= OnPointer;
            if (ray != null) ray.WhenPointerEventRaised -= OnPointer;
            if (interactionRoot != null) Destroy(interactionRoot);
        }
    }
}

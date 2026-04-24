using UnityEngine;

namespace FrontierDepths.Core
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private float maxDistance = 4f;
        [SerializeField] private LayerMask interactionMask = ~0;

        public IInteractable FocusedInteractable { get; private set; }
        public string PromptText { get; private set; }

        private void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            RefreshFocus();
        }

        public bool TryInteract()
        {
            if (FocusedInteractable == null)
            {
                return false;
            }

            if (!FocusedInteractable.CanInteract(this, out _))
            {
                return false;
            }

            FocusedInteractable.Interact(this);
            return true;
        }

        private void RefreshFocus()
        {
            FocusedInteractable = null;
            PromptText = string.Empty;

            if (interactionCamera == null)
            {
                return;
            }

            Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            FocusedInteractable = hit.collider.GetComponentInParent<IInteractable>();
            if (FocusedInteractable == null)
            {
                return;
            }

            if (FocusedInteractable.CanInteract(this, out string reason))
            {
                PromptText = $"[E] {FocusedInteractable.Prompt}";
            }
            else
            {
                PromptText = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason;
            }
        }
    }
}

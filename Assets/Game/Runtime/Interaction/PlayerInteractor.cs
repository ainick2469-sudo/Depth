using UnityEngine;

namespace FrontierDepths.Core
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private float maxDistance = 4f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private FirstPersonController playerController;

        public IInteractable FocusedInteractable { get; private set; }
        public bool HasFocusedInteractable => FocusedInteractable != null;
        public bool FocusedCanInteract { get; private set; }
        public string FocusedPrompt { get; private set; }
        public string BlockedReason { get; private set; }

        private void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = GetComponentInChildren<Camera>();
            }

            playerController = GetComponent<FirstPersonController>();
        }

        private void Update()
        {
            RefreshFocus();
        }

        public bool TryInteract()
        {
            if (playerController != null && playerController.IsUiCaptured)
            {
                return false;
            }

            if (!HasFocusedInteractable || !FocusedCanInteract)
            {
                return false;
            }

            FocusedInteractable.Interact(this);
            return true;
        }

        private void RefreshFocus()
        {
            FocusedInteractable = null;
            FocusedCanInteract = false;
            FocusedPrompt = string.Empty;
            BlockedReason = string.Empty;

            if (playerController != null && playerController.IsUiCaptured)
            {
                return;
            }

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

            FocusedPrompt = FocusedInteractable.Prompt;
            if (FocusedInteractable.CanInteract(this, out string reason))
            {
                FocusedCanInteract = true;
            }
            else
            {
                BlockedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason;
            }
        }
    }
}

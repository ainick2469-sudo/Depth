using UnityEngine;

namespace FrontierDepths.Core
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float sprintMultiplier = 1.45f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float mouseSensitivity = 0.12f;

        private CharacterController controller;
        private PlayerInteractor interactor;
        private float pitch;
        private float verticalVelocity;
        private bool uiCaptured;

        public PlayerInteractor Interactor => interactor;
        public bool IsUiCaptured => uiCaptured;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            interactor = GetComponent<PlayerInteractor>();
            playerCamera ??= GetComponentInChildren<Camera>();

            gameObject.layer = 2;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = 2;
            }

            ApplyCursorState();
        }

        private void Update()
        {
            if (uiCaptured && Input.GetKeyDown(KeyCode.Escape))
            {
                SetUiCaptured(false);
            }

            if (uiCaptured)
            {
                return;
            }

            Vector2 look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity * 10f;
            pitch = Mathf.Clamp(pitch - look.y, -89f, 89f);
            transform.Rotate(Vector3.up, look.x);
            playerCamera.transform.localEulerAngles = Vector3.right * pitch;

            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 desired = transform.right * move.x + transform.forward * move.y;
            float speed = walkSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = desired * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.E))
            {
                interactor.TryInteract();
            }
        }

        public void WarpTo(Vector3 worldPosition)
        {
            controller.enabled = false;
            transform.position = worldPosition;
            controller.enabled = true;
        }

        public void SetUiCaptured(bool value)
        {
            uiCaptured = value;
            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            Cursor.lockState = uiCaptured ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = uiCaptured;
        }
    }
}

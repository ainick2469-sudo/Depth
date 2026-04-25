using UnityEngine;

namespace FrontierDepths.Core
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        private const float GroundSnapVelocity = -2f;

        [SerializeField] private Camera playerCamera;
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float sprintMultiplier = 1.45f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float lookSensitivity = 1.2f;
        [SerializeField] private float footstepDistance = 2.25f;
        [SerializeField] private float landingVelocityThreshold = 8f;
        [SerializeField] private float feedbackVolume = 0.14f;

        private CharacterController controller;
        private PlayerInteractor interactor;
        private AudioSource feedbackAudioSource;
        private float pitch;
        private float verticalVelocity;
        private float stepDistanceAccumulated;
        private float eventDistanceAccumulated;
        private Vector3 lastPosition;
        private bool wasGroundedLastFrame;
        private bool externalUiCaptured;
        private bool manualPauseCaptured;
        private int suppressLookFrames;
        private LookRecoilState lookRecoil;

        private static AudioClip footstepClip;
        private static AudioClip landingClip;

        public PlayerInteractor Interactor => interactor;
        public bool IsUiCaptured => externalUiCaptured || manualPauseCaptured;
        public bool IsManualPauseActive => manualPauseCaptured;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            interactor = GetComponent<PlayerInteractor>();
            playerCamera ??= GetComponentInChildren<Camera>();
            feedbackAudioSource = GetComponent<AudioSource>();
            if (feedbackAudioSource == null)
            {
                feedbackAudioSource = gameObject.AddComponent<AudioSource>();
            }

            feedbackAudioSource.playOnAwake = false;
            feedbackAudioSource.loop = false;
            feedbackAudioSource.spatialBlend = 0f;
            feedbackAudioSource.volume = feedbackVolume;

            EnsureFeedbackClips();
            gameObject.layer = 2;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = 2;
            }

            pitch = NormalizePitch(playerCamera != null ? playerCamera.transform.localEulerAngles.x : 0f);
            lastPosition = transform.position;
            wasGroundedLastFrame = controller.isGrounded;
            UpdateUiCaptureState();
        }

        private void OnDisable()
        {
            RestoreRealtime();
        }

        private void OnDestroy()
        {
            RestoreRealtime();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                manualPauseCaptured = true;
                suppressLookFrames = 0;
                UpdateUiCaptureState();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                manualPauseCaptured = true;
                suppressLookFrames = 0;
                UpdateUiCaptureState();
            }
        }

        private void Update()
        {
            if (suppressLookFrames > 0)
            {
                suppressLookFrames--;
            }

            lookRecoil.Tick(Time.unscaledDeltaTime);
            ApplyCameraRotation();

            if (IsUiCaptured)
            {
                stepDistanceAccumulated = 0f;
                lastPosition = transform.position;
                wasGroundedLastFrame = controller.isGrounded;
                return;
            }

            HandleLook();
            HandleMovement();

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (InputFrameGuard.WasTownServiceCloseConsumedThisFrame)
                {
                    return;
                }

                interactor.TryInteract();
            }
        }

        public void WarpTo(Vector3 worldPosition)
        {
            controller.enabled = false;
            transform.position = worldPosition;
            controller.enabled = true;
            verticalVelocity = GroundSnapVelocity;
            stepDistanceAccumulated = 0f;
            eventDistanceAccumulated = 0f;
            lastPosition = worldPosition;
            wasGroundedLastFrame = controller.isGrounded;
        }

        public void SetUiCaptured(bool value)
        {
            externalUiCaptured = value;
            UpdateUiCaptureState();
        }

        public void ToggleManualPause()
        {
            manualPauseCaptured = !manualPauseCaptured;
            if (!manualPauseCaptured)
            {
                suppressLookFrames = 1;
            }

            UpdateUiCaptureState();
        }

        public void ResumeGameplayCapture()
        {
            if (!manualPauseCaptured)
            {
                return;
            }

            manualPauseCaptured = false;
            suppressLookFrames = 1;
            UpdateUiCaptureState();
        }

        public void ApplyLookRecoil(float pitchUpDegrees, float yawDegrees, float recoverySeconds)
        {
            lookRecoil.AddImpulse(pitchUpDegrees, yawDegrees, recoverySeconds);
            ApplyCameraRotation();
        }

        private void HandleLook()
        {
            if (playerCamera == null || suppressLookFrames > 0)
            {
                return;
            }

            float mouseX = Input.GetAxisRaw("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch - mouseY, -89f, 89f);
            transform.Rotate(Vector3.up, mouseX);
            ApplyCameraRotation();
        }

        private void HandleMovement()
        {
            bool groundedBeforeMove = controller.isGrounded;
            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 desired = transform.right * moveInput.x + transform.forward * moveInput.y;
            desired = Vector3.ClampMagnitude(desired, 1f);
            float speed = walkSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

            if (groundedBeforeMove && verticalVelocity < 0f)
            {
                verticalVelocity = GroundSnapVelocity;
            }

            if (Input.GetKeyDown(KeyCode.Space) && groundedBeforeMove)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                GameplayEventBus.Publish(new GameplayEvent
                {
                    eventType = GameplayEventType.PlayerJumped,
                    sourceObject = gameObject,
                    timestamp = Time.unscaledTime
                });
            }

            float preMoveVerticalVelocity = verticalVelocity;
            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = desired * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            bool groundedAfterMove = controller.isGrounded;
            if (groundedAfterMove && verticalVelocity < 0f)
            {
                verticalVelocity = GroundSnapVelocity;
            }

            HandleFeedback(desired, groundedAfterMove, preMoveVerticalVelocity);
            wasGroundedLastFrame = groundedAfterMove;
            lastPosition = transform.position;
        }

        private void HandleFeedback(Vector3 desired, bool groundedAfterMove, float preMoveVerticalVelocity)
        {
            if (feedbackAudioSource == null || IsUiCaptured)
            {
                return;
            }

            if (!wasGroundedLastFrame && groundedAfterMove && preMoveVerticalVelocity <= -landingVelocityThreshold)
            {
                feedbackAudioSource.PlayOneShot(landingClip, 1f);
            }

            if (!groundedAfterMove || desired.sqrMagnitude <= 0.001f)
            {
                if (!groundedAfterMove)
                {
                    stepDistanceAccumulated = 0f;
                }

                return;
            }

            Vector3 planarDelta = transform.position - lastPosition;
            planarDelta.y = 0f;
            float planarDistance = planarDelta.magnitude;
            stepDistanceAccumulated += planarDistance;
            if (DistanceMovedAccumulator.TryAccumulate(ref eventDistanceAccumulated, planarDistance, 10f, out float emittedDistance))
            {
                GameplayEventBus.Publish(new GameplayEvent
                {
                    eventType = GameplayEventType.DistanceMoved,
                    sourceObject = gameObject,
                    distance = emittedDistance,
                    timestamp = Time.unscaledTime
                });
            }

            while (stepDistanceAccumulated >= footstepDistance)
            {
                feedbackAudioSource.PlayOneShot(footstepClip, 0.85f);
                stepDistanceAccumulated -= footstepDistance;
            }
        }

        private void UpdateUiCaptureState()
        {
            Time.timeScale = IsUiCaptured ? 0f : 1f;
            Cursor.lockState = IsUiCaptured ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsUiCaptured;
        }

        private void ApplyCameraRotation()
        {
            if (playerCamera == null)
            {
                return;
            }

            Vector2 recoilOffset = lookRecoil.OffsetDegrees;
            playerCamera.transform.localEulerAngles = new Vector3(pitch + recoilOffset.x, recoilOffset.y, 0f);
        }

        private static void RestoreRealtime()
        {
            Time.timeScale = 1f;
        }

        private static float NormalizePitch(float rawPitch)
        {
            return rawPitch > 180f ? rawPitch - 360f : rawPitch;
        }

        private static void EnsureFeedbackClips()
        {
            footstepClip ??= CreateFeedbackClip("FootstepTick", 0.045f, 640f, 0.1f, 0.03f);
            landingClip ??= CreateFeedbackClip("LandingThump", 0.08f, 180f, 0.16f, 0.05f);
        }

        private static AudioClip CreateFeedbackClip(string name, float durationSeconds, float frequency, float amplitude, float decaySeconds)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(durationSeconds * sampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-time / Mathf.Max(0.01f, decaySeconds));
                samples[i] = Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

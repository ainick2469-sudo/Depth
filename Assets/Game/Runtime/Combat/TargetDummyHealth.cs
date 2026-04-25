using System;
using System.Text;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public enum TargetDummyKind
    {
        Standard,
        Armored,
        StatusTest
    }

    public sealed class TargetDummyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private TargetDummyKind dummyKind = TargetDummyKind.Standard;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float physicalDamageMultiplier = 1f;
        [SerializeField] private float resetDelay = 2f;
        [SerializeField] private TextMesh statusText;

        private Renderer[] renderers;
        private Collider[] colliders;
        private MaterialPropertyBlock materialPropertyBlock;
        private Camera labelCamera;
        private Color baseColor = Color.gray;
        private float currentHealth;
        private bool dead;
        private bool deathEventRaised;
        private float resetTimer;
        private float flashTimer;
        private string lastStatusText = string.Empty;

        public event Action<TargetDummyHealth, DamageResult> Damaged;
        public event Action<TargetDummyHealth> Died;

        public TargetDummyKind DummyKind => dummyKind;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => dead;
        public string LastStatusText => lastStatusText;

        private void Awake()
        {
            CacheComponents();
            ResetDummy();
        }

        private void Update()
        {
            AdvanceReset(Time.deltaTime);
            AdvanceFlash(Time.deltaTime);
            UpdateLabelFacingCamera();
        }

        public void Configure(TargetDummyKind kind)
        {
            dummyKind = kind;
            switch (kind)
            {
                case TargetDummyKind.Armored:
                    maxHealth = 150f;
                    physicalDamageMultiplier = 0.55f;
                    baseColor = new Color(0.82f, 0.66f, 0.28f, 1f);
                    break;
                case TargetDummyKind.StatusTest:
                    maxHealth = 100f;
                    physicalDamageMultiplier = 1f;
                    baseColor = new Color(0.38f, 0.74f, 0.78f, 1f);
                    break;
                default:
                    maxHealth = 100f;
                    physicalDamageMultiplier = 1f;
                    baseColor = new Color(0.64f, 0.68f, 0.72f, 1f);
                    break;
            }

            CacheComponents();
            ResetDummy();
        }

        public void SetStatusText(TextMesh text)
        {
            statusText = text;
            RefreshStatusText();
        }

        public DamageResult ApplyDamage(DamageInfo damageInfo)
        {
            if (dead || damageInfo.amount <= 0f)
            {
                return DamageResult.Ignored;
            }

            float finalDamage = damageInfo.amount;
            if (dummyKind == TargetDummyKind.Armored && damageInfo.damageType == DamageType.Physical)
            {
                finalDamage *= physicalDamageMultiplier;
            }

            currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
            if (dummyKind == TargetDummyKind.StatusTest)
            {
                lastStatusText = BuildStatusText(damageInfo);
            }

            bool killed = currentHealth <= 0f;
            DamageResult result = new DamageResult
            {
                applied = true,
                damageApplied = finalDamage,
                killedTarget = killed,
                remainingHealth = currentHealth
            };

            FlashHit();
            RefreshStatusText();
            Damaged?.Invoke(this, result);

            if (killed)
            {
                MarkDead();
            }

            return result;
        }

        public void AdvanceReset(float deltaTime)
        {
            if (!dead)
            {
                return;
            }

            resetTimer += Mathf.Max(0f, deltaTime);
            if (resetTimer >= resetDelay)
            {
                ResetDummy();
            }
        }

        public void ResetDummy()
        {
            CacheComponents();
            dead = false;
            deathEventRaised = false;
            resetTimer = 0f;
            flashTimer = 0f;
            currentHealth = maxHealth;
            lastStatusText = string.Empty;
            SetCollidersEnabled(true);
            SetRenderersEnabled(true);
            ApplyColor(baseColor);
            RefreshStatusText();
        }

        private void MarkDead()
        {
            dead = true;
            resetTimer = 0f;
            SetCollidersEnabled(false);
            SetRenderersEnabled(false);

            if (!deathEventRaised)
            {
                deathEventRaised = true;
                Died?.Invoke(this);
            }
        }

        private void CacheComponents()
        {
            renderers ??= GetComponentsInChildren<Renderer>(true);
            colliders ??= GetComponentsInChildren<Collider>(true);
            materialPropertyBlock ??= new MaterialPropertyBlock();
        }

        private void FlashHit()
        {
            flashTimer = 0.12f;
            ApplyColor(Color.white);
        }

        private void AdvanceFlash(float deltaTime)
        {
            if (dead || flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= Mathf.Max(0f, deltaTime);
            if (flashTimer <= 0f)
            {
                ApplyColor(baseColor);
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = enabled;
                }
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(enabled);
            }
        }

        private void ApplyColor(Color color)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer != null && targetRenderer.GetComponent<TextMesh>() == null)
                {
                    targetRenderer.GetPropertyBlock(materialPropertyBlock);
                    materialPropertyBlock.SetColor("_Color", color);
                    targetRenderer.SetPropertyBlock(materialPropertyBlock);
                }
            }
        }

        private void UpdateLabelFacingCamera()
        {
            if (statusText == null)
            {
                return;
            }

            if (labelCamera == null || !labelCamera.isActiveAndEnabled)
            {
                labelCamera = Camera.main;
                if (labelCamera == null)
                {
                    labelCamera = FindAnyObjectByType<Camera>();
                }
            }

            if (labelCamera == null)
            {
                return;
            }

            Vector3 awayFromCamera = statusText.transform.position - labelCamera.transform.position;
            if (awayFromCamera.sqrMagnitude <= 0.001f)
            {
                return;
            }

            statusText.transform.rotation = Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
        }

        private void RefreshStatusText()
        {
            if (statusText == null)
            {
                return;
            }

            string title = dummyKind switch
            {
                TargetDummyKind.Armored => "Armored",
                TargetDummyKind.StatusTest => "Status",
                _ => "Standard"
            };

            statusText.text = string.IsNullOrWhiteSpace(lastStatusText)
                ? $"{title}\n{currentHealth:0}/{maxHealth:0}"
                : $"{title}\n{currentHealth:0}/{maxHealth:0}\n{lastStatusText}";
        }

        private static string BuildStatusText(DamageInfo damageInfo)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(damageInfo.damageType);
            builder.Append(" / ");
            builder.Append(damageInfo.deliveryType);

            if (damageInfo.tags != null && damageInfo.tags.Length > 0)
            {
                builder.Append(" / ");
                for (int i = 0; i < damageInfo.tags.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(",");
                    }

                    builder.Append(damageInfo.tags[i]);
                }
            }

            if (damageInfo.statusChance > 0f)
            {
                builder.Append(" / Status ");
                builder.Append(Mathf.RoundToInt(damageInfo.statusChance * 100f));
                builder.Append("%");
            }

            return builder.ToString();
        }
    }
}

using System;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float deathDisableDelay = 0.45f;
        [SerializeField] private Color baseColor = new Color(0.72f, 0.28f, 0.22f, 1f);

        private Renderer[] renderers;
        private Collider[] colliders;
        private MaterialPropertyBlock propertyBlock;
        private float currentHealth;
        private float flashTimer;
        private float deathTimer;
        private bool isDead;
        private bool deathEventRaised;

        public event Action<EnemyHealth, DamageInfo, DamageResult> Damaged;
        public event Action<EnemyHealth> Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            CacheComponents();
            ResetHealth();
        }

        private void Update()
        {
            TickFlash(Time.deltaTime);
            TickDeathDisable(Time.deltaTime);
        }

        public void Configure(float health, Color color)
        {
            maxHealth = Mathf.Max(1f, health);
            baseColor = color;
            ResetHealth();
        }

        public DamageResult ApplyDamage(DamageInfo damageInfo)
        {
            if (isDead || damageInfo.amount <= 0f)
            {
                return new DamageResult
                {
                    applied = false,
                    damageApplied = 0f,
                    killedTarget = isDead,
                    remainingHealth = currentHealth
                };
            }

            float finalDamage = Mathf.Max(0f, damageInfo.amount);
            currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
            bool killed = currentHealth <= 0f;

            DamageResult result = new DamageResult
            {
                applied = true,
                damageApplied = finalDamage,
                killedTarget = killed,
                remainingHealth = currentHealth
            };

            Flash(Color.white, 0.12f);
            Damaged?.Invoke(this, damageInfo, result);

            if (killed)
            {
                MarkDead(damageInfo);
            }

            return result;
        }

        public void ResetHealth()
        {
            CacheComponents();
            currentHealth = Mathf.Max(1f, maxHealth);
            isDead = false;
            deathEventRaised = false;
            flashTimer = 0f;
            deathTimer = 0f;
            SetCollidersEnabled(true);
            SetRenderersEnabled(true);
            ApplyColor(baseColor);
        }

        internal void SetStateColor(Color color)
        {
            if (!isDead && flashTimer <= 0f)
            {
                baseColor = color;
                ApplyColor(baseColor);
            }
        }

        internal void Flash(Color color, float duration)
        {
            if (isDead)
            {
                return;
            }

            flashTimer = Mathf.Max(flashTimer, duration);
            ApplyColor(color);
        }

        private void MarkDead(DamageInfo damageInfo)
        {
            isDead = true;
            deathTimer = 0f;
            SetCollidersEnabled(false);
            ApplyColor(new Color(0.16f, 0.16f, 0.16f, 1f));

            if (!deathEventRaised)
            {
                deathEventRaised = true;
                PublishEnemyKilled(damageInfo);
                Died?.Invoke(this);
            }
        }

        private void PublishEnemyKilled(DamageInfo damageInfo)
        {
            RunState run = GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current
                : null;

            GameplayEventBus.Publish(new GameplayEvent
            {
                eventType = GameplayEventType.EnemyKilled,
                sourceObject = damageInfo.source,
                targetObject = gameObject,
                weaponId = damageInfo.weaponId,
                damageType = damageInfo.damageType.ToString(),
                deliveryType = damageInfo.deliveryType.ToString(),
                floorIndex = run != null ? run.floorIndex : 0,
                timestamp = Time.unscaledTime
            });
        }

        private void TickFlash(float deltaTime)
        {
            if (isDead || flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= Mathf.Max(0f, deltaTime);
            if (flashTimer <= 0f)
            {
                ApplyColor(baseColor);
            }
        }

        private void TickDeathDisable(float deltaTime)
        {
            if (!isDead)
            {
                return;
            }

            deathTimer += Mathf.Max(0f, deltaTime);
            if (deathTimer >= deathDisableDelay)
            {
                SetRenderersEnabled(false);
            }
        }

        private void CacheComponents()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            propertyBlock ??= new MaterialPropertyBlock();
        }

        private void SetCollidersEnabled(bool enabled)
        {
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
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = enabled;
                }
            }
        }

        private void ApplyColor(Color color)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}

using System;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityAfterHit = 0.25f;
        [SerializeField] private float hurtVolume = 0.22f;

        private AudioSource hurtAudioSource;
        private AudioClip hurtClip;
        private float currentHealth;
        private float invulnerableUntil;
        private bool isDead;
        private bool deathEventRaised;
        private bool initialized;

        public event Action<PlayerHealth, DamageResult> Damaged;
        public event Action<PlayerHealth, float> Healed;
        public event Action<PlayerHealth> Died;

        public float MaxHealth
        {
            get
            {
                EnsureInitialized();
                return maxHealth;
            }
        }

        public float CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return currentHealth;
            }
        }
        public float InvulnerableUntil => invulnerableUntil;
        public bool IsDead => isDead;

        private void Awake()
        {
            EnsureAudio();
            EnsureInitialized();
        }

        public DamageResult ApplyDamage(DamageInfo damageInfo)
        {
            return ApplyDamage(damageInfo, Time.time);
        }

        internal DamageResult ApplyDamage(DamageInfo damageInfo, float currentTime)
        {
            EnsureInitialized();
            if (isDead || damageInfo.amount <= 0f || currentTime < invulnerableUntil)
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
            invulnerableUntil = currentTime + Mathf.Max(0f, invulnerabilityAfterHit);
            bool killed = currentHealth <= 0f;

            DamageResult result = new DamageResult
            {
                applied = true,
                damageApplied = finalDamage,
                killedTarget = killed,
                remainingHealth = currentHealth
            };

            PlayHurtFeedback();
            PublishDamageTaken(damageInfo, result);
            Damaged?.Invoke(this, result);

            if (killed)
            {
                MarkDead();
            }

            return result;
        }

        public float Heal(float amount)
        {
            EnsureInitialized();
            if (isDead || amount <= 0f)
            {
                return 0f;
            }

            float before = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float healed = currentHealth - before;
            if (healed > 0f)
            {
                Healed?.Invoke(this, healed);
            }

            return healed;
        }

        public void ResetHealth()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = maxHealth;
            invulnerableUntil = 0f;
            isDead = false;
            deathEventRaised = false;
            initialized = true;
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            ResetHealth();
        }

        private void MarkDead()
        {
            isDead = true;
            if (deathEventRaised)
            {
                return;
            }

            deathEventRaised = true;
            Died?.Invoke(this);
        }

        private void PublishDamageTaken(DamageInfo damageInfo, DamageResult result)
        {
            RunState run = GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current
                : null;

            GameplayEventBus.Publish(new GameplayEvent
            {
                eventType = GameplayEventType.DamageTaken,
                sourceObject = damageInfo.source,
                targetObject = gameObject,
                weaponId = damageInfo.weaponId,
                damageType = damageInfo.damageType.ToString(),
                deliveryType = damageInfo.deliveryType.ToString(),
                amount = damageInfo.amount,
                finalAmount = result.damageApplied,
                wasCritical = damageInfo.isCritical,
                killedTarget = result.killedTarget,
                floorIndex = run != null ? run.floorIndex : 0,
                timestamp = Time.unscaledTime
            });
        }

        private void PlayHurtFeedback()
        {
            EnsureAudio();
            if (hurtAudioSource != null && hurtClip != null)
            {
                hurtAudioSource.PlayOneShot(hurtClip, 1f);
            }
        }

        private void EnsureAudio()
        {
            Transform audioTransform = transform.Find("PlayerHealthAudio");
            if (audioTransform == null)
            {
                GameObject audioObject = new GameObject("PlayerHealthAudio", typeof(AudioSource));
                audioObject.transform.SetParent(transform, false);
                audioTransform = audioObject.transform;
            }

            hurtAudioSource = audioTransform.GetComponent<AudioSource>();
            hurtAudioSource.playOnAwake = false;
            hurtAudioSource.loop = false;
            hurtAudioSource.spatialBlend = 0f;
            hurtAudioSource.volume = hurtVolume;
            hurtClip ??= CreateHurtClip();
        }

        private static AudioClip CreateHurtClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.11f;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-time / 0.045f);
                samples[i] = Mathf.Sin(time * 185f * Mathf.PI * 2f) * 0.18f * envelope;
            }

            AudioClip clip = AudioClip.Create("PlayerHurtPlaceholder", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

using System;
using UnityEngine;

namespace FrontierDepths.Core
{
    public sealed class PlayerResourceController : MonoBehaviour
    {
        public const float DefaultMaxStamina = 100f;
        public const float DefaultStaminaRegenPerSecond = 24f;
        public const float DefaultStaminaRegenDelay = 0.75f;
        public const float DefaultMaxFocus = 100f;
        public const float DefaultFocusRegenPerSecond = 8f;
        public const float DefaultMaxMana = 100f;
        public const float DefaultManaRegenPerSecond = 5f;

        [SerializeField] private float maxStamina = DefaultMaxStamina;
        [SerializeField] private float currentStamina = DefaultMaxStamina;
        [SerializeField] private float staminaRegenPerSecond = DefaultStaminaRegenPerSecond;
        [SerializeField] private float staminaRegenDelayAfterSpend = DefaultStaminaRegenDelay;
        [SerializeField] private float maxFocus = DefaultMaxFocus;
        [SerializeField] private float currentFocus = DefaultMaxFocus;
        [SerializeField] private float focusRegenPerSecond = DefaultFocusRegenPerSecond;
        [SerializeField] private float focusRegenDelayAfterSpend = DefaultStaminaRegenDelay;
        [SerializeField] private float maxMana = DefaultMaxMana;
        [SerializeField] private float currentMana = DefaultMaxMana;
        [SerializeField] private float manaRegenPerSecond = DefaultManaRegenPerSecond;
        [SerializeField] private float manaRegenDelayAfterSpend = DefaultStaminaRegenDelay;

        private float staminaRegenBlockedUntil;
        private float focusRegenBlockedUntil;
        private float manaRegenBlockedUntil;
        private string statusMessage = string.Empty;
        private float statusMessageUntil;

        public event Action ResourcesChanged;

        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float StaminaNormalized => maxStamina <= 0f ? 0f : Mathf.Clamp01(currentStamina / maxStamina);
        public float StaminaRegenPerSecond => staminaRegenPerSecond;
        public float StaminaRegenDelayAfterSpend => staminaRegenDelayAfterSpend;
        public float MaxFocus => maxFocus;
        public float CurrentFocus => currentFocus;
        public float FocusNormalized => maxFocus <= 0f ? 0f : Mathf.Clamp01(currentFocus / maxFocus);
        public float FocusRegenPerSecond => focusRegenPerSecond;
        public float FocusRegenDelayAfterSpend => focusRegenDelayAfterSpend;
        public float MaxMana => maxMana;
        public float CurrentMana => currentMana;
        public float ManaNormalized => maxMana <= 0f ? 0f : Mathf.Clamp01(currentMana / maxMana);
        public float ManaRegenPerSecond => manaRegenPerSecond;
        public string StatusMessage => Time.unscaledTime <= statusMessageUntil ? statusMessage : string.Empty;

        private void Awake()
        {
            Normalize();
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Normalize()
        {
            maxStamina = Mathf.Max(1f, maxStamina);
            currentStamina = Mathf.Clamp(currentStamina <= 0f ? maxStamina : currentStamina, 0f, maxStamina);
            staminaRegenPerSecond = Mathf.Max(0f, staminaRegenPerSecond);
            staminaRegenDelayAfterSpend = Mathf.Max(0f, staminaRegenDelayAfterSpend);
            maxFocus = Mathf.Max(1f, maxFocus);
            currentFocus = Mathf.Clamp(currentFocus <= 0f ? maxFocus : currentFocus, 0f, maxFocus);
            focusRegenPerSecond = Mathf.Max(0f, focusRegenPerSecond);
            focusRegenDelayAfterSpend = Mathf.Max(0f, focusRegenDelayAfterSpend);
            maxMana = Mathf.Max(1f, maxMana);
            currentMana = Mathf.Clamp(currentMana <= 0f ? maxMana : currentMana, 0f, maxMana);
            manaRegenPerSecond = Mathf.Max(0f, manaRegenPerSecond);
            manaRegenDelayAfterSpend = Mathf.Max(0f, manaRegenDelayAfterSpend);
        }

        public bool TrySpendStamina(float amount, string reason = "")
        {
            amount = Mathf.Max(0f, amount);
            if (amount <= 0f)
            {
                return true;
            }

            if (currentStamina + 0.001f < amount)
            {
                SetStatusMessage(string.IsNullOrWhiteSpace(reason) ? "Not enough stamina." : $"Not enough stamina for {reason}.");
                return false;
            }

            currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
            staminaRegenBlockedUntil = Time.time + staminaRegenDelayAfterSpend;
            RaiseChanged();
            return true;
        }

        public bool TrySpendFocus(float amount, string reason = "")
        {
            amount = Mathf.Max(0f, amount);
            if (amount <= 0f)
            {
                return true;
            }

            if (currentFocus + 0.001f < amount)
            {
                SetStatusMessage(string.IsNullOrWhiteSpace(reason) ? "Not enough Focus." : $"Not enough Focus for {reason}.");
                return false;
            }

            currentFocus = Mathf.Clamp(currentFocus - amount, 0f, maxFocus);
            focusRegenBlockedUntil = Time.time + focusRegenDelayAfterSpend;
            RaiseChanged();
            return true;
        }

        public bool TrySpendMana(float amount, string reason = "")
        {
            amount = Mathf.Max(0f, amount);
            if (amount <= 0f)
            {
                return true;
            }

            if (currentMana + 0.001f < amount)
            {
                SetStatusMessage(string.IsNullOrWhiteSpace(reason) ? "Not enough mana." : $"Not enough mana for {reason}.");
                return false;
            }

            currentMana = Mathf.Clamp(currentMana - amount, 0f, maxMana);
            manaRegenBlockedUntil = Time.time + manaRegenDelayAfterSpend;
            RaiseChanged();
            return true;
        }

        public float RestoreStamina(float amount)
        {
            float before = currentStamina;
            currentStamina = Mathf.Clamp(currentStamina + Mathf.Max(0f, amount), 0f, maxStamina);
            if (!Mathf.Approximately(before, currentStamina))
            {
                RaiseChanged();
            }

            return currentStamina - before;
        }

        public float RestoreFocus(float amount)
        {
            float before = currentFocus;
            currentFocus = Mathf.Clamp(currentFocus + Mathf.Max(0f, amount), 0f, maxFocus);
            if (!Mathf.Approximately(before, currentFocus))
            {
                RaiseChanged();
            }

            return currentFocus - before;
        }

        public float RestoreMana(float amount)
        {
            float before = currentMana;
            currentMana = Mathf.Clamp(currentMana + Mathf.Max(0f, amount), 0f, maxMana);
            if (!Mathf.Approximately(before, currentMana))
            {
                RaiseChanged();
            }

            return currentMana - before;
        }

        public void SetStatusMessage(string message, float seconds = 1.15f)
        {
            statusMessage = message ?? string.Empty;
            statusMessageUntil = Time.unscaledTime + Mathf.Max(0.1f, seconds);
        }

        public void SetResourceValuesForTests(float stamina, float focus, float mana = -1f)
        {
            currentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
            currentFocus = Mathf.Clamp(focus, 0f, maxFocus);
            if (mana >= 0f)
            {
                currentMana = Mathf.Clamp(mana, 0f, maxMana);
            }

            RaiseChanged();
        }

        public void TickForTests(float deltaTime, float time)
        {
            Tick(deltaTime, time);
        }

        private void Tick(float deltaTime, float time)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            bool changed = false;
            if (time >= staminaRegenBlockedUntil && currentStamina < maxStamina && staminaRegenPerSecond > 0f)
            {
                float before = currentStamina;
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSecond * deltaTime);
                changed |= !Mathf.Approximately(before, currentStamina);
            }

            if (time >= focusRegenBlockedUntil && currentFocus < maxFocus && focusRegenPerSecond > 0f)
            {
                float before = currentFocus;
                currentFocus = Mathf.Min(maxFocus, currentFocus + focusRegenPerSecond * deltaTime);
                changed |= !Mathf.Approximately(before, currentFocus);
            }

            if (time >= manaRegenBlockedUntil && currentMana < maxMana && manaRegenPerSecond > 0f)
            {
                float before = currentMana;
                currentMana = Mathf.Min(maxMana, currentMana + manaRegenPerSecond * deltaTime);
                changed |= !Mathf.Approximately(before, currentMana);
            }

            if (changed)
            {
                RaiseChanged();
            }
        }

        private void RaiseChanged()
        {
            ResourcesChanged?.Invoke();
        }
    }
}

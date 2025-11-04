using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleFPS
{
	public class UIHealth : MonoBehaviour
	{
		[Header("Health UI")]
		public TextMeshProUGUI Value;
		public Image Progress;
		public GameObject ImmortalityIndicator;
		public GameObject HealthHitTakenEffect;
		public GameObject ShildHitTakenEffect;
		public GameObject DeathEffect;
		public Animation HealthProgressAnimation;
		public TextMeshProUGUI HealValue;

		[Header("Shield UI")]
		public Image ShieldBar; // ← NEW: Reference to the shield bar image

		private int _lastHealth = -1;
		private float _lastShield = -1f;

		public void UpdateHealth(Health health)
		{
			ImmortalityIndicator.SetActive(health.IsImmortal);

			int currentHealth = Mathf.CeilToInt(health.CurrentHealth);
			bool healthDamageTaken = currentHealth < _lastHealth;
			// Only update if health changed
			if (currentHealth != _lastHealth)
			{
				Value.text = currentHealth.ToString();

				float progress = health.CurrentHealth / health.MaxHealth;
				Progress.fillAmount = progress;
				SampleHealthProgressAnimation(progress);

				if (healthDamageTaken)
				{
					// Restart hit effect
					HealthHitTakenEffect.SetActive(false);
					HealthHitTakenEffect.SetActive(true);
				}

				DeathEffect.SetActive(!health.IsAlive);
				_lastHealth = currentHealth;
			}

			// Update shield bar (even if health didn’t change)
			UpdateShield(health, healthDamageTaken);
		}

		private void UpdateShield(Health health, bool healthDamageTaken)
		{
			if (ShieldBar == null)
				return;

			float currentShield = Mathf.CeilToInt(health.CurrentShield);

			if (currentShield != _lastShield)
			{

				float progress = health.CurrentShield / health.MaxShild;
				ShieldBar.fillAmount = progress;

				if (currentShield < _lastShield && !healthDamageTaken)
				{
					// Restart hit effect
					ShildHitTakenEffect.SetActive(false);
					ShildHitTakenEffect.SetActive(true);
				}

				_lastShield = currentShield;
			}
		}

		public void ShowHeal(float value)
		{
			HealValue.text = $"+{Mathf.RoundToInt(value)} HP";

			// Restart animation
			HealValue.gameObject.SetActive(false);
			HealValue.gameObject.SetActive(true);
		}

		private void Awake()
		{
			HealthHitTakenEffect.SetActive(false);
			HealValue.gameObject.SetActive(false);

			if (ShieldBar != null)
				ShieldBar.fillAmount = 1f;
		}

		/// <summary>
		/// Coloring of the health bar is done through animation.
		/// Sample animation at correct time to achieve desired health bar state.
		/// </summary>
		private void SampleHealthProgressAnimation(float normalizedTime)
		{
			var animationState = HealthProgressAnimation[HealthProgressAnimation.clip.name];
			animationState.weight = 1f;
			animationState.enabled = true;
			animationState.normalizedTime = normalizedTime;
			HealthProgressAnimation.Sample();
			animationState.enabled = false;
		}
	}
}

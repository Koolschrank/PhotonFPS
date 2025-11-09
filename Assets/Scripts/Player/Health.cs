using Fusion;
using UnityEngine;

namespace SimpleFPS
{
	/// <summary>
	/// Stores player health, triggers heal/damage effects and informs about player death.
	/// </summary>
	public class Health : NetworkBehaviour
	{
		public Player	  Player;
		public float      MaxHealth = 100f;
		public float      MaxShild = 100f;
		public float      ShieldRechargeDelay = 4.5f;
		public float      ShieldRegenRate = 25f;
		public float      ImmortalDurationAfterSpawn = 2f;
		public GameObject ImmortalityIndicator;
		public GameObject HitEffectPrefab;

		public bool IsAlive => CurrentHealth > 0f;
		public bool IsImmortal => _immortalTimer.ExpiredOrNotRunning(Runner) == false;

		[Networked]
		public float CurrentHealth { get; private set; }

		[Networked]
		public float CurrentShield { get; private set; }

		[Networked]
		private int _hitCount { get; set; }
		[Networked]
		private Vector3 _lastHitPosition { get; set; }
		[Networked]
		private Vector3 _lastHitDirection { get; set; }
		[Networked]
		private TickTimer _immortalTimer { get; set; }

		[Networked]
		private TickTimer _shildRegenTimer { get; set; }

		private int _visibleHitCount;
		private SceneObjects _sceneObjects;

		public bool ApplyDamage(PlayerKey instigator, float damage, Vector3 position, Vector3 direction, EWeaponType weaponType, bool isCritical)
		{
			if (CurrentHealth <= 0f)
				return false;

			if (IsImmortal)
				return false;

			if (CurrentShield > 0)
				{
				// Apply damage to shield first.
				float shieldDamage = Mathf.Min(CurrentShield, damage);
				CurrentShield -= shieldDamage;
				damage -= shieldDamage;
				// Start shield regen timer.
				_shildRegenTimer = TickTimer.CreateFromSeconds(Runner, ShieldRechargeDelay);
			}


			CurrentHealth -= damage;

			if (CurrentHealth <= 0f)
			{
				CurrentHealth = 0f;

				var playerKey = new PlayerKey(Object.InputAuthority, Player.LocalIndex);
				Player.DisableFPSCamera();
				_sceneObjects.Gameplay.PlayerKilled(instigator, playerKey, weaponType, isCritical);
			}

			// Store relative hit position.
			// Only last hit is stored. For casual gameplay this is enough, no need to store precise data for each hit.
			_lastHitPosition = position - transform.position;
			_lastHitDirection = -direction;

			_hitCount++;

			return true;
		}

		public bool AddHealth(float health)
		{
			if (CurrentHealth <= 0f)
				return false;
			if (CurrentHealth >= MaxHealth)
				return false;

			CurrentHealth = Mathf.Min(CurrentHealth + health, MaxHealth);

			if (HasInputAuthority && Runner.IsForward)
			{
				// Heal effect is shown only to local player.
				// We assume the prediction will be correct most of the time so we don't need to network anything explicitly.
				_sceneObjects.GameUI.PlayerView.Health.ShowHeal(health);
			}

			return true;
		}

		public void StopImmortality()
		{
			_immortalTimer = default;
		}

		public override void Spawned()
		{
			_sceneObjects = Runner.GetSingleton<SceneObjects>();

			if (HasStateAuthority)
			{
				CurrentHealth = MaxHealth;
				CurrentShield = MaxShild;

				_immortalTimer = TickTimer.CreateFromSeconds(Runner, ImmortalDurationAfterSpawn);
			}

			_visibleHitCount = _hitCount;
		}

		public override void FixedUpdateNetwork()
		{
			// Handle shield regeneration on state authority only
			if (HasStateAuthority && CurrentHealth > 0f)
			{
				if (_shildRegenTimer.Expired(Runner))
				{
					if (CurrentShield < MaxShild)
					{
						// Regenerate over time
						CurrentShield = Mathf.Min(CurrentShield + ShieldRegenRate * Runner.DeltaTime, MaxShild);
					}
				}
			}
		}

		public override void Render()
		{
			if (_visibleHitCount < _hitCount)
			{
				// Network hit counter changed in FUN, play damage effect.
				PlayDamageEffect();
			}

			ImmortalityIndicator.SetActive(IsImmortal);

			// Sync network hit counter with local.
			_visibleHitCount = _hitCount;
		}

		private void PlayDamageEffect()
		{
			if (HitEffectPrefab != null)
			{
				var hitPosition = transform.position + _lastHitPosition;
				var hitRotation = Quaternion.LookRotation(_lastHitDirection);

				Instantiate(HitEffectPrefab, hitPosition, hitRotation);
			}
		}
	}
}

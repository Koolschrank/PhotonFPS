using Fusion;
using SimpleFPS;
using System;
using UnityEngine;


namespace SimpleFPS
{
	/// <summary>
	/// Stores player health, triggers heal/damage effects and informs about player death.
	/// </summary>
	public class Health : NetworkBehaviour
	{
		public Player	  Player;
		public HealthBlockData[] HealthBlocks;
		public float      ImmortalDurationAfterSpawn = 2f;
		public GameObject ImmortalityIndicator;
		public GameObject HitEffectPrefab;

		public bool IsAlive => currentHealthValues[0] > 0f;
		public bool IsImmortal => _immortalTimer.ExpiredOrNotRunning(Runner) == false;

		[Networked, Capacity(4)]
		public NetworkArray<float> currentHealthValues { get; }

		[Networked]
		private int _hitCount { get; set; }
		[Networked]
		private Vector3 _lastHitPosition { get; set; }
		[Networked]
		private Vector3 _lastHitDirection { get; set; }
		[Networked]
		private TickTimer _immortalTimer { get; set; }

		[Networked] private TickTimer healthBlock0Timer { get; set; }
		[Networked] private TickTimer healthBlock1Timer { get; set; }
		[Networked] private TickTimer healthBlock2Timer { get; set; }
		[Networked] private TickTimer healthBlock3Timer { get; set; }

		private TickTimer[] HealthBlockTimers => new[]
		{
		healthBlock0Timer,
		healthBlock1Timer,
		healthBlock2Timer,
		healthBlock3Timer
		};

		[Networked]
		public RagdollBulletImpact ragdollBulletImpact { get; private set; }

		private int _visibleHitCount;
		private SceneObjects _sceneObjects;

		public void ResetHealth()
		{
			if (HasStateAuthority)
			{
				for (int i = 0; i < HealthBlocks.Length; i++)
				{
					currentHealthValues.Set(i, HealthBlocks[i].startEmpty ? 0f : HealthBlocks[i].maxValue);
				}
				_immortalTimer = TickTimer.CreateFromSeconds(Runner, ImmortalDurationAfterSpawn);
			}
		}

		public bool ApplyDamage(PlayerKey instigator, float damage, DamageMaterial damageMaterial, float damageForce, Vector3 position, Vector3 direction, EWeaponType weaponType, bool isCritical)
		{
			if (!IsAlive)
				return false;

			if (IsImmortal)
				return false;

			float remainingDamage = damage;
			for (int i = HealthBlocks.Length - 1; i >= 0; i--)
			{
				float blockHealth = currentHealthValues[i];
				if (blockHealth <= 0f)
					continue;
				HealthBlockData blockData = HealthBlocks[i];
				HealthMaterial healthMaterial = blockData.healthMaterial;
				float effectiveDamage = healthMaterial.GetDamage(remainingDamage, damageMaterial);
				
				if (effectiveDamage <= blockHealth)
				{
					currentHealthValues.Set(i, blockHealth - effectiveDamage);
					remainingDamage = 0f;

					// Start recharge timer for this block and all blocks above it.
					for (int j = i; j < HealthBlocks.Length; j++)
					{
						if (HealthBlocks[j].canRecharge)
						{
							if (j == 0)
								healthBlock0Timer = TickTimer.CreateFromSeconds(Runner, HealthBlocks[j].rechargeDelay);
							else if (j == 1)
								healthBlock1Timer = TickTimer.CreateFromSeconds(Runner, HealthBlocks[j].rechargeDelay);
							else if (j == 2)
								healthBlock2Timer = TickTimer.CreateFromSeconds(Runner, HealthBlocks[j].rechargeDelay);
							else if (j == 3)
								healthBlock3Timer = TickTimer.CreateFromSeconds(Runner, HealthBlocks[j].rechargeDelay);
							
						}
					}
					break;
				}
				else
				{
					float percentage = blockHealth / effectiveDamage;
					remainingDamage -= effectiveDamage * percentage;
					currentHealthValues.Set(i, 0f);
				}
				
			}

			if (!IsAlive)
			{
				currentHealthValues.Set(0, 0f);

				var playerKey = new PlayerKey(Object.InputAuthority, Player.LocalIndex);
				_sceneObjects.Gameplay.PlayerKilled(instigator, playerKey, weaponType, isCritical);


				ragdollBulletImpact = new RagdollBulletImpact(
					position,
					direction,
					damageForce
					);
				
					
			}

			// Store relative hit position.
			// Only last hit is stored. For casual gameplay this is enough, no need to store precise data for each hit.
			_lastHitPosition = position - transform.position;
			_lastHitDirection = -direction;

			_hitCount++;

			return true;
		}

		public bool AddHealth(float health, int healthBlockIndex)
		{
			if (!IsAlive) 
				return false;

			var blockData = HealthBlocks[healthBlockIndex];
			var blockHealth = currentHealthValues.Get(healthBlockIndex);
			

			if (blockHealth >= blockData.maxValue)
				return false;

			
			blockHealth = Mathf.Min(blockHealth + health, blockData.maxValue);
			currentHealthValues.Set(healthBlockIndex, blockHealth);
			Debug.Log($"Health block {healthBlockIndex} healed by {health}. Current value: {blockHealth}");

			if (HasInputAuthority && Runner.IsForward)
			{
				// Heal effect is shown only to local player.
				// We assume the prediction will be correct most of the time so we don't need to network anything explicitly.
				_sceneObjects.Device.uiManager.playerViews[Player.LocalIndex].PlayerView.Health.ShowHeal(health);
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
				ResetHealth();
				_immortalTimer = TickTimer.CreateFromSeconds(Runner, ImmortalDurationAfterSpawn);
			}

			_visibleHitCount = _hitCount;
		}

		public override void FixedUpdateNetwork()
		{
			// Handle shield regeneration on state authority only
			if (HasStateAuthority && IsAlive)
			{
				for (int i = 0; i < HealthBlocks.Length; i++)
				{
					if (HealthBlocks[i].canRecharge && HealthBlockTimers[i].Expired(Runner))
					{
						float currentHealth = currentHealthValues[i];
						float maxHealth = HealthBlocks[i].maxValue;
						if (currentHealth < maxHealth)
						{
							AddHealth(HealthBlocks[i].rechargeSpeed * Runner.DeltaTime, i);
						}
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



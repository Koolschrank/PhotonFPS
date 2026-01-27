using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SimpleFPS
{
	public enum EShootType
	{
		Single = 0,
		Automatic = 1,
		Burst = 2,

		chargeInstantRelease = 10,
		chargeManualRelease = 11,
		chargeAutomatic = 12,

		specialMelee = 20,

	}

	public enum EWeaponType
	{
		None = 0,
		Pistol = 1,
		AssultRifle = 2,
		BattleRifle = 3,
		Shotgun = 4,
		SniperRifle = 5,
		RocketLauncher = 6,
		PlasmaRifle = 7,
		LaserGun = 8,
		DualSMG = 9,
		HeavyCrossbow = 10,

		Hammer = 20,
		GreatSword = 21,

		GrenadeLauncher = 31,
		FireGrenadeLauncher = 32,

		HealBeam = 40,
		GravityGun = 41,

	}

	public enum EWeaponThirdPersonAnimationType
	{
		None,
		Pistol = 1,
		Rifle = 2,
		Shotgun = 3,
		
	}

	/// <summary>
	/// Main script that handles all the shooting. Weapon fires hitscan projectiles that are synchronized
	/// over the networked through projectile data buffer (_projectileData array). Check Projectiles Essentials
	/// sample where the basic projectile concepts and their implementation in Fusion is explained in detail.
	/// </summary>
	public class Weapon : NetworkBehaviour
	{
		/*
		public WeaponData data;

		[Header("Visuals")]
		public Sprite      Icon;
		public string      Name;
		public EWeaponThirdPersonAnimationType ThirdPersonAnimationType;
		public RuntimeAnimatorController HandsAnimatorController;

		[Header("Sounds")]
		public AudioSource FireSound;
		public AudioSource ReloadingSound;
		public AudioSource EmptyClipSound;

		[Header("Visuals")]
		[NonSerialized] public bool firstPersonVisible = false;
		[NonSerialized] public bool thirdPersonVisible = false;
		public WeaponVisual_ThirdPerson ThirdPersonVisual;
		public WeaponVisual_FPS FirstPersonVisual;

		public bool HasAmmo => AmmoInMagazin > 0 || RemainingAmmo > 0;

		[Networked, HideInInspector]
		public PlayerKey OwnerPlayerKey { get; set; }
		[Networked, HideInInspector]
		public NetworkBool IsCollected { get; set; }
		[Networked, HideInInspector]
		public NetworkBool IsReloading { get; set; }
		[Networked, HideInInspector]
		public int AmmoInMagazin { get; set; }
		[Networked, HideInInspector]
		public int RemainingAmmo { get; set; }

		[Networked]
		private int _fireCount { get; set; }
		[Networked]
		private TickTimer _fireCooldown { get; set; }
		[Networked, Capacity(32)]
		private NetworkArray<ProjectileData> _projectileData { get; }

		private int _fireTicks;
		private int _visibleFireCount;
		private bool _reloadingVisible;
		private SceneObjects _sceneObjects;

		public bool Fire(Vector3 firePosition, Vector3 fireDirection, bool justPressed)
		{
			if (IsCollected == false)
				return false;
			if (justPressed == false && IsAutomatic == false)
				return false;
			if (IsReloading)
				return false;
			if (_fireCooldown.ExpiredOrNotRunning(Runner) == false)
				return false;

			if (AmmoInMagazin <= 0)
			{
				PlayEmptyClipSound(justPressed);
				return false;
			}

			// Random needs to be initialized with same seed on both input and
			// state authority to ensure the projectiles are fired in the same direction on both.
			UnityEngine.Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));

			for (int i = 0; i < ProjectilesPerShot; i++)
			{
				var projectileDirection = fireDirection;

				if (Dispersion > 0f)
				{
					// We use unit sphere on purpose -> non-uniform distribution (more projectiles in the center).
					var dispersionRotation = Quaternion.Euler(UnityEngine.Random.insideUnitSphere * Dispersion);
					projectileDirection = dispersionRotation * fireDirection;
				}

				FireProjectile(firePosition, projectileDirection);
			}

			_fireCooldown = TickTimer.CreateFromTicks(Runner, _fireTicks);
			AmmoInMagazin--;

			return true;
		}

		public void Reload()
		{
			if (IsCollected == false)
				return;
			if (AmmoInMagazin >= MaxClipAmmo)
				return;
			if (RemainingAmmo <= 0)
				return;
			if (IsReloading)
				return;
			if (_fireCooldown.ExpiredOrNotRunning(Runner) == false)
				return; // Fire finishing.

			IsReloading = true;
			_fireCooldown = TickTimer.CreateFromSeconds(Runner, ReloadTime);
		}

		public void AddAmmo(int amount)
		{
			RemainingAmmo += amount;
		}

		public void ToggleVisibility(bool isVisible)
		{
			gameObject.SetActive(isVisible);

			//if (_muzzleEffectInstance != null)
			//{
			//	_muzzleEffectInstance.SetActive(false);
			//}
		}

		public float GetReloadProgress()
		{
			if (IsReloading == false)
				return 1f;

			return 1f - _fireCooldown.RemainingTime(Runner).GetValueOrDefault() / ReloadTime;
		}

		public void CreateFPSVisual(int layer)
		{
			var spawnedWeaponVisual = Instantiate(FirstPersonVisual.gameObject,transform);
			firstPersonVisible = true;
			FirstPersonVisual = spawnedWeaponVisual.GetComponent<WeaponVisual_FPS>();
			FirstPersonVisual.CreateMuzzleFlash();
			LayerTools.SetLayerRecursively(FirstPersonVisual.gameObject, layer);
		}

		public void CreateThirdPersonVisual(int layer)
		{
			var spawnedWeaponVisual = Instantiate(ThirdPersonVisual.gameObject,transform);
			thirdPersonVisible = true;
			ThirdPersonVisual = spawnedWeaponVisual.GetComponent<WeaponVisual_ThirdPerson>();
			ThirdPersonVisual.CreateMuzzleFlash();
			LayerTools.SetLayerRecursively(ThirdPersonVisual.gameObject, layer);
		}

		public override void Spawned()
		{
			if (HasStateAuthority)
			{
				AmmoInMagazin = Mathf.Clamp(StartAmmo, 0, MaxClipAmmo);
				RemainingAmmo = StartAmmo - AmmoInMagazin;
			}

			_visibleFireCount = _fireCount;

			float fireTime = 60f / FireRate;
			_fireTicks = Mathf.CeilToInt(fireTime / Runner.DeltaTime);

			_sceneObjects = Runner.GetSingleton<SceneObjects>();
		}

		public void SimulateTick()
		{

		}

		public override void FixedUpdateNetwork()
		{
			if (IsCollected == false)
				return;

			if (AmmoInMagazin == 0)
			{
				// Try auto-reload.
				Reload();
			}

			if (IsReloading && _fireCooldown.ExpiredOrNotRunning(Runner))
			{
				// Reloading finished.
				IsReloading = false;

				int reloadAmmo = MaxClipAmmo - AmmoInMagazin;
				reloadAmmo = Mathf.Min(reloadAmmo, RemainingAmmo);

				AmmoInMagazin += reloadAmmo;
				RemainingAmmo -= reloadAmmo;

				// Add small prepare time after reload.
				_fireCooldown = TickTimer.CreateFromSeconds(Runner, 0.25f);
			}
		}

		public override void Render()
		{
			if (_visibleFireCount < _fireCount)
			{
				PlayFireEffect();
			}

			// Prepare projectile visuals for all projectiles that were not displayed yet.
			for (int i = _visibleFireCount; i < _fireCount; i++)
			{
				var data = _projectileData[i % _projectileData.Length];
				if (firstPersonVisible)
				{
					FirstPersonVisual.SpawnProjectile(data);
				}
				if (thirdPersonVisible)
				{
					ThirdPersonVisual.SpawnProjectile(data);
				}
			}

			_visibleFireCount = _fireCount;

			if (_reloadingVisible != IsReloading)
			{
				if (IsReloading)
				{
					if (firstPersonVisible)
						{
						FirstPersonVisual.SetAnimationTrigger("Reload");
					}
					if (thirdPersonVisible)
					{
						ThirdPersonVisual.SetAnimationTrigger("Reload");
					}
					ReloadingSound.Play();
				}

				_reloadingVisible = IsReloading;
			}
		}

		private void FireProjectile(Vector3 firePosition, Vector3 fireDirection)
		{
			var projectileData = new ProjectileData();
			
			var hitOptions = HitOptions.IncludePhysX;
			var hits = new List<LagCompensatedHit>();

			if ( 0<Runner.LagCompensation.RaycastAll(
					firePosition,
					fireDirection,
					MaxHitDistance,
					Object.InputAuthority,
					hits,
					HitMask,
					true,
					hitOptions))
			{
				LagCompensatedHit? bestValidHit = null;
				float closestDistance = float.MaxValue;

				foreach (var h in hits)
				{
					if (h.Hitbox != null)
					{
						var hitPlayer = h.Hitbox.Root.GetComponent<Player>();

						// Ignore self (same InputAuthority and same LocalIndex)
						if (hitPlayer != null &&
							hitPlayer.Object.InputAuthority == Object.InputAuthority &&
							hitPlayer.LocalIndex == OwnerPlayerKey.LocalIndex)
							continue;
					}

					// Found a valid hit, pick the closest
					if (h.Distance < closestDistance)
					{
						closestDistance = h.Distance;
						bestValidHit = h;
					}
				}

				if (bestValidHit.HasValue)
				{
					var hit = bestValidHit.Value;
					projectileData.HitPosition = hit.Point;
					projectileData.HitNormal = hit.Normal;

					if (hit.Hitbox != null)
						ApplyDamage(hit.Hitbox, hit.Point, fireDirection);
					else
						projectileData.ShowHitEffect = true;
				}
			}

			_projectileData.Set(_fireCount % _projectileData.Length, projectileData);
			_fireCount++;
		}


		/*private void FireProjectile(Vector3 firePosition, Vector3 fireDirection)
		{
			

			var projectileData = new ProjectileData();

			var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;

			// Whole projectile path and effects are immediately processed (= hitscan projectile).
			if (Runner.LagCompensation.Raycast(firePosition, fireDirection, MaxHitDistance,
				    Object.InputAuthority, out var hit, HitMask, hitOptions))
			{
				projectileData.HitPosition = hit.Point;
				projectileData.HitNormal = hit.Normal;

				if (hit.Hitbox != null)
				{
					ApplyDamage(hit.Hitbox, hit.Point, fireDirection);
				}
				else
				{
					// Hit effect is shown only when player hits solid object.
					projectileData.ShowHitEffect = true;
				}
			}

			_projectileData.Set(_fireCount % _projectileData.Length, projectileData);
			_fireCount++;
		}

		private void PlayFireEffect()
		{
			if (FireSound != null)
			{
				FireSound.PlayOneShot(FireSound.clip);
			}

			if (firstPersonVisible)
			{
				FirstPersonVisual.PlayMuzzleFlash();
				FirstPersonVisual.SetAnimationTrigger("Fire");
			}
			if (thirdPersonVisible)
			{
				ThirdPersonVisual.PlayMuzzleFlash();
				ThirdPersonVisual.SetAnimationTrigger("Fire");
			}

			GetComponentInParent<Player>().PlayFireEffect();
		}

		private void ApplyDamage(Hitbox enemyHitbox, Vector3 position, Vector3 direction)
		{
			var enemyHealth = enemyHitbox.Root.GetComponent<Health>();
			if (enemyHealth == null || enemyHealth.IsAlive == false)
				return;

			float damageMultiplier = enemyHitbox is BodyHitbox bodyHitbox ? bodyHitbox.DamageMultiplier : 1f;
			bool isCriticalHit = damageMultiplier > 1f;

			float damage = Damage * damageMultiplier;
			if (_sceneObjects.Gameplay.DoubleDamageActive)
			{
				damage *= 2f;
			}

			if (enemyHealth.ApplyDamage(OwnerPlayerKey, damage, DamageForce, position, direction, Type, isCriticalHit) == false)
				return;

			if (HasInputAuthority && Runner.IsForward)
			{
				// For local player show UI hit effect.

				_sceneObjects.Device.uiManager.playerViews[OwnerPlayerKey.LocalIndex].PlayerView.Crosshair.ShowHit(enemyHealth.IsAlive == false, isCriticalHit);
			}
		}

		private void PlayEmptyClipSound(bool fireJustPressed)
		{
			// For automatic weapons we want to play empty clip sound once after last fire.
			bool firstEmptyShot = _fireCooldown.TargetTick.GetValueOrDefault() == Runner.Tick - 1;

			if (fireJustPressed == false && firstEmptyShot == false)
				return;

			if (EmptyClipSound == null || EmptyClipSound.isPlaying)
				return;

			if (Runner.IsForward && HasInputAuthority)
			{
				EmptyClipSound.Play();
			}
		}
		*/


	}

	/// <summary>
	/// Structure representing single projectile shot.
	/// </summary>
	public struct ProjectileData : INetworkStruct
	{
		public Vector3 HitPosition;
		public Vector3 HitNormal;
		public NetworkBool ShowHitEffect;
	}
}

using Fusion;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;
using static Unity.Collections.Unicode;

namespace SimpleFPS
{
    public class WeaponFireHandler : NetworkBehaviour
	{
		public Transform firePoint;
		public Player player;

		[Networked]
		private int _fireCount { get; set; }
		[Networked, Capacity(32)]
		private NetworkArray<ProjectileData> _projectileData { get; }

		private SceneObjects _sceneObjects;

		public override void Spawned()
		{
			_sceneObjects = Runner.GetSingleton<SceneObjects>();
		}

		public void Fire(WeaponData weaponData, BulletData bulletData)
		{
			if (bulletData is HitScanData hitScanData)
			{
				ShootHitScanProjectile(weaponData, hitScanData);
			}
		}

		private void ShootHitScanProjectile(WeaponData weaponData, HitScanData bulletData)
        {
			
			var firePosition = firePoint.position;
			var fireDirection = firePoint.forward;

			var projectileData = new ProjectileData();

			var hitOptions = HitOptions.IncludePhysX;
			var hits = new List<LagCompensatedHit>();

			if (0 < Runner.LagCompensation.RaycastAll(
					firePosition,
					fireDirection,
					bulletData.MaxHitDistance,
					Object.InputAuthority,
					hits,
					bulletData.HitMask,
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
							hitPlayer.LocalIndex == player.LocalIndex)
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
						ApplyDamage(hit.Hitbox, hit.Point, fireDirection, weaponData, bulletData);
					else
						projectileData.ShowHitEffect = true;
				}
			}

			_projectileData.Set(_fireCount % _projectileData.Length, projectileData);
			_fireCount++;

		}

		private void ApplyDamage(Hitbox enemyHitbox, Vector3 position, Vector3 direction, WeaponData weaponData, HitScanData bulletData)
		{
			var enemyHealth = enemyHitbox.Root.GetComponent<Health>();
			if (enemyHealth == null || enemyHealth.IsAlive == false)
				return;

			float damageMultiplier = enemyHitbox is BodyHitbox bodyHitbox ? bodyHitbox.DamageMultiplier : 1f;
			bool isCriticalHit = damageMultiplier > 1f;

			float damage = bulletData.Damage * damageMultiplier;
			var OwnerPlayerKey = new PlayerKey(Object.InputAuthority, player.LocalIndex);


			if (enemyHealth.ApplyDamage(OwnerPlayerKey, damage, bulletData.DamageForce, position, direction, weaponData.weaponType, isCriticalHit) == false)
				return;

			if (HasInputAuthority && Runner.IsForward)
			{
				// For local player show UI hit effect.

				_sceneObjects.Device.uiManager.playerViews[OwnerPlayerKey.LocalIndex].PlayerView.Crosshair.ShowHit(enemyHealth.IsAlive == false, isCriticalHit);
			}
		}

	}
}

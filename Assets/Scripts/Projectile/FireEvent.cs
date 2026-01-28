using UnityEngine;

namespace SimpleFPS
{
	public struct FireEvent
	{
		// Always valid
		public Vector3 FirePosition;
		public Vector3 FireDirection;

		// Weapon type
		public bool IsProjectile;

		// Hitscan result
		public bool HasHit;
		public Vector3 HitPosition;
		public Vector3 HitNormal;

		// Projectile result
		//public ProjectileSimulation Projectile;
	}

}

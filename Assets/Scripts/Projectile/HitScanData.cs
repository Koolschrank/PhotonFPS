using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "HitScanData", menuName = "Projectile/HitScanData", order = 1)]
	public class HitScanData : BulletData
    {
		public float Damage = 10f;
		public DamageMaterial DamageMaterial;
		public float DamageForce = 100f;
		public LayerMask HitMask;
		public float MaxHitDistance = 100f;
		[Range(1, 20)]
		public int ProjectilesPerShot = 1;
	}
}

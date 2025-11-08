using UnityEngine;
using UnityEngine.Serialization;

namespace SimpleFPS
{
    public class WeaponVisual : MonoBehaviour
    {

		public Transform Pivot;
		public Transform LeftHandHandle;
		public Animator WeaponAnimator;

		[Header("Fire Effect")]
		[FormerlySerializedAs("MuzzleTransform")]
		public Transform MuzzleTransform;
		public GameObject MuzzleEffectPrefab;
		public ProjectileVisual ProjectileVisualPrefab;

		public GameObject _muzzleEffectInstance;


		public void CreateMuzzleFlash()
		{
			if (MuzzleEffectPrefab != null)
			{
				_muzzleEffectInstance = Instantiate(MuzzleEffectPrefab, MuzzleTransform);
				_muzzleEffectInstance.SetActive(false);

				LayerTools.SetLayerRecursively(_muzzleEffectInstance, gameObject.layer);
			}
		}

		public void PlayMuzzleFlash()
		{
			_muzzleEffectInstance.SetActive(false);
			_muzzleEffectInstance.SetActive(true);
		}

		public void SetAnimationTrigger(string animation)
			{
			WeaponAnimator.SetTrigger(animation);
		}

		public void SpawnProjectile(ProjectileData data)
		{
			var projectileVisual = Instantiate(ProjectileVisualPrefab, MuzzleTransform.position, MuzzleTransform.rotation);
			projectileVisual.SetHit(data.HitPosition, data.HitNormal, data.ShowHitEffect);
			LayerTools.SetLayerRecursively(projectileVisual.gameObject, gameObject.layer);
		}
	}
}

using UnityEngine;

namespace SimpleFPS
{
	public class WeaponVisual : MonoBehaviour
	{
		[Header("General")]
		public WeaponData Data;
		public Animator WeaponAnimator;

		[Header("Hands / IK")]
		public Transform Pivot;
		public Transform LeftHandHandle;

		[Header("Muzzle")]
		public Transform MuzzleTransform;
		public GameObject MuzzleFlashPrefab;

		[Header("Hitscan Visuals")]
		public HitscanProjectileVisual HitscanVisual;
		public GameObject HitscanBulletTrailPrefab;
		public GameObject HitscanImpactPrefab;

		[Header("Projectile Visuals")]
		public ProjectileVisualBase ProjectileVisualFP;
		public ProjectileVisualBase ProjectileVisualTP;

		[Header("Projectile Convergence")]
		[Range(0.01f, 0.3f)]
		public float ConvergeTimeFP = 0.1f;

		[Range(0.01f, 0.3f)]
		public float ConvergeTimeTP = 0.05f;

		GameObject _muzzleFlashInstance;

		// ----------------------------------------------------
		// Initialization
		// ----------------------------------------------------

		public void Initialize(int visualLayer)
		{
			CreateMuzzleFlash(visualLayer);
		}

		void CreateMuzzleFlash(int layer)
		{
			if (MuzzleFlashPrefab == null)
				return;

			_muzzleFlashInstance = Instantiate(
				MuzzleFlashPrefab,
				MuzzleTransform
			);

			_muzzleFlashInstance.SetActive(false);
			LayerTools.SetLayerRecursively(_muzzleFlashInstance, layer);
		}

		// ----------------------------------------------------
		// Fire Event Reaction
		// ----------------------------------------------------

		public void OnFire(
			FireEvent fireEvent//,
			//bool isLocalPlayer
		)
		{
			PlayMuzzleFlash();
			PlayFireAnimation();

			if (fireEvent.IsProjectile)
			{
				/*
				SpawnProjectileVisuals(
					fireEvent.Projectile,
					isLocalPlayer
				);*/
			}
			else
			{
				SpawnHitscanVisuals(fireEvent);
			}
		}

		// ----------------------------------------------------
		// Visuals
		// ----------------------------------------------------

		void PlayMuzzleFlash()
		{
			if (_muzzleFlashInstance == null)
				return;

			_muzzleFlashInstance.SetActive(false);
			_muzzleFlashInstance.SetActive(true);
		}

		void PlayFireAnimation()
		{
			if (WeaponAnimator == null)
				return;

			WeaponAnimator.SetTrigger("Fire");
		}

		// ----------------------------------------------------
		// Hitscan
		// ----------------------------------------------------

		void SpawnHitscanVisuals(FireEvent fireEvent)
		{
			var bullet = Instantiate(
				HitscanVisual,
				MuzzleTransform.position,
				MuzzleTransform.rotation
			);

			LayerTools.SetLayerRecursively(bullet.gameObject, gameObject.layer);
			bullet.SetUp(fireEvent);
		}

		// ----------------------------------------------------
		// Projectile
		// ----------------------------------------------------

		/*
		void SpawnProjectileVisuals(
			ProjectileSimulation projectile,
			bool isLocalPlayer
		)
		{
			if (projectile == null)
				return;

			// First-person visual (local only)
			if (isLocalPlayer && ProjectileVisualFP != null)
			{
				var fp = Instantiate(ProjectileVisualFP);
				fp.Init(
					projectile,
					MuzzleTransform.position,
					ConvergeTimeFP
				);

				LayerTools.SetLayerRecursively(
					fp.gameObject,
					gameObject.layer
				);
			}

			// Third-person visual (everyone)
			if (ProjectileVisualTP != null)
			{
				var tp = Instantiate(ProjectileVisualTP);
				tp.Init(
					projectile,
					MuzzleTransform.position,
					ConvergeTimeTP
				);
			}
		}*/
	}
}

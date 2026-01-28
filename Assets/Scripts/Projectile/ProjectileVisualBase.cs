using UnityEngine;

namespace SimpleFPS
{
	public abstract class ProjectileVisualBase : MonoBehaviour
	{
		/*
		[Header("Follow Settings")]
		[Tooltip("Additional visual offset (optional)")]
		public Vector3 VisualOffset;

		[Tooltip("How fast the visual interpolates when not converging")]
		public float FollowSmoothing = 20f;

		protected ProjectileSimulation _projectile;
		protected Vector3 _initialOffset;
		protected float _convergeTime;
		protected float _elapsed;

		bool _initialized;

		// ----------------------------------------------------
		// Initialization
		// ----------------------------------------------------

		public void Init(
			ProjectileSimulation projectile,
			Vector3 spawnPosition,
			float convergeTime
		)
		{
			_projectile = projectile;
			_convergeTime = Mathf.Max(0.001f, convergeTime);
			_elapsed = 0f;

			// Offset between fake spawn and real projectile
			_initialOffset = spawnPosition - projectile.Position;

			transform.position = spawnPosition;
			_initialized = true;
		}

		// ----------------------------------------------------
		// Update
		// ----------------------------------------------------

		protected virtual void Update()
		{
			if (!_initialized || _projectile == null)
			{
				Destroy(gameObject);
				return;
			}

			_elapsed += Time.deltaTime;

			// Converge offset to zero
			float t = Mathf.Clamp01(_elapsed / _convergeTime);
			Vector3 convergeOffset = Vector3.Lerp(
				_initialOffset,
				Vector3.zero,
				t
			);

			// Target position
			Vector3 targetPos =
				_projectile.Position +
				convergeOffset +
				VisualOffset;

			// Smooth follow
			transform.position = Vector3.Lerp(
				transform.position,
				targetPos,
				FollowSmoothing * Time.deltaTime
			);

			// Optional orientation
			AlignRotation();
		}

		// ----------------------------------------------------
		// Rotation
		// ----------------------------------------------------

		protected virtual void AlignRotation()
		{
			Vector3 velocity = _projectile.Velocity;
			if (velocity.sqrMagnitude > 0.001f)
			{
				transform.rotation = Quaternion.LookRotation(velocity);
			}
		}

		// ----------------------------------------------------
		// Cleanup
		// ----------------------------------------------------

		public virtual void OnProjectileImpact()
		{
			Destroy(gameObject);
		}*/
	}
}

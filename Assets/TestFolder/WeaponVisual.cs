using UnityEngine;
using UnityEngine.Serialization;

namespace SimpleFPS
{
    public class WeaponVisual : MonoBehaviour
    {

		public Transform Pivot;
		public Transform LeftHandHandle;

		[Header("Fire Effect")]
		[FormerlySerializedAs("MuzzleTransform")]
		public Transform MuzzleTransform;
		public GameObject MuzzleEffectPrefab;
		public ProjectileVisual ProjectileVisualPrefab;
	}
}

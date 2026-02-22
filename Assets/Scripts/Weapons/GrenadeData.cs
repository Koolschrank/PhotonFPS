using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "GrenadeData", menuName = "Weapons/GrenadeData", order = 2)]
	public class GrenadeData : ScriptableObject
    {
		public EGrenadeType grenadeType;
		public GranadeProjectile granadeProjectile;
        public GameObject granadeVisual;
        
        public float throwDelay = 0.5f;
        public float throwStateDuration = 1f;
		public float throwForce = 10f;
        public float throwArc = 1f;
	}
}

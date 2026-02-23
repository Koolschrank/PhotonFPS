using UnityEngine;

namespace SimpleFPS
{
	[CreateAssetMenu(fileName = "HealthBlockData", menuName = "Health/HealthBlockData", order = 0)]
	public class HealthBlockData : ScriptableObject
    {
		public int maxValue;
		public HealthMaterial healthMaterial;
		public bool startEmpty;
		public bool canHeadshot;
		public bool canRecharge;
		public float rechargeDelay;
		public float rechargeSpeed;
	}
}

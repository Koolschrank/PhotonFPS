using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "DamageMaterial", menuName = "SimpleFPS/DamageMaterial", order = 1)]
	public class DamageMaterial : ScriptableObject
    {
        public float againstPlayerBody = 1f;
        public float againstFlesh = 1f;
        public float againstEnergyShield = 1f;
        public float againstHeavyEnergyShield = 1f;
        public float againstArmor = 1f;
        public float againstHeavyArmor = 1f;
	}
}

using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "HealthMaterial", menuName = "health/HealthMaterial", order = 0)]
	public class HealthMaterial : ScriptableObject
    {
        public float GetDamage(float damage, DamageMaterial damageMaterial)
        {
            damage *= DamageMultiplier;
            float finalDamage = 0f;
            finalDamage += damage * PlayerBody * damageMaterial.againstPlayerBody;
            finalDamage += damage * Flesh * damageMaterial.againstFlesh; 
            finalDamage += damage * EnergyShild * damageMaterial.againstEnergyShield;
            finalDamage += damage * HeavyEnergyShild * damageMaterial.againstHeavyEnergyShield;
            finalDamage += damage * Armor * damageMaterial.againstArmor;
            finalDamage += damage * HeavyArmor * damageMaterial.againstHeavyArmor;
            return finalDamage;
		}

		[Range(0f, 1f)]
        public float DamageMultiplier = 1f;
        [Range(0f, 1f)]
        public float PlayerBody = 1f;
        [Range(0f, 1f)]
        public float Flesh = 0f;
        [Range(0f, 1f)]
        public float EnergyShild = 0f;
        [Range(0f, 1f)]
        public float HeavyEnergyShild = 0f;
        [Range(0f, 1f)]
        public float Armor = 0f;
        [Range(0f, 1f)]
        public float HeavyArmor = 0f;
	}

    public enum EHealthMaterial
    {
        PlayerBody = 0,
        Flesh = 1,
		EnergyShild = 2,
		HeavyEnergyShild = 3,
		Armor = 4,
        HeavyArmor = 5,
	}


}

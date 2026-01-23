using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData", order = 1)]
	public class WeaponData : ScriptableObject
    {
        public EWeaponType weaponType;
        public GameObject weaponPrefab;
        public GameObject weaponPickUp;
	}
}

using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "PlayerEquipment", menuName = "Player Equipment", order = 1)]
	public class PlayerEquipment : ScriptableObject
    {
        public EWeaponType weaponInHand;
        [Range(1,10)]
        public int weaponInHandMagazins;

        public EWeaponType secondaryWeapon;
        [Range(1, 10)]
        public int secondaryWeaponMagazins;
	}
}

using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "PlayerEquipment", menuName = "Player Equipment", order = 1)]
	public class PlayerEquipment : ScriptableObject
    {
        public EWeaponType weaponInHand;
        public int weaponInHandMagazins;

        public EWeaponType secondaryWeapon;
        public int secondaryWeaponMagazins;
	}
}

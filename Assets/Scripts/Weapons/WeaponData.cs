using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData", order = 1)]
	public class WeaponData : ScriptableObject
    {
        public EWeaponType weaponType;

		public WeaponVisualFirstPerson weaponVisualFirstPerson;
		public WeaponVisualThirdPerson weaponVisualThirdPerson;
		public WeaponPickup weaponPickUp;
		
		[Header("Fire stats")]
		public BulletData bulletData;
		public EShootType shootType;
		public int ShotsPerSecond = 2;
		
		public float Dispersion = 0f;
		

		[Header("Ammo")]
		public int MagazinSize = 12;
		public int StartAmmo = 25;
		public float ReloadTime = 2f;
		[Range(0f, 1f)]
		public float ReloadFillAt = 0.7f;

		[Header("other")]
		public float switchOutTime = 0.5f;
		public float switchInTime = 0.5f;

		[Header("UI")]
		public Sprite weaponIcon;
		public string weaponName;
	}
}

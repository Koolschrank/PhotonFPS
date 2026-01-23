using UnityEngine;

namespace SimpleFPS
{
    public static class WeaponDatabase
    {
		public static WeaponDataList weaponList;

		public static WeaponData GetWeaponData(EWeaponType type)
		{
			return weaponList.GetWeaponData(type);
		}

		[RuntimeInitializeOnLoadMethod]
		static void Init()
		{
			weaponList = Resources.Load<WeaponDataList>("WeaponDataList");
		}
	}
}



using System.Collections.Generic;
using UnityEngine;

namespace SimpleFPS
{
	[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponDataList", order = 1)]
	public class WeaponDataList : ScriptableObject
	{
		public List<WeaponData> weapons;

		public WeaponData GetWeaponData(EWeaponType	type)
			{
			foreach (var weaponData in weapons)
			{
				if (weaponData.weaponType == type)
					return weaponData;
			}
			Debug.LogError($"WeaponDataList: No weapon data found for type {type}");
			return null;
		}
	}
}

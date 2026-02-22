using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SimpleFPS
{
	[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponDataList", order = 1)]
	public class WeaponDataList : ScriptableObject
	{
		public List<WeaponData> weapons;
		public List<GrenadeData> grenades;
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

		public GrenadeData GetGrenadeData(EGrenadeType type)
		{
			foreach (var grenadeData in grenades)
			{
				if (grenadeData.grenadeType == type)
					return grenadeData;
			}
			Debug.LogError($"WeaponDataList: No grenade data found for type {type}");
			return null;
		}
	}

	public enum EShootType
	{
		Single = 0,
		Automatic = 1,
		Burst = 2,

		chargeInstantRelease = 10,
		chargeManualRelease = 11,
		chargeAutomatic = 12,

		specialMelee = 20,

	}

	public enum EWeaponType
	{
		None = 0,
		Pistol = 1,
		AssultRifle = 2,
		BattleRifle = 3,
		Shotgun = 4,
		SniperRifle = 5,
		RocketLauncher = 6,
		PlasmaRifle = 7,
		LaserGun = 8,
		DualSMG = 9,
		HeavyCrossbow = 10,

		Hammer = 20,
		GreatSword = 21,

		GrenadeLauncher = 31,
		FireGrenadeLauncher = 32,

		HealBeam = 40,
		GravityGun = 41,

	}

	public enum EWeaponThirdPersonAnimationType
	{
		None = 0,
		Pistol = 1,
		Rifle = 2,
		Shotgun = 3,

	}

	public enum EGrenadeType
	{
		FragGrenade = 0,
		PlasmaGrenade = 1,
		FireGrenade = 2,
	}

	[Serializable]
	public struct WeaponState : INetworkStruct
	{
		public EWeaponType WeaponType;  // store enum as byte
		public int AmmoInMagazin;
		public int AmmoReserve;
		public float Util; // zoom level, charge level, etc.

	}
}

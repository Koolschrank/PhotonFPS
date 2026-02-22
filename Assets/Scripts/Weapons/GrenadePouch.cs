using System;
using UnityEngine;

namespace SimpleFPS
{
    [CreateAssetMenu(fileName = "GrenadePouch", menuName = "ScriptableObjects/GrenadePouch", order = 1)]
	public class GrenadePouch : ScriptableObject
    {
		public GrenadeSlot[] grenadeSlots;

		public int GetMaxGrenadesOfType(EGrenadeType type)
		{
			foreach (var slot in grenadeSlots)
			{
				if (slot.grenadeType == type)
					return slot.maxGrenades;
			}
			Debug.LogError($"GrenadePouch: No grenade slot found for type {type}");
			return 0;
		}


		[Serializable]
		public struct GrenadeSlot
		{
			public EGrenadeType grenadeType;
			public byte maxGrenades;
			public byte startingGrenades;
		}
	}

    
}

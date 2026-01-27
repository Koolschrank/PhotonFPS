using System;
using Fusion;
using NUnit.Framework;
using UnityEngine;

namespace SimpleFPS
{
	public struct WeaponState : INetworkStruct
	{
		public EWeaponType WeaponType;
		public int AmmoInMagazin;
		public int AmmoReserve;
		public float Util;       // meaning depends on weapon like zoom level for sniper, charge level for charge weapon etc.
	}


	/// <summary>
	/// Weapons component hold references to all player weapons
	/// and allows for weapon actions such as Fire or Reload.
	/// </summary>
	public class Weapons : NetworkBehaviour
	{
		[Header("Setup")]
		public Player player;
		public Transform FireTransform;
		public WeaponFireHandler WeaponFireHandler;
		public Setup FirstPersonSetup;
		public Setup ThirdPersonSetup;
		public float WeaponSwitchTime = 1f;
		


		[Header("Sounds")]
		public AudioSource SwitchSound;

		[Networked, Capacity(2)]
		public NetworkArray<WeaponState> WeaponsOwned { get; }

		[Networked]
		public int ActiveWeaponSlot { get; set; }

		[Networked] private TickTimer _fireCooldown { get; set; }
		[Networked] private TickTimer _reloadCooldown { get; set; }
		[Networked] private bool _reloadAmmoApplied { get; set; }
		[Networked] private TickTimer _switchTimer { get; set; }
		[Networked] private bool _switchApplied { get; set; }
		[Networked] private TickTimer _granadeThrowTimer { get; set; }
		[Networked] private TickTimer _inMeleeTimer { get; set; }

		public WeaponState ActiveWeapon => WeaponsOwned[ActiveWeaponSlot];

		public WeaponState BackWeapon{
			get
			{
				int otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
				return WeaponsOwned[otherWeaponSlot];
			}
		}

		public bool InFireCooldown => _fireCooldown.ExpiredOrNotRunning(Runner) == false;
		public float? FireCooldownRemaining => _fireCooldown.RemainingTime(Runner);

		private int fireCooldownInTicks;
		public bool InReloadCooldown => _reloadCooldown.ExpiredOrNotRunning(Runner) == false;
		public bool IsSwitching => _switchTimer.ExpiredOrNotRunning(Runner) == false;
		public bool InGranadeThrowCooldown => _granadeThrowTimer.ExpiredOrNotRunning(Runner) == false;
		public bool InMeleeCooldown => _inMeleeTimer.ExpiredOrNotRunning(Runner) == false;

		public bool _firstPersonActive;
		public bool _thirdPersonActive;

		[HideInInspector]
		public Granade[] AllGranades;

		[Networked, HideInInspector]
		public Granade CurrentGranade { get; set; }


		public Transform firstPersonWeaponParent;
		private WeaponVisualFirstPerson firstPersonWeaponVisual;
		public Transform thirdPersonWeaponParent;
		private WeaponVisualThirdPerson thirdPersonWeaponVisual;
		public Transform backWeaponParent;
		private WeaponVisualThirdPerson weaponOnBackVisual;


		
		

		/*

		[Networked, HideInInspector]
		public Weapon CurrentWeapon { get; set; }

		[Networked, Capacity(2)]
		public NetworkArray<NetworkObject> WeaponsInBack { get; }

		

		[Networked]
		private Weapon _pendingWeapon { get; set; }

		private Weapon _visibleWeapon;*/

		public void ProcessInput(NetworkButtons _previousButtons, NetworkedInputPlayer input)
		{
			bool throwGranadePressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Granade);
			bool firePressed = input.Buttons.IsSet(EInputButton.Fire);
			bool fireJustPressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Fire);
			bool reloadPressed = input.Buttons.IsSet(EInputButton.Reload);
			bool meleePressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Melee);
			bool switchWeaponPressed = input.Buttons.WasPressed(_previousButtons, EInputButton.SwitchWeapon);

			if (IsSwitching) return;
			if (InGranadeThrowCooldown) return;
			if (InMeleeCooldown) return;

			if (meleePressed)
			{
				// cancel reloading if releoading
				// perform melee attack
			}
			else if (throwGranadePressed)
			{
				// cancel reloading if releoading
				ThrowGranade();
			}
			else if (switchWeaponPressed)
			{
				// cancel reloading if releoading
				SwitchWeapon();
			}
			else if (reloadPressed)
			{
				EndReload();
			}
			else if (input.Buttons.IsSet(EInputButton.Fire))
			{
				bool justPressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Fire);
				TryFire(justPressed);
				//Health.StopImmortality();
			}
		}

		public void SimulateTick()
		{
			SimulateRelaod();
			SimulateSwitchWeapon();
		}

		public void SimulateRelaod()
		{
			if (InReloadCooldown)
			{
				if (_reloadAmmoApplied == false)
				{
					var weaponData = WeaponDatabase.weaponList.GetWeaponData(WeaponsOwned[ActiveWeaponSlot].WeaponType);
					float reloadFillTime = weaponData.ReloadTime * weaponData.ReloadFillAt;
					if (_reloadCooldown.RemainingTime(Runner) <= (weaponData.ReloadTime - reloadFillTime))
					{
						Reload();
					}
				}
				if (_reloadCooldown.Expired(Runner))
				{
					EndReload();
				}
			}
		}

		public void SimulateSwitchWeapon()
		{
			if (IsSwitching)
			{
				if (_switchApplied == false)
				{
					var otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
					var otherWeaponData = WeaponsOwned[otherWeaponSlot];
					float switchInTime = WeaponDatabase.weaponList.GetWeaponData(otherWeaponData.WeaponType).switchInTime;
					if (_switchTimer.RemainingTime(Runner) <= switchInTime)
					{
						SwitchWeapon();
					}
				}
				if (_switchTimer.Expired(Runner))
				{
					EndSwitchWeapon();
				}
			}
		}



		public void SetFirstPersonLayer(int layer)
		{
			FirstPersonSetup.WeaponLayer = layer;
			if (firstPersonWeaponVisual != null)
			{
				LayerTools.SetLayerRecursively(firstPersonWeaponVisual.gameObject, layer);
			}
		}

		public void SetThirdPersonLayer(int layer)
		{
			ThirdPersonSetup.WeaponLayer = layer;
			if (thirdPersonWeaponVisual != null)
			{
				LayerTools.SetLayerRecursively(thirdPersonWeaponVisual.gameObject, layer);
			}
			if (weaponOnBackVisual != null)
			{
				LayerTools.SetLayerRecursively(weaponOnBackVisual.gameObject, layer);
			}
		}

		public void SetThirdPersonVisuals(bool thirdPerson)
		{
			if (thirdPerson == _thirdPersonActive)
				return;

			_thirdPersonActive = thirdPerson;
		}

		public void SetFirstPersonVisuals(bool firstPerson)
		{
			if (firstPerson == _firstPersonActive)
				return;

			_firstPersonActive = firstPerson;

			//for (int i = 0; i < AllWeapons.Length; i++)
			//{
			//	// First person weapons are rendered with a different (overlay) camera
			//	// to prevent clipping through geometry.
			//	AllWeapons[i].gameObject.SetLayer(_activeSetup.WeaponLayer, true);
			//}
		}

		public void ThrowGranade()
		{
			if (CurrentGranade == null || IsSwitching)
				return;

			if (CurrentGranade.Throw(FireTransform.position, FireTransform.forward) == false)
				return;
		}

		public void TryFire(bool justPressed)
		{
			var isShootCooldownOver = !InFireCooldown;
			if (!isShootCooldownOver)
				return;

			if (ActiveWeaponSlot < 0 || ActiveWeaponSlot >= WeaponsOwned.Length || WeaponsOwned[ActiveWeaponSlot].WeaponType == 0)
				return;

			

			var weaponData = WeaponDatabase.weaponList.GetWeaponData(WeaponsOwned[ActiveWeaponSlot].WeaponType);
			var shootType = weaponData.shootType;
			

			switch (shootType)
			{
				case EShootType.Single:
					if (justPressed)
						Fire();
					return;
				case EShootType.Burst:
				case EShootType.Automatic:
					Fire();
					return;
				
			}
			
		}

		public void Fire()
		{
			var weaponData = WeaponDatabase.weaponList.GetWeaponData(WeaponsOwned[ActiveWeaponSlot].WeaponType);
			var bulletData = weaponData.bulletData;
			WeaponFireHandler.Fire(weaponData, bulletData);

			

			_fireCooldown = TickTimer.CreateFromSeconds(Runner, fireCooldownInTicks);

			if (_firstPersonActive && Runner.IsForward)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.Fire);
			}
		}

		public void TryStartReload()
		{
			var weaponData = WeaponDatabase.weaponList.GetWeaponData(WeaponsOwned[ActiveWeaponSlot].WeaponType);
			var weaponState = WeaponsOwned[ActiveWeaponSlot];
			bool isMagazinFull = weaponState.AmmoInMagazin >= weaponData.MagazinSize;
			bool hasReserveAmmo = weaponState.AmmoReserve > 0;
			if (isMagazinFull || !hasReserveAmmo)
				return;

			StartReload();
		}

		public void StartReload()
		{
			_reloadAmmoApplied = false;
			var weaponData = WeaponDatabase.weaponList.GetWeaponData(WeaponsOwned[ActiveWeaponSlot].WeaponType);
			_reloadCooldown = TickTimer.CreateFromSeconds(Runner, weaponData.ReloadTime);
			if (_firstPersonActive && Runner.IsForward)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.IsReloading);
			}
		}

		public void Reload()
		{
			_reloadAmmoApplied = true;
			var weaponData = WeaponDatabase.weaponList.GetWeaponData(WeaponsOwned[ActiveWeaponSlot].WeaponType);
			var weaponState = WeaponsOwned[ActiveWeaponSlot];
			int ammoNeeded = weaponData.MagazinSize - weaponState.AmmoInMagazin;
			int ammoToLoad = Math.Min(ammoNeeded, weaponState.AmmoReserve);
			weaponState.AmmoInMagazin += ammoToLoad;
			weaponState.AmmoReserve -= ammoToLoad;
			WeaponsOwned.Set(ActiveWeaponSlot, weaponState);
		}

		public void EndReload()
		{
			_reloadCooldown = TickTimer.None;
		}

		public void CancelReload()
		{
			_reloadCooldown = TickTimer.None;
		}

		public void DropWeapon()
		{
			WeaponsOwned.Set(ActiveWeaponSlot, new WeaponState { WeaponType = EWeaponType.None, AmmoInMagazin = 0, AmmoReserve = 0, Util = 0f });
		}

		public void TryStartSwitchWeapon()
		{
			int otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
			var otherWeaponData = WeaponsOwned[otherWeaponSlot];
			if (otherWeaponData.WeaponType == EWeaponType.None)
				return;
		}

		public void StartSwitchWeapon()
		{
			var currentWeaponData = WeaponsOwned[ActiveWeaponSlot];
			int otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
			var otherWeaponData = WeaponsOwned[otherWeaponSlot];
			float switchTime = 
				WeaponDatabase.weaponList.GetWeaponData(currentWeaponData.WeaponType).switchOutTime
				+ WeaponDatabase.weaponList.GetWeaponData(otherWeaponData.WeaponType).switchInTime;
			_switchTimer = TickTimer.CreateFromSeconds(Runner, switchTime);
			_switchApplied = false;

			// For local player start with switch animation but only
			// in forward tick as starting animation multiple times
			// during resimulations is not desired.
			if (_firstPersonActive && Runner.IsForward)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.Hide);
				SwitchSound.Play();
			}
		}

		public void SwitchWeapon()
		{
			int otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
			EquipWeapon(otherWeaponSlot);
			_switchApplied = true;


		}

		public void EndSwitchWeapon()
		{
			_switchTimer = TickTimer.None;
		}

		/*
		public void SwitchWeapon_Old()
		{
			var newWeapon = GetWeaponInBack();

			if (newWeapon == null || newWeapon.IsCollected == false)
				return;

			if (newWeapon == CurrentWeapon && _pendingWeapon == null)
				return;

			if (newWeapon == _pendingWeapon)
				return;

			if (CurrentWeapon.IsReloading)
				return;

			_pendingWeapon = newWeapon;
			_switchTimer = TickTimer.CreateFromSeconds(Runner, WeaponSwitchTime);

			
		}*/

		public void EquipWeapon(int weaponIndex)
		{
			ActiveWeaponSlot = weaponIndex;
			var weaponState = WeaponsOwned[ActiveWeaponSlot];


			// start switch timer if not already started, that can happen when picking up a weapon
			if (_switchTimer.ExpiredOrNotRunning(Runner))
			{
				var weaponData = WeaponDatabase.weaponList.GetWeaponData(weaponState.WeaponType);
				_switchTimer = TickTimer.CreateFromSeconds(Runner, weaponData.switchInTime);
			}

			// calculate fire cooldown in ticks
			var weaponFireRate = WeaponDatabase.weaponList.GetWeaponData(weaponState.WeaponType).FireRate;
			float fireTime = 60f / weaponFireRate;
			fireCooldownInTicks = Mathf.CeilToInt(fireTime / Runner.DeltaTime);
		}

		public void PickupWeapon(WeaponState newWeapon)
		{
			var weaponType = newWeapon.WeaponType;
			var weaponData = WeaponDatabase.weaponList.GetWeaponData(weaponType);
			int otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
			bool isOtherWeaponSlotFree = WeaponsOwned[otherWeaponSlot].WeaponType == EWeaponType.None;

			if (isOtherWeaponSlotFree)
			{
				WeaponsOwned.Set(otherWeaponSlot, newWeapon);
				TryStartSwitchWeapon();
			}
			else
			{
				if (WeaponsOwned[ActiveWeaponSlot].WeaponType != EWeaponType.None)
				{
					DropWeapon();
				}
				WeaponsOwned.Set(ActiveWeaponSlot, newWeapon);
				EquipWeapon(ActiveWeaponSlot);
			}

			/*
			var weaponObject = Runner.Spawn(
				weaponData.weaponPrefab,
				WeaponParentObject.position,
				WeaponParentObject.rotation,
				player.Object.InputAuthority,
				onBeforeSpawned: (runner, newObj) =>
				{
					if (newObj.TryGetComponent(out Weapon w))
					{
						w.AmmoInMagazin = ammoInMagazin;
						w.RemainingAmmo = ammoInReserve;
					}
				});*/

			


		}

		

		private void Awake()
		{
			// All weapons are already present inside Player prefab.
			// This is the simplest solution when only few weapons are available in the game.
			
			AllGranades = GetComponentsInChildren<Granade>();
		}

		public override void Spawned()
		{
			if (HasStateAuthority)
			{
				//CurrentWeapon = AllWeapons[0];
				//CurrentWeapon.IsCollected = true;

				CurrentGranade = AllGranades[0];
			}
		}

		public override void Render()
		{
			UpdateFirstPersonWeapon();

			/*
			if (_firstPersonActive && CurrentWeapon != null)
			{
				FirstPersonSetup.Animator.SetBool(AnimatorId.IsReloading, CurrentWeapon.IsReloading);
			}*/
		}

		private void UpdateFirstPersonWeapon()
		{
			if (!_firstPersonActive) return;

			bool hasWeaponInHand = WeaponsOwned[ActiveWeaponSlot].WeaponType != EWeaponType.None;
			if (!hasWeaponInHand)
			{
				return;
			}
			var currentWeaponState = WeaponsOwned[ActiveWeaponSlot];

			if (firstPersonWeaponVisual == null)
			{
				SpawnFirstPersonWeapon(currentWeaponState);
			}
			else if (firstPersonWeaponVisual.data.weaponType != currentWeaponState.WeaponType)
			{
				Destroy(firstPersonWeaponVisual.gameObject);
				SpawnFirstPersonWeapon(currentWeaponState);
			}
			/*
			if (_firstPersonActive && !visibleWeapon.firstPersonVisible)
			{
				visibleWeapon.CreateFPSVisual(FirstPersonSetup.WeaponLayer);
				FirstPersonSetup.LeftHandSnap.Handle = visibleWeapon.FirstPersonVisual.LeftHandHandle;
			}
			if (_thirdPersonActive && !visibleWeapon.thirdPersonVisible)
			{
				visibleWeapon.CreateThirdPersonVisual(ThirdPersonSetup.WeaponLayer);
			}

			FirstPersonSetup.Animator.runtimeAnimatorController = visibleWeapon.HandsAnimatorController;
			ThirdPersonSetup.Animator.SetFloat(AnimatorId.WeaponId, (int)visibleWeapon.ThirdPersonAnimationType);

			// Hide and show animations are played only for local player
			if (_firstPersonActive)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.Show);
			}*/
		}

		public void UpdateThirdPersonVisual()
		{
			if (!_thirdPersonActive) return;

			bool hasWeaponInHand = WeaponsOwned[ActiveWeaponSlot].WeaponType != EWeaponType.None;
			if (hasWeaponInHand)
			{
				var currentWeaponState = WeaponsOwned[ActiveWeaponSlot];

				if (thirdPersonWeaponVisual == null)
				{
					SpawnThirdPersonWeapon(currentWeaponState);
				}
				else if (thirdPersonWeaponVisual.data.weaponType != currentWeaponState.WeaponType)
				{
					Destroy(thirdPersonWeaponVisual.gameObject);
					SpawnThirdPersonWeapon(currentWeaponState);
				}
			}
			var otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
			bool hasWeaponOnBack = WeaponsOwned[otherWeaponSlot].WeaponType != EWeaponType.None;
			if (hasWeaponOnBack)
			{
				var currentBackWeapon = WeaponsOwned[otherWeaponSlot];
				if (weaponOnBackVisual == null)
				{
					SpawnWeaponOnBack(currentBackWeapon);
				}
				else if (weaponOnBackVisual.data.weaponType != currentBackWeapon.WeaponType)
				{
					Destroy(weaponOnBackVisual.gameObject);
					SpawnWeaponOnBack(currentBackWeapon);
				}
			}
		}

		public void SpawnFirstPersonWeapon(WeaponState weaponInstance)
		{
			var weaponToSpawn = WeaponDatabase.weaponList.GetWeaponData(weaponInstance.WeaponType).weaponVisualFirstPerson;
			firstPersonWeaponVisual = Instantiate(weaponToSpawn, firstPersonWeaponParent);
			LayerTools.SetLayerRecursively(firstPersonWeaponVisual.gameObject, FirstPersonSetup.WeaponLayer);
		}

		public void SpawnThirdPersonWeapon(WeaponState weaponInstance)
		{
			var weaponToSpawn = WeaponDatabase.weaponList.GetWeaponData(weaponInstance.WeaponType).weaponVisualThirdPerson;
			thirdPersonWeaponVisual = Instantiate(weaponToSpawn, thirdPersonWeaponParent);
			LayerTools.SetLayerRecursively(thirdPersonWeaponVisual.gameObject, ThirdPersonSetup.WeaponLayer);
		}

		public void SpawnWeaponOnBack(WeaponState weaponInstance)
		{
			var weaponToSpawn = WeaponDatabase.weaponList.GetWeaponData(weaponInstance.WeaponType).weaponVisualThirdPerson;
			weaponOnBackVisual =  Instantiate(weaponToSpawn, backWeaponParent);
			LayerTools.SetLayerRecursively(weaponOnBackVisual.gameObject, ThirdPersonSetup.WeaponLayer);
		}


		// DATA STRUCTURES

		[Serializable]
		public class Setup
		{
			public Transform WeaponHandle;
			[Layer]
			[NonSerialized]public int       WeaponLayer;
			public Animator  Animator;
			public HandSnap  LeftHandSnap;
		}
	}
}

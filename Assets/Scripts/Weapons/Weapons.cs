using System;
using Fusion;
using NUnit.Framework;
using UnityEngine;

namespace SimpleFPS
{
	public struct WeaponState : INetworkStruct
	{
		public EWeaponType WeaponType;
		public int AmmoInMag;
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
		public Transform WeaponParentObject;
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
		[Networked] private TickTimer _switchTimer { get; set; }
		[Networked] private TickTimer _granadeThrowTimer { get; set; }
		[Networked] private TickTimer _inMeleeTimer { get; set; }

		public bool InFireCooldown => _fireCooldown.ExpiredOrNotRunning(Runner) == false;
		public float? FireCooldownRemaining => _fireCooldown.RemainingTime(Runner);
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


		public Weapon[] weaponObjectsOwned { get; } = new Weapon[2];


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
				Reload();
			}
			else if (input.Buttons.IsSet(EInputButton.Fire))
			{
				bool justPressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Fire);
				Fire(justPressed);
				//Health.StopImmortality();
			}
		}



		public void SetFirstPersonLayer(int layer)
		{
			

			FirstPersonSetup.WeaponLayer = layer;

			if (weaponObjectsOwned[ActiveWeaponSlot] != null && weaponObjectsOwned[ActiveWeaponSlot].firstPersonVisible)
			{
				LayerTools.SetLayerRecursively(weaponObjectsOwned[ActiveWeaponSlot].FirstPersonVisual.gameObject, layer);
			}
		}

		public void SetThirdPersonLayer(int layer)
		{
			ThirdPersonSetup.WeaponLayer = layer;
			if (weaponObjectsOwned[ActiveWeaponSlot] != null && weaponObjectsOwned[ActiveWeaponSlot].thirdPersonVisible)
			{
				LayerTools.SetLayerRecursively(weaponObjectsOwned[ActiveWeaponSlot].ThirdPersonVisual.gameObject, layer);
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

		public void Fire(bool justPressed)
		{
			if (ActiveWeaponSlot < 0 || ActiveWeaponSlot >= WeaponsOwned.Length || WeaponsOwned[ActiveWeaponSlot].WeaponType == 0)
				return;

			if (CurrentWeapon.Fire(FireTransform.position, FireTransform.forward, justPressed) == false)
				return;

			// For local player play fire animation but only
			// in forward tick as starting animation multiple times
			// during resimulations is not desired.
			if (_firstPersonActive && Runner.IsForward)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.Fire);
			}
		}

		public void Reload()
		{
			if (CurrentWeapon == null || IsSwitching)
				return;

			CurrentWeapon.Reload();
		}

		public void DropWeapon()
		{
			if (CurrentWeapon == null || IsSwitching)
				return;

			// For simplicity just remove current weapon	
			CurrentWeapon = null;
		}

		public void SwitchWeapon()
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

			// For local player start with switch animation but only
			// in forward tick as starting animation multiple times
			// during resimulations is not desired.
			if (_firstPersonActive && Runner.IsForward)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.Hide);
				SwitchSound.Play();
			}
		}

		public void PickupWeapon(EWeaponType weaponType, int ammoInMagazin, int ammoInReserve)
		{
			// create weapon object 
			var weaponData = WeaponDatabase.weaponList.GetWeaponData(weaponType);
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
				});
			// check if back weapon slot is free
			// - if yes: put new weapon in back slot, and switch to it
			// - if no: drop current weapon and equip new weapon
			if (WeaponsInBack[0] == null)
			{
				WeaponsInBack.Set(0, weaponObject);
				SwitchWeapon();
			}
			else
			{
				DropWeapon();
				CurrentWeapon = weaponObject.GetComponent<Weapon>();
				
			}

			


		}

		public Weapon GetWeaponInBack()
		{
			if (weaponsInBack[0] != null && weaponsInBack[0] != CurrentWeapon)
				return weaponsInBack[0];

			return default;
		}

		private void Awake()
		{
			// All weapons are already present inside Player prefab.
			// This is the simplest solution when only few weapons are available in the game.
			
			AllGranades = GetComponentsInChildren<Granade>();
		}

		private void LateUpdate()
		{
			if (Object == null)
				return; // Not valid

			if (_visibleWeapon != null)
			{
				if (_firstPersonActive)
				{
					var weaponTransform = _visibleWeapon.FirstPersonVisual.gameObject.transform;
					var weaponPivot = _visibleWeapon.FirstPersonVisual.Pivot;
					weaponTransform.rotation = FirstPersonSetup.WeaponHandle.rotation * weaponPivot.localRotation;
					weaponTransform.position = FirstPersonSetup.WeaponHandle.position + weaponTransform.rotation * weaponPivot.localPosition;
				}
				if (_thirdPersonActive)
				{
					
					var weaponTransform = _visibleWeapon.ThirdPersonVisual.gameObject.transform;
					var weaponPivot = _visibleWeapon.ThirdPersonVisual.Pivot;

					weaponTransform.SetParent(ThirdPersonSetup.WeaponHandle);
					weaponTransform.rotation = ThirdPersonSetup.WeaponHandle.rotation * weaponPivot.localRotation;
					weaponTransform.position = ThirdPersonSetup.WeaponHandle.position + weaponTransform.rotation * weaponPivot.localPosition;
				}

				
			}
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

		public override void FixedUpdateNetwork()
		{
			TryActivatePendingWeapon();
		}

		public override void Render()
		{
			UpdateVisibleWeapon();

			if (_firstPersonActive && CurrentWeapon != null)
			{
				FirstPersonSetup.Animator.SetBool(AnimatorId.IsReloading, CurrentWeapon.IsReloading);
			}
		}

		private void UpdateVisibleWeapon()
		{
			if (_visibleWeapon == CurrentWeapon)
				return;

			_visibleWeapon = CurrentWeapon;

			// Update weapon visibility
			//for (int i = 0; i < AllWeapons.Length; i++)
			//{
			//	var weapon = AllWeapons[i];
			//	weapon.ToggleVisibility(weapon == CurrentWeapon);
			//}

			_visibleWeapon.ToggleVisibility(true); 
			foreach (var weapon in weaponsInBack)
			{
				weapon.ToggleVisibility(false);
			}

			var playerKey = new PlayerKey(Runner.LocalPlayer, player.LocalIndex);
			if (!_visibleWeapon.OwnerPlayerKey.Equals(playerKey))
			{
				_visibleWeapon.OwnerPlayerKey = playerKey;
			}

			if (_firstPersonActive&&!_visibleWeapon.firstPersonVisible)
			{
				_visibleWeapon.CreateFPSVisual(FirstPersonSetup.WeaponLayer);
				

				FirstPersonSetup.LeftHandSnap.Handle = _visibleWeapon.FirstPersonVisual.LeftHandHandle;
			}
			if (_thirdPersonActive && !_visibleWeapon.thirdPersonVisible)
			{
				_visibleWeapon.CreateThirdPersonVisual(ThirdPersonSetup.WeaponLayer);
			}


			FirstPersonSetup.Animator.runtimeAnimatorController = _visibleWeapon.HandsAnimatorController;
			ThirdPersonSetup.Animator.SetFloat(AnimatorId.WeaponId, (int)_visibleWeapon.ThirdPersonAnimationType);

			// Hide and show animations are played only for local player
			if (_firstPersonActive)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.Show);
			}
		}

		private void TryActivatePendingWeapon()
		{
			if (IsSwitching == false || _pendingWeapon == null)
				return;

			if (_switchTimer.RemainingTime(Runner) > WeaponSwitchTime * 0.5f)
				return; // Too soon.

			CurrentWeapon = _pendingWeapon;
			_pendingWeapon = null;

			// Make the weapon immediately active (previous weapon will be deactivated in Render)
			CurrentWeapon.ToggleVisibility(true);
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

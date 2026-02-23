using Fusion;
using System;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace SimpleFPS
{
	


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

		public GrenadePouch grenadePouch; // this should later be set via gamemode or player equipment system

		[Networked, Capacity(4)]
		public NetworkArray<byte> GrenadesOwned { get; }

		[Networked]
		public int ActiveGrenadeSlot { get; set; }

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

		public Transform firstPersonWeaponParent;
		private WeaponVisualFirstPerson firstPersonWeaponVisual;
		public Transform thirdPersonWeaponParent;
		private WeaponVisualThirdPerson thirdPersonWeaponVisual;
		public Transform backWeaponParent;
		private WeaponVisualThirdPerson weaponOnBackVisual;


		private int _visibleFireCount;

		

		/*

		[Networked, HideInInspector]
		public Weapon CurrentWeapon { get; set; }

		[Networked, Capacity(2)]
		public NetworkArray<NetworkObject> WeaponsInBack { get; }

		

		[Networked]
		private Weapon _pendingWeapon { get; set; }

		private Weapon _visibleWeapon;*/

		public void ApplyEquipment(PlayerEquipment playerEquipment)
		{
			ActiveWeaponSlot = 0;
			if (playerEquipment.weaponInHand != EWeaponType.None)
			{
				var weaponData = WeaponDatabase.weaponList.GetWeaponData(playerEquipment.weaponInHand);
				int magazinSize = weaponData.MagazinSize;
				WeaponsOwned.Set(0, new WeaponState
				{
					WeaponType = playerEquipment.weaponInHand,
					AmmoInMagazin = magazinSize,
					AmmoReserve = magazinSize * (playerEquipment.weaponInHandMagazins - 1)
				});
			}
			if (playerEquipment.secondaryWeapon != EWeaponType.None)
			{
				var weaponData = WeaponDatabase.weaponList.GetWeaponData(playerEquipment.secondaryWeapon);
				int magazinSize = weaponData.MagazinSize;
				WeaponsOwned.Set(1, new WeaponState
				{
					WeaponType = playerEquipment.secondaryWeapon,
					AmmoInMagazin = magazinSize,
					AmmoReserve = magazinSize * (playerEquipment.secondaryWeaponMagazins - 1)
				});
			}

			if (grenadePouch != null) // here starting grenades are set even though right now grenades are not part of player equipment but part of this script
			{
				for (int i = 0; i < grenadePouch.grenadeSlots.Length; i++)
				{
					var grendeSlot = grenadePouch.grenadeSlots[i];
					GrenadesOwned.Set(i, grendeSlot.startingGrenades);
					ActiveGrenadeSlot = 0;
				}


			}

			EquipWeapon(ActiveWeaponSlot);
		}

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
				TryStartSwitchWeapon();
			}
			else if (reloadPressed)
			{
				TryStartReload();
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
			var activeWeaponState = WeaponsOwned[ActiveWeaponSlot];
			if (activeWeaponState.WeaponType != EWeaponType.None 
				&& activeWeaponState.AmmoInMagazin <= 0 
				&& activeWeaponState.AmmoReserve > 0
				)
			{
				TryStartReload();
			}
				


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
			if (IsSwitching )
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
			if (ActiveGrenadeSlot < 0 
				|| ActiveGrenadeSlot >= GrenadesOwned.Length 
				|| GrenadesOwned[ActiveGrenadeSlot] <= 0)
				return;
			
			if (HasStateAuthority)
			{
				var grenadeType = grenadePouch.grenadeSlots[ActiveGrenadeSlot].grenadeType;
				var grenadeData = WeaponDatabase.GetGrenadeData(grenadeType);
				var throwPosition = FireTransform.position;
				var throwDirection = FireTransform.forward;
				throwDirection = Quaternion.Euler(-grenadeData.throwArc, 0, 0) * throwDirection;
				float throwForce = grenadeData.throwForce;
				Quaternion rotation = Quaternion.LookRotation(throwDirection, Vector3.up);

				var projectile = Runner.Spawn(grenadeData.granadeProjectile, throwPosition, rotation, Object.InputAuthority) as GranadeProjectile;
				projectile.Throw(throwPosition, rotation, throwForce);

				
				byte grenadesLeft = (byte)(GrenadesOwned[ActiveGrenadeSlot] - 1);
				GrenadesOwned.Set(ActiveGrenadeSlot, grenadesLeft);
			}
		}

		public void TryFire(bool justPressed)
		{
			if (WeaponsOwned[ActiveWeaponSlot].AmmoInMagazin <= 0)
				return;

			if (InFireCooldown) return;
			if (InReloadCooldown) return;
			if (IsSwitching) return;
			if (InMeleeCooldown) return;
			if (InGranadeThrowCooldown) return;

			if (ActiveWeaponSlot < 0 || ActiveWeaponSlot >= WeaponsOwned.Length || WeaponsOwned[ActiveWeaponSlot].WeaponType == EWeaponType.None)
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

			

			_fireCooldown = TickTimer.CreateFromTicks(Runner, fireCooldownInTicks);

			if (_firstPersonActive && Runner.IsForward)
			{
				FirstPersonSetup.Animator.SetTrigger(AnimatorId.Fire);
			}

			// Decrease ammo
			var weaponState = WeaponsOwned[ActiveWeaponSlot];
			weaponState.AmmoInMagazin -= 1;
			WeaponsOwned.Set(ActiveWeaponSlot, weaponState);
		}

		public void TryStartReload()
		{
			if (InFireCooldown) return;
			if (InReloadCooldown) return;
			if (IsSwitching) return;
			if (InMeleeCooldown) return;
			if (InGranadeThrowCooldown) return;


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
			Debug.Log("StartReload");
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

		public void DropEverything()
			{
			DropWeapon();
			RemoveBackWeapon();
		}

		public void DropWeapon()
		{
			WeaponsOwned.Set(ActiveWeaponSlot, new WeaponState { WeaponType = EWeaponType.None, AmmoInMagazin = 0, AmmoReserve = 0, Util = 0f });
			if (firstPersonWeaponVisual != null)
				Destroy(firstPersonWeaponVisual.gameObject);
			if (thirdPersonWeaponVisual != null)
				Destroy(thirdPersonWeaponVisual.gameObject);
		}

		public void RemoveBackWeapon()
		{
			int otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
			WeaponsOwned.Set(otherWeaponSlot, new WeaponState { WeaponType = EWeaponType.None, AmmoInMagazin = 0, AmmoReserve = 0, Util = 0f });
			if (weaponOnBackVisual != null)
				Destroy(weaponOnBackVisual.gameObject);
		}

		public void TryStartSwitchWeapon()
		{
			int otherWeaponSlot = (ActiveWeaponSlot + 1) % WeaponsOwned.Length;
			var otherWeaponData = WeaponsOwned[otherWeaponSlot];
			if (otherWeaponData.WeaponType == EWeaponType.None)
				return;

			if (IsSwitching) return;
			if (InFireCooldown) return;
			if (InMeleeCooldown) return;
			if (InGranadeThrowCooldown) return;

			StartSwitchWeapon();
		}

		public void StartSwitchWeapon()
		{
			if (InReloadCooldown)
			{
				CancelReload();
			}

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
			EquipWeapon((byte)otherWeaponSlot);
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
				_switchApplied = true;
			}

			CalculateFirerateOfCurrentWeapon();
		}

		public void CalculateFirerateOfCurrentWeapon()
		{
			var weaponState = WeaponsOwned[ActiveWeaponSlot];
			// calculate fire cooldown in ticks
			var weaponFireRate = WeaponDatabase.weaponList.GetWeaponData(weaponState.WeaponType).ShotsPerSecond;
			float fireTime = 1f / weaponFireRate;
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
			WeaponFireHandler.OnFire += VisulizeHitBullet;
		}

		public override void Spawned()
		{
		}

		public override void Render()
		{
			UpdateFirstPersonWeapon();
			UpdateThirdPersonVisual();

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
			else if (firstPersonWeaponVisual.Data.weaponType != currentWeaponState.WeaponType)
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
				else if (thirdPersonWeaponVisual.Data.weaponType != currentWeaponState.WeaponType)
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
				else if (weaponOnBackVisual.Data.weaponType != currentBackWeapon.WeaponType)
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

			FirstPersonSetup.Animator.runtimeAnimatorController = weaponToSpawn.HandsAnimatorController;
		}

		public void SpawnThirdPersonWeapon(WeaponState weaponInstance)
		{
			var weaponToSpawn = WeaponDatabase.weaponList.GetWeaponData(weaponInstance.WeaponType).weaponVisualThirdPerson;
			thirdPersonWeaponVisual = Instantiate(weaponToSpawn, thirdPersonWeaponParent);
			LayerTools.SetLayerRecursively(thirdPersonWeaponVisual.gameObject, ThirdPersonSetup.WeaponLayer);
			ThirdPersonSetup.Animator.SetFloat(AnimatorId.WeaponId, (int)weaponToSpawn.ThirdPersonAnimationType);

			CalculateFirerateOfCurrentWeapon();
		}

		public void SpawnWeaponOnBack(WeaponState weaponInstance)
		{
			var weaponToSpawn = WeaponDatabase.weaponList.GetWeaponData(weaponInstance.WeaponType).weaponVisualThirdPerson;
			weaponOnBackVisual =  Instantiate(weaponToSpawn, backWeaponParent);
			LayerTools.SetLayerRecursively(weaponOnBackVisual.gameObject, ThirdPersonSetup.WeaponLayer);
		}

		public void VisulizeHitBullet(FireEvent fireEvent)
		{

			if (firstPersonWeaponVisual != null)
			{
				firstPersonWeaponVisual.OnFire(fireEvent);
			}
			if (thirdPersonWeaponVisual != null)
			{
				thirdPersonWeaponVisual.OnFire(fireEvent);
			}
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

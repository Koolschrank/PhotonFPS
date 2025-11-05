using Fusion;
using Fusion.Addons.SimpleKCC;
using SimpleFPS;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleFPS
{
	public enum EInputButton
	{
		Jump,
		Fire,
		Reload,
		Pistol,
		Rifle,
		Shotgun,
		Spray,

		SwitchWeapon,
		Crouch,
		Melee,
		Grenade,
		Aim,



	}

	/// <summary>
	/// Input structure sent over network to the server.
	/// </summary>
	public struct NetworkedInput : INetworkInput
	{
		public NetworkedInputPlayer slot0;
		public NetworkedInputPlayer slot1;
		public NetworkedInputPlayer slot2;
		public NetworkedInputPlayer slot3;

		// Safe indexer to get/set slots by index
		public NetworkedInputPlayer this[int index]
		{
			readonly get
			{
				return index switch
				{
					0 => slot0,
					1 => slot1,
					2 => slot2,
					3 => slot3,
					_ => default
				};
			}
			set
			{
				switch (index)
				{
					case 0: slot0 = value; break;
					case 1: slot1 = value; break;
					case 2: slot2 = value; break;
					case 3: slot3 = value; break;
					default:
						Debug.LogWarning($"[NetworkedInput] Invalid slot index {index}");
						break;
				}
			}
		}

		/// <summary>
		/// Sets the slot by index, used from InputCollector to fill player inputs.
		/// </summary>
		public void SetSlot(int index, in NetworkedInputPlayer slot)
		{
			this[index] = slot;
		}

		/// <summary>
		/// Tries to get a slot safely without throwing.
		/// </summary>
		public bool TryGetSlot(int index, out NetworkedInputPlayer slot)
		{
			if ((uint)index > 3)
			{
				slot = default;
				return false;
			}

			slot = this[index];
			return true;
		}
	}

	public struct NetworkedInputPlayer : INetworkInput
	{
		public Vector2 MoveDirection;
		public Vector2 LookRotationDelta;
		public NetworkButtons Buttons;
	}


	/// <summary>
	/// Handles player input.
	/// </summary>
	[DefaultExecutionOrder(-10)]
	public sealed class PlayerInput : NetworkBehaviour, IBeforeUpdate
	{
		public static float LookSensitivity = 1f;

	
		private NetworkedInput _accumulatedInput;
		private Vector2Accumulator _lookRotationAccumulator = new Vector2Accumulator(0.02f, true);

		public override void Spawned()
		{
			if (!HasInputAuthority) return;

			var networkEvents = Runner.GetComponent<NetworkEvents>();
			networkEvents.OnInput.AddListener(OnInput);

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (runner == null) return;
			var networkEvents = runner.GetComponent<NetworkEvents>();
			if (networkEvents != null)
				networkEvents.OnInput.RemoveListener(OnInput);
		}

		void IBeforeUpdate.BeforeUpdate()
		{
			if (!HasInputAuthority) return;

			// Toggle cursor with Enter
			var keyboard = Keyboard.current;
			if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
			{
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
				else
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
			}

			if (Cursor.lockState != CursorLockMode.Locked) return;

			// Handle mouse look
			var mouse = Mouse.current;
			if (mouse != null)
			{
				var mouseDelta = mouse.delta.ReadValue();
				var lookDelta = new Vector2(-mouseDelta.y, mouseDelta.x) * (LookSensitivity / 60f);
				_lookRotationAccumulator.Accumulate(lookDelta);

				_accumulatedInput.slot0.Buttons.Set(EInputButton.Fire, mouse.leftButton.isPressed);
			}

			// Handle keyboard movement & buttons
			if (keyboard != null)
			{
				Vector2 moveDir = Vector2.zero;
				if (keyboard.wKey.isPressed) moveDir += Vector2.up;
				if (keyboard.sKey.isPressed) moveDir += Vector2.down;
				if (keyboard.aKey.isPressed) moveDir += Vector2.left;
				if (keyboard.dKey.isPressed) moveDir += Vector2.right;

				_accumulatedInput.slot0.MoveDirection = moveDir.normalized;
				_accumulatedInput.slot0.Buttons.Set(EInputButton.Jump, keyboard.spaceKey.isPressed);
				_accumulatedInput.slot0.Buttons.Set(EInputButton.Reload, keyboard.rKey.isPressed);
				_accumulatedInput.slot0.Buttons.Set(EInputButton.Pistol, keyboard.digit1Key.isPressed || keyboard.numpad1Key.isPressed);
				_accumulatedInput.slot0.Buttons.Set(EInputButton.Rifle, keyboard.digit2Key.isPressed || keyboard.numpad2Key.isPressed);
				_accumulatedInput.slot0.Buttons.Set(EInputButton.Shotgun, keyboard.digit3Key.isPressed || keyboard.numpad3Key.isPressed);
				_accumulatedInput.slot0.Buttons.Set(EInputButton.Spray, keyboard.fKey.isPressed);

				
			}
		}

		private void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			return;
			_accumulatedInput.slot0.LookRotationDelta = _lookRotationAccumulator.ConsumeTickAligned(runner);

			// For now, only slot0 is sent. Other slots are empty.
			networkInput.Set(_accumulatedInput);
		}
	}
}

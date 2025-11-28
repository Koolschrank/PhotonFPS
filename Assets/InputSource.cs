using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

namespace SimpleFPS
{
	/// <summary>
	/// Represents one physical input device (controller or keyboard).
	/// Automatically spawned by PlayerInputManager.
	/// </summary>
	public class InputSource : MonoBehaviour
	{
		[HideInInspector] public int playerIndex;
		public Vector2 Move;
		public Vector2 Look;
		public NetworkButtons Buttons;

		public void OnMove(InputAction.CallbackContext ctx) => Move = ctx.ReadValue<Vector2>();
		public void OnLook(InputAction.CallbackContext ctx) => Look = ctx.ReadValue<Vector2>();

		public void OnShoot(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.Fire);
		public void OnAim(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.Aim);
		public void OnMelee(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.Melee);
		public void OnGrenade(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.Granade);
		public void OnReload(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.Reload);
		public void OnSwitchWeapon(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.SwitchWeapon);
		public void OnCrouch(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.Crouch);
		public void OnJump(InputAction.CallbackContext ctx) => SetButton(ctx, EInputButton.Jump);

		private void SetButton(InputAction.CallbackContext ctx, EInputButton button)
		{
			if (ctx.performed) Buttons.Set(button, true);
			else if (ctx.canceled) Buttons.Set(button, false);
		}
	}
}

using UnityEngine;

namespace SimpleFPS
{
	public class UIMenuView : MonoBehaviour
	{
		private PlayerUI _PlayerUI;

		private CursorLockMode _previousLockState;
		private bool _previousCursorVisibility;

		// Called from button OnClick event.
		public void ResumeGame()
		{
			gameObject.SetActive(false);
		}

		// Called from button OnClick event.
		public void OpenSettings()
		{
			_PlayerUI.SettingsView.gameObject.SetActive(true);
		}

		// Called from button OnClick event.
		public void LeaveGame()
		{
			// Clear previous cursor state so it does not get locked when unloading scene.
			_previousLockState = CursorLockMode.None;
			_previousCursorVisibility = true;

			_PlayerUI.GoToMenu();
		}

		private void Awake()
		{
			_PlayerUI = GetComponentInParent<PlayerUI>();
		}

		private void OnEnable()
		{
			_previousLockState = Cursor.lockState;
			_previousCursorVisibility = Cursor.visible;

			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void OnDisable()
		{
			Cursor.lockState = _previousLockState;
			Cursor.visible = _previousCursorVisibility;
		}
	}
}

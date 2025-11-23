using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SimpleFPS
{
    public class PlayerUI : MonoBehaviour
	{
		public int localPlayerIndex;
		public Gameplay Gameplay;

		public UIPlayerView PlayerView;
		public UIGameplayView GameplayView;
		public UIGameOverView GameOverView;
		public GameObject ScoreboardView;
		public GameObject MenuView;
		public UISettingsView SettingsView;
		private SceneObjects _sceneObjects;


		public NetworkRunner Runner { get; private set; }

		public void Initialize(NetworkRunner runner)
		{
			Runner = runner;
		}

		// Called from NetworkEvents on NetworkRunner object
		public void OnRunnerShutdown(NetworkRunner runner, ShutdownReason reason)
		{
			if (GameOverView.gameObject.activeSelf)
				return; // Regular shutdown - GameOver already active

			ScoreboardView.SetActive(false);
			SettingsView.gameObject.SetActive(false);
			MenuView.gameObject.SetActive(false);

		}

		public void GoToMenu()
		{
			if (Runner != null)
			{
				Runner.Shutdown();
			}

			SceneManager.LoadScene("Startup");
		}

		private void Awake()
		{
			
			PlayerView.gameObject.SetActive(false);
			MenuView.SetActive(false);
			SettingsView.gameObject.SetActive(false);

			SettingsView.LoadSettings();

			// Make sure the cursor starts unlocked
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void Update()
		{
			if (Application.isBatchMode == true)
				return;

			if (Gameplay.Object == null || Gameplay.Object.IsValid == false)
				return;

			if (_sceneObjects == null)
			{
				Debug.Log("PlayerUI: Fetching SceneObjects singleton.");
				if (Runner == null) return;

				_sceneObjects = Runner.GetSingleton<SceneObjects>();
				if (_sceneObjects == null)
					return;
			}

			var keyboard = Keyboard.current;
			bool gameplayActive = Gameplay.State < EGameplayState.Finished;

			ScoreboardView.SetActive(gameplayActive && keyboard != null && keyboard.tabKey.isPressed);

			if (gameplayActive && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
			{
				MenuView.SetActive(!MenuView.activeSelf);
			}

			GameplayView.gameObject.SetActive(gameplayActive);
			GameOverView.gameObject.SetActive(gameplayActive == false);

			if (_sceneObjects.Device == null) return;

			var playerObject = _sceneObjects.Device.GetLocalPlayerObjectViaIndex(localPlayerIndex);
			if (playerObject != null)
			{
				var key = new PlayerKey(Runner.LocalPlayer, playerObject.LocalIndex);
				var playerData = Gameplay.PlayerData.Get(key);

				PlayerView.UpdatePlayer(playerObject, playerData);
				PlayerView.gameObject.SetActive(gameplayActive);
			}
			else
			{
				PlayerView.gameObject.SetActive(false);
			}
		}
	}
}

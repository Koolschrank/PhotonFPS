using Fusion;

namespace SimpleFPS
{
	/// <summary>
	/// Singleton on Runner used to obtain scene object references using lazy getters.
	/// </summary>
	public class SceneObjects : SimulationBehaviour
	{
		// Use Runner.GetSingleton<SceneObjects>() to get SceneObjects instance.

		public Gameplay Gameplay
		{
			get
			{
				if (_gameplay == null && Runner != null && Runner.SceneManager != null && Runner.SceneManager.MainRunnerScene.IsValid())
				{
					var gameplays = Runner.SceneManager.MainRunnerScene.GetComponents<Gameplay>(true);
					if (gameplays.Length > 0)
					{
						_gameplay = gameplays[0];
					}
				}

				return _gameplay;
			}
		}

		public GameUI GameUI
		{
			get
			{
				if (_gameUI == null && Runner != null && Runner.SceneManager != null && Runner.SceneManager.MainRunnerScene.IsValid())
				{
					var gameUIs = Runner.SceneManager.MainRunnerScene.GetComponents<GameUI>(true);
					if (gameUIs.Length > 0)
					{
						_gameUI = gameUIs[0];
					}
				}

				return _gameUI;
			}
		}

		public Device Device
		{
			get
			{
				if (_device == null && Runner != null && Runner.SceneManager != null && Runner.SceneManager.MainRunnerScene.IsValid())
				{
					var devices = Runner.SceneManager.MainRunnerScene.GetComponents<Device>(true);
					if (devices.Length > 0)
					{
						_device = devices[0];
					}
				}
				return _device;
			}
		}

		private Gameplay _gameplay;
		private GameUI _gameUI;
		private Device _device;
	}
}

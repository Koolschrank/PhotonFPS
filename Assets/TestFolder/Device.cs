using UnityEngine;
using Fusion;

namespace SimpleFPS
{
    public class Device : MonoBehaviour
    {
        public Gameplay gameplay;
		public NetworkRunner runner;

		int localPlayers = 0;
        public ScreenManager screenManager;

		private void Start()
		{
            if (gameplay == null)
                gameplay = FindAnyObjectByType<Gameplay>();
            if (runner == null)
                runner = FindAnyObjectByType<NetworkRunner>();

			// subscribe to new local players added
			gameplay.OnNewPlayerAdded += LocalPlayerAdded;
			
		}

		public void LocalPlayerAdded(PlayerKey playerKey)
        {
			if (playerKey.PlayerRef != runner.LocalPlayer) return;

			localPlayers++;
			screenManager.LocalPlayerAdded();

		}
    }
}

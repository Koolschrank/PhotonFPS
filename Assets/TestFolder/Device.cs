using UnityEngine;
using Fusion;
using NUnit.Framework;
using System.Collections.Generic;

namespace SimpleFPS
{
    public class Device : MonoBehaviour
    {
        public Gameplay gameplay;
		private NetworkRunner runner;
		public PlayerUIManager uiManager;

		int localPlayers = 0;
        public ScreenManager screenManager;
		

		public List<Player> localPlayerObjects = new List<Player>();

		private void Start()
		{
            if (gameplay == null)
                gameplay = FindAnyObjectByType<Gameplay>();
            if (runner == null)
                runner = FindAnyObjectByType<NetworkRunner>();

			// subscribe to new local players added
			gameplay.OnNewPlayerAdded += LocalPlayerAdded;

			uiManager.InitilizeAllUIs(runner);
			
		}

		public void LocalPlayerAdded(PlayerKey playerKey)
        {
			if (playerKey.PlayerRef != runner.LocalPlayer) return;

			localPlayers++;
			screenManager.LocalPlayerAdded();

			uiManager.SetActiveUIs(localPlayers);

		}

		public void LocalPlayerObjectSpawned(Player player)
		{
			
			localPlayerObjects.RemoveAll(p => p == null);
			localPlayerObjects.RemoveAll(p => !p.isSpawned);


			if (player.Object.InputAuthority != runner.LocalPlayer)
				return;

			
			bool alreadyExists = localPlayerObjects.Exists(p => p.LocalIndex.Equals(player.LocalIndex));
			if (alreadyExists)
			{
				for (int i = 0; i < localPlayerObjects.Count; i++)
				{
					if (localPlayerObjects[i].LocalIndex.Equals(player.LocalIndex))
					{
						localPlayerObjects[i] = player;
						return;
					}
				}
			}

			localPlayerObjects.Add(player);

		}

		public Player GetLocalPlayerObjectViaIndex(int index)
		{
			localPlayerObjects.RemoveAll(p => p == null);

			foreach (Player player in localPlayerObjects)
			{
				if (!player.isSpawned) continue;
				if (player.LocalIndex == index)
					return player;
			}
			return null;
		}

	}
}

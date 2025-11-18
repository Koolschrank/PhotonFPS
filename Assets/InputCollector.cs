using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.Unicode;

namespace SimpleFPS
{
	/// <summary>
	/// Local singleton that collects inputs from all joined InputSources.
	/// Handles PlayerInputManager events automatically.
	/// </summary>
	[DefaultExecutionOrder(-100)]
	public class InputCollector : MonoBehaviour, INetworkRunnerCallbacks
	{
		[Header("References")]
		public PlayerInputManager inputManager; // assign in inspector
		public NetworkRunner runner;

		readonly List<InputSource> sources = new List<InputSource>();

		private void Awake()
		{
			if (runner == null)
				runner = FindAnyObjectByType<NetworkRunner>();

			if (runner != null)
			{
				runner.AddCallbacks(this);
				Debug.Log("[InputCollector] Added to runner callbacks.");
			}
			else
			{
				Debug.LogError("[InputCollector] No NetworkRunner found!");
			}

			if (inputManager == null)
				inputManager = GetComponent<PlayerInputManager>();

			if (inputManager != null)
				inputManager.onPlayerJoined += OnPlayerJoined;

			Debug.Log("[InputCollector] Ready – waiting for controllers.");

			
		}

		private void OnDestroy()
		{
			if (inputManager != null)
				inputManager.onPlayerJoined -= OnPlayerJoined;
		}

		private void OnPlayerJoined(UnityEngine.InputSystem.PlayerInput playerInput)
		{
			playerInput.transform.SetParent(this.transform);
			var src = playerInput.GetComponent<InputSource>();
			if (src == null)
			{
				Debug.LogError($"[InputCollector] PlayerInput {playerInput.name} has no InputSource!");
				return;
			}

			src.playerIndex = sources.Count;
			sources.Add(src);

			if (sources.Count > 1)
			{
				runner.GetSingleton<SceneObjects>().Gameplay.CallLocalPlayerSpawnRPC(new PlayerKey( runner.LocalPlayer, sources.Count - 1));
			}

			Debug.Log($"[InputCollector] New InputSource joined → slot {src.playerIndex}");
		}

		public void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			if (sources.Count == 0)
				return;

			NetworkedInput input = default;

			for (int i = 0; i < sources.Count && i < 4; i++)
			{
				var src = sources[i];
				var slot = new NetworkedInputPlayer
				{
					MoveDirection = src.Move,
					LookRotationDelta = src.Look,
					Buttons = src.Buttons
				};
				input.SetSlot(i, slot);
			}
			networkInput.Set(input);

			
		}

		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
		public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
		public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
		public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
		public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

	}
}

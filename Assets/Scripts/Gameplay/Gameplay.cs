using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleFPS
{

	/// <summary>
	/// Runtime data structure to hold player information which must survive events like player death/disconnect.
	/// </summary>
	public struct PlayerData : INetworkStruct
	{
		

		[Networked, Capacity(24)]
		public string Nickname { get => default; set { } }
		public PlayerRef PlayerRef;
		public int LocalIndex;
		public int Kills;
		public int Deaths;
		public int LastKillTick;
		public int StatisticPosition;
		public bool IsAlive;
		public bool IsConnected;
	}

	public enum EGameplayState
	{
		Skirmish = 0,
		Running = 1,
		Finished = 2,
	}

	/// <summary>
	/// Drives gameplay logic - state, timing, handles player connect/disconnect/spawn/despawn/death, calculates statistics.
	/// </summary>
	public class Gameplay : NetworkBehaviour
	{
		public Action<PlayerKey> OnNewPlayerAdded;

		public GameUI GameUI;
		public Device Device;
		public Player PlayerPrefab;
		public PlayerEquipment startEquipment;
		public float GameDuration = 180f;
		public float PlayerRespawnTime = 5f;
		public float DoubleDamageDuration = 30f;

		[Networked, Capacity(32), HideInInspector]
		public NetworkDictionary<PlayerKey, PlayerData> PlayerData { get; }
		public Dictionary<PlayerKey, Player> PlayerDict { get; } = new Dictionary<PlayerKey, Player>(); // only for host to track spawned players

		[Networked, HideInInspector]
		public TickTimer RemainingTime { get; set; }

		[Networked, HideInInspector]
		public EGameplayState State { get; set; }

		public bool DoubleDamageActive => State == EGameplayState.Running &&
										  RemainingTime.RemainingTime(Runner).GetValueOrDefault() < DoubleDamageDuration;

		private bool _isNicknameSent;
		private float _runningStateTime;
		private List<Player> _spawnedPlayers = new(16);
		private List<PlayerRef> _pendingPlayers = new(16);
		private List<PlayerData> _tempPlayerData = new(16);
		private List<Transform> _recentSpawnPoints = new(4);

		// Called when the gameplay object is spawned into the world
		public override void Spawned()
		{
			if (Runner.Mode == SimulationModes.Server)
			{
				Application.targetFrameRate = TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
			}

			if (Runner.GameMode == GameMode.Shared)
			{
				throw new System.NotSupportedException("This sample doesn't support Shared Mode, please start the game as Server, Host or Client.");
			}
		}


		// Called every fixed tick
		public override void FixedUpdateNetwork()
		{
			

			if (HasStateAuthority == false)
				return;

			
			// Handle new/disconnected players automatically
			PlayerManager.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);

			// Start gameplay when enough players are connected

			bool otherDevicesConnected = false;
			foreach (var pair in PlayerData)
			{
				if (pair.Value.IsConnected && pair.Key.PlayerRef != Runner.LocalPlayer)
				{
					otherDevicesConnected = true;
					break;
				}
			}


			if (State == EGameplayState.Skirmish && otherDevicesConnected)
			{
				StartGameplay();
			}

			if (State == EGameplayState.Running)
			{
				_runningStateTime += Runner.DeltaTime;

				var sessionInfo = Runner.SessionInfo;

				// Hide match after 60s to prevent random joins
				if (sessionInfo.IsVisible && (_runningStateTime > 60f || sessionInfo.PlayerCount >= sessionInfo.MaxPlayers))
				{
					sessionInfo.IsVisible = false;
				}

				if (RemainingTime.Expired(Runner))
				{
					StopGameplay();
				}
			}
		}

		// Called every render frame
		public override void Render()
		{
			if (Runner.Mode == SimulationModes.Server)
				return;

			// Every client sends nickname once
			if (_isNicknameSent == false)
			{
				RPC_SetPlayerNickname(Runner.LocalPlayer, PlayerPrefs.GetString("Photon.Menu.Username"));
				_isNicknameSent = true;
			}
		}

		
		private void SpawnPlayer(PlayerRef playerRef)
		{
			Debug.Log($"SpawnPlayer called for {playerRef}");
			SpawnPlayerForLocalIndex(playerRef, 0);
		}

		private void SpawnPlayerForLocalIndex(PlayerRef playerRef, int localIndex)
		{
			Debug.Log($"Spawning player for {playerRef} localIndex={localIndex}");
			var key = new PlayerKey(playerRef, localIndex);

			bool isNewPlayer = !PlayerData.ContainsKey(key);
			if (PlayerData.TryGet(key, out var data) == false)
			{
				data = new PlayerData
				{
					PlayerRef = playerRef,
					LocalIndex = localIndex,
					Nickname = playerRef.ToString(),
					StatisticPosition = int.MaxValue,
					IsAlive = false,
					IsConnected = false
				};
			}
			if (isNewPlayer)
			{
				RPC_PlayerAdded(key);
			}

			if (data.IsConnected)
				return;

			data.IsConnected = true;
			data.IsAlive = true;
			PlayerData.Set(key, data);
			

			RespawnPlayer(key);
		}

		public void RespawnPlayer(PlayerKey playerKey)
		{
			if (!HasStateAuthority) return;

			if (PlayerDict.TryGetValue(playerKey, out var existingPlayer))
			{
				Runner.Despawn(existingPlayer.Object);
				PlayerDict.Remove(playerKey);
			}

			var spawnPoint = GetSpawnPoint();

			var player = Runner.Spawn(
				PlayerPrefab, 
				spawnPoint.position, 
				spawnPoint.rotation, 
				playerKey.PlayerRef, 
				onBeforeSpawned: (runner, newObj) =>
				{
					if (newObj.TryGetComponent(out Player p))
					{
						p.LocalIndex = playerKey.LocalIndex;
						
					}
					if (newObj.TryGetComponent(out Weapons weapons))
					{
						weapons.ApplyEquipment(startEquipment);
					}
				});
			PlayerDict.Add(playerKey, player);

			Runner.SetPlayerObject(playerKey.PlayerRef, player.Object);
			RecalculateStatisticPositions();
		}


		private void DespawnPlayer(PlayerRef playerRef, Player player)
		{
			var key = new PlayerKey(playerRef, 0);

			if (PlayerData.TryGet(key, out var playerData))
			{
				if (playerData.IsConnected)
				{
					Debug.LogWarning($"{playerRef} disconnected.");
				}

				playerData.IsConnected = false;
				playerData.IsAlive = false;
				PlayerData.Set(key, playerData);
			}

			Runner.Despawn(player.Object);
			RecalculateStatisticPositions();
		}

		public void PlayerKilled(PlayerKey killerKey, PlayerKey victimKey, EWeaponType weaponType, bool isCritical)
		{
			if (!HasStateAuthority)
				return;

			if (PlayerData.TryGet(killerKey, out var killerData))
			{
				killerData.Kills++;
				killerData.LastKillTick = Runner.Tick;
				PlayerData.Set(killerKey, killerData);
			}

			if (PlayerData.TryGet(victimKey, out var victimData))
			{
				victimData.Deaths++;
				victimData.IsAlive = false;
				PlayerData.Set(victimKey, victimData);
			}

			RPC_PlayerKilled(killerKey, victimKey, weaponType, isCritical);
			StartCoroutine(RespawnPlayer(victimKey, PlayerRespawnTime));
			RecalculateStatisticPositions();
		}


		private IEnumerator RespawnPlayer(PlayerKey key, float delay)
		{
			if (delay > 0f)
				yield return new WaitForSecondsRealtime(delay);

			if (Runner == null)
				yield break;
			/*
			// NOTE: Runner.GetPlayerObject(playerRef) returns the single player object
			// registered for that PlayerRef via Runner.SetPlayerObject(...).
			// If you support multiple local players per device later, do NOT rely on
			// Runner.GetPlayerObject to retrieve a specific local avatar (see notes below).
			var existingPlayerObj = Runner.GetPlayerObject(key.PlayerRef);
			if (existingPlayerObj != null)
			{
				// If you are still using one player object per device (localIndex == 0),
				// this will despawn it safely. If you support multiple local avatars, you'll
				// need a different per-key mapping and despawn logic.
				Runner.Despawn(existingPlayerObj);
			}*/

			if (PlayerData.TryGet(key, out var data) == false || data.IsConnected == false)
				yield break;

			data.IsAlive = true;
			PlayerData.Set(key, data);

			//var spawnPoint = GetSpawnPoint();
			RespawnPlayer(key);
			/*
			var playerObject = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, key.PlayerRef);

			// If your Player script has fields for PlayerKey / LocalIndex, set them here:
			if (playerObject.TryGetComponent<Player>(out var playerScript))
			{
				playerScript.LocalIndex = key.LocalIndex;
			}

			// Register mapping.
			// For now (one avatar per device), keep the original behavior:
			Runner.SetPlayerObject(key.PlayerRef, playerObject.Object);

			// If you later support >1 local player per device, replace Runner.SetPlayerObject
			// with a custom mapping per PlayerKey (see notes below).*/
		}


		private Transform GetSpawnPoint()
		{
			Transform spawnPoint = default;

			var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);
			for (int i = 0, offset = UnityEngine.Random.Range(0, spawnPoints.Length); i < spawnPoints.Length; i++)
			{
				spawnPoint = spawnPoints[(offset + i) % spawnPoints.Length].transform;

				if (_recentSpawnPoints.Contains(spawnPoint) == false)
					break;
			}

			_recentSpawnPoints.Add(spawnPoint);
			if (_recentSpawnPoints.Count > 3)
			{
				_recentSpawnPoints.RemoveAt(0);
			}

			return spawnPoint;
		}

		private void StartGameplay()
		{
			StopAllCoroutines();

			State = EGameplayState.Running;
			RemainingTime = TickTimer.CreateFromSeconds(Runner, GameDuration);

			foreach (var pair in PlayerData)
			{
				var data = pair.Value;
				data.Kills = 0;
				data.Deaths = 0;
				data.StatisticPosition = int.MaxValue;
				data.IsAlive = false;

				PlayerData.Set(pair.Key, data);
				StartCoroutine(RespawnPlayer(pair.Key, 0f));
			}
		}

		private void StopGameplay()
		{
			RecalculateStatisticPositions();
			State = EGameplayState.Finished;
		}

		private void RecalculateStatisticPositions()
		{
			if (State == EGameplayState.Finished)
				return;

			_tempPlayerData.Clear();
			foreach (var pair in PlayerData)
			{
				_tempPlayerData.Add(pair.Value);
			}

			_tempPlayerData.Sort((a, b) =>
			{
				if (a.Kills != b.Kills)
					return b.Kills.CompareTo(a.Kills);

				return a.LastKillTick.CompareTo(b.LastKillTick);
			});

			for (int i = 0; i < _tempPlayerData.Count; i++)
			{
				var data = _tempPlayerData[i];
				data.StatisticPosition = data.Kills > 0 ? i + 1 : int.MaxValue;
				PlayerData.Set(new PlayerKey(data.PlayerRef, data.LocalIndex), data);
			}
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_PlayerKilled(PlayerKey killerKey, PlayerKey victimKey, EWeaponType weaponType, bool isCriticalKill)
		{
			string killerNickname = PlayerData.TryGet(killerKey, out var killerData) ? killerData.Nickname : "???";
			string victimNickname = PlayerData.TryGet(victimKey, out var victimData) ? victimData.Nickname : "???";

			var playerUIs = Device.uiManager.playerViews;
			foreach
				(var playerUI in playerUIs)
			{
				playerUI.GameplayView.KillFeed.ShowKill(killerNickname, victimNickname, weaponType, isCriticalKill);
			}
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
		private void RPC_SetPlayerNickname(PlayerRef playerRef, string nickname)
		{
			var key = new PlayerKey(playerRef, 0);
			if (PlayerData.TryGet(key, out var data))
			{
				data.Nickname = nickname;
				PlayerData.Set(key, data);
			}
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
		private void RPC_PlayerAdded(PlayerKey playerKey)
		{
			OnNewPlayerAdded?.Invoke(playerKey);
		}

		public void CallLocalPlayerSpawnRPC(PlayerKey playerKey)
		{
			RPC_LocalPlayerSpawned(playerKey);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
		private void RPC_LocalPlayerSpawned(PlayerKey playerKey)
		{
			SpawnPlayerForLocalIndex(playerKey.PlayerRef, playerKey.LocalIndex);
		}


	}
}

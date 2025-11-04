using System;
using Fusion;

namespace SimpleFPS
{
	/// <summary>
	/// Uniquely identifies one local player on a given device.
	/// </summary>
	public struct PlayerKey : INetworkStruct, IEquatable<PlayerKey>
	{
		public PlayerRef PlayerRef; // The device connection
		public byte LocalIndex;     // 0–3 for local players on that device

		public PlayerKey(PlayerRef playerRef, byte localIndex)
		{
			PlayerRef = playerRef;
			LocalIndex = localIndex;
		}

		public bool Equals(PlayerKey other)
		{
			return PlayerRef == other.PlayerRef && LocalIndex == other.LocalIndex;
		}

		public override bool Equals(object obj)
		{
			return obj is PlayerKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + PlayerRef.GetHashCode();
				hash = hash * 31 + LocalIndex.GetHashCode();
				return hash;
			}
		}

		public override string ToString()
		{
			return $"{PlayerRef}-P{LocalIndex}";
		}
	}
}

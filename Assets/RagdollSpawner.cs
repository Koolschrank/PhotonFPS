using Fusion;
using UnityEngine;

public class RagdollSpawner : NetworkBehaviour
{
	[Header("References")]
	[SerializeField] private Animator playerAnimator;      // Player skeleton
	[SerializeField] private GameObject playerVisualRoot; // Player mesh root
	[SerializeField] private NetworkObject ragdollPrefab; // Ragdoll prefab (NetworkObject)

	[Networked]
	public NetworkObject RagdollInstance { get; set; }    // Networked reference for all clients

	public RagdollBulletImpact bulletImpact;
	public bool IsRagdollSpawned = false;

	public override void Spawned()
	{
		// Ensure visuals are visible at spawn
		if (playerVisualRoot != null)
			playerVisualRoot.SetActive(true);


		if (Object.HasStateAuthority)
		{
			SpawnRagdoll_HostOnly();
		}
	}

	public void CancelRagdoll()
		{
		if (RagdollInstance == null)
			return;
		var ragdoll = RagdollInstance.GetComponent<RagdollController>();
		ragdoll.CancelRagdoll();
		IsRagdollSpawned = false;
	}

	public void SpawnRagdoll()
	{
		if (RagdollInstance == null)
			return;

		
		IsRagdollSpawned = true;

		var ragdoll = RagdollInstance.GetComponent<RagdollController>();
		ragdoll.StartRagdoll();

		ragdoll.CopyPose(playerVisualRoot.transform);
		

		if (bulletImpact.force != 0)
		{
			var hitRigidbody = ragdoll.GetClosesRigidbody(bulletImpact.Position);
			if (hitRigidbody != null)
			{
				hitRigidbody.Rigidbody.AddForceAtPosition(bulletImpact.Normal * bulletImpact.force, bulletImpact.Position, ForceMode.Impulse);
			}
		}
	}

	/// <summary>
	/// Host-only function to spawn the ragdoll
	/// </summary>
	private void SpawnRagdoll_HostOnly()
	{
		if (RagdollInstance != null)
			return;

		// Spawn the ragdoll NetworkObject
		var rag = Runner.Spawn(ragdollPrefab, transform.position, transform.rotation);
		RagdollInstance = rag; // Networked reference synced to clients
	}

	
}


public struct RagdollBulletImpact : INetworkStruct
{
	public Vector3 Position;
	public Vector3 Normal;
	public float force;

	public RagdollBulletImpact(Vector3 position, Vector3 normal, float force)
	{
		Position = position;
		Normal = normal;
		this.force = force;
	}
}
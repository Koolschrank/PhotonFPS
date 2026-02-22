using Fusion;
using UnityEngine;

public class RagdollSpawner : NetworkBehaviour
{
	[Header("References")]
	[SerializeField] private Animator playerAnimator;      // Player skeleton
	[SerializeField] private GameObject playerVisualRoot; // Player mesh root
	[SerializeField] private GameObject ragdollPrefab; // Ragdoll prefab 

	public GameObject RagdollInstance { get; set; }    

	public RagdollBulletImpact bulletImpact;
	public bool IsRagdollSpawned = false;

	

	private void SpawnRagdoll()
	{

		if (RagdollInstance != null)
			return;

		RagdollInstance = Instantiate(ragdollPrefab, transform.position, transform.rotation);
	}

	public void DecoupleRagdoll()
	{
		RagdollInstance = null;
		IsRagdollSpawned = false;
	}

	public void CancelRagdoll()
		{
		if (RagdollInstance == null)
			return;
		var ragdoll = RagdollInstance.GetComponent<RagdollController>();
		ragdoll.CancelRagdoll();
		IsRagdollSpawned = false;
	}



	public void ActivateRagdoll()
	{
		IsRagdollSpawned = true;
		if (RagdollInstance == null)
			SpawnRagdoll();

		var ragdoll = RagdollInstance.GetComponent<RagdollController>();
		ragdoll.StartRagdoll();

		ragdoll.CopyPose(playerVisualRoot.transform);
		

		if (bulletImpact.force != 0)
		{
			var hitRigidbody = ragdoll.GetClosesRigidbody(bulletImpact.Position);
			if (hitRigidbody != null)
			{
				hitRigidbody.AddForceAtPosition(bulletImpact.Normal * bulletImpact.force, bulletImpact.Position, ForceMode.Impulse);
			}
		}
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
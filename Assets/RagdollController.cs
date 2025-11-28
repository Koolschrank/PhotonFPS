using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RagdollController : NetworkBehaviour
{
	private NetworkRigidbody3D[] ragdollBones;

	public Transform[] visuals;

	public Transform cameraFollowTarget;

	public NetworkObject root;

	void Awake()
	{
		ragdollBones = GetComponentsInChildren<NetworkRigidbody3D>();

		

		this.gameObject.SetActive(false);

	}

	public void SetVisuals(bool visible)
	{
		foreach (var t in visuals)
		{
			t.gameObject.SetActive(visible);
		}
	}
	
	public void CancelRagdoll()
	{
		this.gameObject.SetActive(false);
	}

	public void StartRagdoll()
	{
		if (ragdollBones.Length == 0)
		{
			ragdollBones = GetComponentsInChildren<NetworkRigidbody3D>();
			Debug.Log("Ragdoll bones were not initialized in Awake, initializing in StartRagdoll");
		}

		//SceneManager.MoveGameObjectToScene(gameObject, LocalPhysicsManager.LocalScene);
		this.gameObject.SetActive(true);
	}

	
	public void CopyPose(Transform rootToCopy)
	{
		Transform[] sourceBones = rootToCopy.GetComponentsInChildren<Transform>();
		Transform[] ragdollBonesTransforms = GetComponentsInChildren<Transform>();
		
		foreach (var ragdollBone in ragdollBonesTransforms)
		{
			foreach (var sourceBone in sourceBones)
			{
				NetworkRigidbody3D rigidbody3D = ragdollBone.GetComponent<NetworkRigidbody3D>();
				if (rigidbody3D !=  null &&ragdollBone.name == sourceBone.name)
				{
					rigidbody3D.RBIsKinematic = true;
					rigidbody3D.Teleport (sourceBone.position, sourceBone.rotation);
					rigidbody3D.RBIsKinematic = false;
					break;
				}
			}
		}

	}


	public NetworkRigidbody3D GetClosesRigidbody(Vector3 position)
	{
		NetworkRigidbody3D closest = null;
		float closestDistance = float.MaxValue;
		foreach (NetworkRigidbody3D rb in ragdollBones)
		{
			float distance = Vector3.Distance(rb.RBPosition, position);
			if (distance < closestDistance)
			{
				closest = rb;
				closestDistance = distance;
			}
		}
		return closest;
	}
}

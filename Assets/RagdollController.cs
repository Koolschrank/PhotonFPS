using Fusion;
using Fusion.Addons.Physics;
using System.Collections.Generic;
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

		this.GetComponent<NetworkTransform>().Teleport(rootToCopy.position, rootToCopy.rotation);


		// Build lookup for source bones by name (much faster)
		Transform[] sourceBones = rootToCopy.GetComponentsInChildren<Transform>();
		Dictionary<string, Transform> sourceLookup = new Dictionary<string, Transform>();
		foreach (var sb in sourceBones)
			sourceLookup[sb.name] = sb;

		Transform[] ragdollBones = GetComponentsInChildren<Transform>();

		// First pass: freeze & disable collisions
		foreach (var bone in ragdollBones)
		{
			var rb3D = bone.GetComponent<NetworkRigidbody3D>();
			if (rb3D == null) continue;

			rb3D.RBIsKinematic = true;
			rb3D.Rigidbody.detectCollisions = false;
			rb3D.Rigidbody.linearVelocity = Vector3.zero;
			rb3D.Rigidbody.angularVelocity = Vector3.zero;
		}

		// Second pass: copy pose
		foreach (var ragdollBone in ragdollBones)
		{
			var rb3D = ragdollBone.GetComponent<NetworkRigidbody3D>();
			if (rb3D == null) continue;

			if (sourceLookup.TryGetValue(ragdollBone.name, out Transform sourceBone))
			{
				// Safe: teleport while kinematic
				rb3D.Teleport(sourceBone.position, sourceBone.rotation);
			}
		}

		// Third pass: re-enable physics
		foreach (var bone in ragdollBones)
		{
			var rb3D = bone.GetComponent<NetworkRigidbody3D>();
			if (rb3D == null) continue;

			rb3D.Rigidbody.detectCollisions = true;
			rb3D.RBIsKinematic = false;
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

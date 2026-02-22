using Fusion;
using Fusion.Addons.Physics;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RagdollController : MonoBehaviour
{
	private Rigidbody[] ragdollBones;

	public Transform[] visuals;

	public Transform cameraFollowTarget;

	public Transform root;

	void Awake()
	{
		ragdollBones = GetComponentsInChildren<Rigidbody>();
		CancelRagdoll();

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
		SetVisuals(false);
		foreach (var rb in ragdollBones)
		{
			rb.isKinematic = true;
			rb.useGravity = false;
		}
	}

	public void StartRagdoll()
	{
		if (ragdollBones.Length == 0)
		{
			ragdollBones = GetComponentsInChildren<Rigidbody>();
			Debug.Log("Ragdoll bones were not initialized in Awake, initializing in StartRagdoll");
		}
		foreach (var rb in ragdollBones)
		{
			rb.isKinematic = false;
			rb.useGravity = true;
		}
		SetVisuals(true);

		//SceneManager.MoveGameObjectToScene(gameObject, LocalPhysicsManager.LocalScene);
		this.gameObject.SetActive(true);
	}


	public void CopyPose(Transform rootToCopy)
	{

		// Build lookup for source bones by name (much faster)
		Transform[] sourceBones = rootToCopy.GetComponentsInChildren<Transform>();
		Transform[] ragdollBones = root.GetComponentsInChildren<Transform>();

		foreach (Transform ragdollBone in ragdollBones)
		{
			foreach (Transform sourceBone in sourceBones)
			{
				if (ragdollBone.name == sourceBone.name)
				{
					ragdollBone.position = sourceBone.position;
					ragdollBone.rotation = sourceBone.rotation;
					break;
				}
			}
		}
	}



	public Rigidbody GetClosesRigidbody(Vector3 position)
	{
		Rigidbody closest = null;
		float closestDistance = float.MaxValue;
		foreach (Rigidbody rb in ragdollBones)
		{
			float distance = Vector3.Distance(rb.transform.position, position);
			if (distance < closestDistance)
			{
				closest = rb;
				closestDistance = distance;
			}
		}
		return closest;
	}
}

using UnityEngine;

public class RagdollController : MonoBehaviour
{
	private Rigidbody[] ragdollBones;

	public Transform[] visuals;

	void Awake()
	{
		ragdollBones = GetComponentsInChildren<Rigidbody>();
		SetRagdoll(false);
	}


	public void SetVisuals(bool visible)
	{
		foreach (var t in visuals)
		{
			t.gameObject.SetActive(visible);
		}
	}
	

	public void StartRagdoll()
	{
		SetRagdoll(true);
	}

	public void SetRagdoll(bool enabled)
	{
		SetVisuals(enabled);
		foreach (var rb in ragdollBones)
		{
			rb.isKinematic = !enabled;
			rb.useGravity = enabled;
		}
	}

	public Rigidbody GetClosesRigidbody(Vector3 position)
	{
		Rigidbody closest = null;
		float closestDistance = float.MaxValue;
		foreach (Rigidbody rb in ragdollBones)
		{
			float distance = Vector3.Distance(rb.position, position);
			if (distance < closestDistance)
			{
				closest = rb;
				closestDistance = distance;
			}
		}
		return closest;
	}
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class MapColliderCloner : MonoBehaviour
{
	[Header("Root of your map environment")]
	public Transform environmentRoot;

	void Start()
	{
		CloneColliders();
	}

	void CloneColliders()
	{
		foreach (var collider in environmentRoot.GetComponentsInChildren<Collider>())
		{
			CloneColliderObject(collider);
		}
	}

	void CloneColliderObject(Collider sourceCollider)
	{
		// Create empty clone
		GameObject clone = new GameObject(sourceCollider.name + "_ColliderClone");
		clone.transform.position = sourceCollider.transform.position;
		clone.transform.rotation = sourceCollider.transform.rotation;
		clone.transform.localScale = sourceCollider.transform.localScale;

		// Copy collider type
		System.Type type = sourceCollider.GetType();
		Collider newCol = (Collider)clone.AddComponent(type);

		// Copy collider settings
		CopyColliderValues(sourceCollider, newCol);

		// Move clone to the local physics scene
		SceneManager.MoveGameObjectToScene(
			clone,
			LocalPhysicsManager.LocalScene
		);
	}

	void CopyColliderValues(Collider src, Collider dst)
	{
		dst.isTrigger = src.isTrigger;
		dst.sharedMaterial = src.sharedMaterial;

		if (src is BoxCollider srcBox && dst is BoxCollider dstBox)
		{
			dstBox.center = srcBox.center;
			dstBox.size = srcBox.size;
		}
		else if (src is SphereCollider srcSphere && dst is SphereCollider dstSphere)
		{
			dstSphere.center = srcSphere.center;
			dstSphere.radius = srcSphere.radius;
		}
		else if (src is CapsuleCollider srcCapsule && dst is CapsuleCollider dstCapsule)
		{
			dstCapsule.center = srcCapsule.center;
			dstCapsule.height = srcCapsule.height;
			dstCapsule.radius = srcCapsule.radius;
			dstCapsule.direction = srcCapsule.direction;
		}
		else if (src is MeshCollider srcMesh && dst is MeshCollider dstMesh)
		{
			dstMesh.sharedMesh = srcMesh.sharedMesh;
			dstMesh.convex = srcMesh.convex;
			dstMesh.inflateMesh = srcMesh.inflateMesh;
			dstMesh.skinWidth = srcMesh.skinWidth;
		}
	}
}

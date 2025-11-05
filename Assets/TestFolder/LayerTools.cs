using UnityEngine;

public static class LayerTools
{
	/// <summary>
	/// Sets the layer of the given GameObject and all its children recursively.
	/// </summary>
	/// <param name="obj">The root GameObject to apply the layer to.</param>
	/// <param name="layer">The layer index to set (use LayerMask.NameToLayer("LayerName")).</param>
	public static void SetLayerRecursively(GameObject obj, int layer)
	{
		if (obj == null)
		{
			Debug.LogWarning("SetLayerRecursively called with a null GameObject.");
			return;
		}

		obj.layer = layer;

		foreach (Transform child in obj.transform)
		{
			if (child != null)
				SetLayerRecursively(child.gameObject, layer);
		}
	}
}

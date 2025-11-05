using UnityEngine;
using Cinemachine;
using System.Collections;

public static class CameraUtility
{
	/// <summary>
	/// Forces all cameras and CinemachineBrains to refresh,
	/// simulating what happens when you tweak a camera rect in the editor.
	/// </summary>
	public static void ForceCameraSystemRefresh()
	{
		
	}

	/// <summary>
	/// Call this after you change a camera rect.
	/// </summary>
	public static IEnumerator ForceRefreshNextFrame()
	{
		yield return null; // wait one frame so URP processes the rect change
		ForceCameraSystemRefresh();
	}
}

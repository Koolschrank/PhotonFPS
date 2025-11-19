using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayFromScene
{
	private const string startScene = "Assets/Scenes/Startup.unity";

	private static string previousScenePath;
	private static bool usedButton = false;

	static PlayFromScene()
	{
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	[MenuItem("Tools/Play From Start Scene %#p")]
	public static void PlayFromStartScene()
	{
		if (EditorApplication.isPlaying)
			return;

		usedButton = true;
		previousScenePath = SceneManager.GetActiveScene().path;

		if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			return;

		// Unity 6 official way to set play mode start scene:
		SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(startScene);
		EditorSceneManager.playModeStartScene = sceneAsset;

		EditorApplication.EnterPlaymode();
	}

	private static void OnPlayModeStateChanged(PlayModeStateChange state)
	{
		if (!usedButton)
			return;

		// When exiting play mode
		if (state == PlayModeStateChange.EnteredEditMode)
		{
			usedButton = false;

			// Reset play mode start scene back to "None"
			EditorSceneManager.playModeStartScene = null;

			if (!string.IsNullOrEmpty(previousScenePath))
			{
				// Restore the scene you were working on
				EditorSceneManager.OpenScene(previousScenePath);
			}
		}
	}
}

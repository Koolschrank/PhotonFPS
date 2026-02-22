using UnityEngine;
using UnityEngine.SceneManagement;

public class LocalPhysicsManager : MonoBehaviour
{
	public static PhysicsScene LocalPhysicsScene;
	public static Scene LocalScene;

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
		CreateLocalPhysicsScene();
	}

	private void CreateLocalPhysicsScene()
	{
		var parameters = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
		LocalScene = SceneManager.CreateScene("LocalRagdollScene", parameters);
		LocalPhysicsScene = LocalScene.GetPhysicsScene();
	}

	private void FixedUpdate()
	{

		LocalPhysicsScene.Simulate(Time.fixedDeltaTime);
	}
}

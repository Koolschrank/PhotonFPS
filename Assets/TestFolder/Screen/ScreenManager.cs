using UnityEngine;
using UnityEngine.Device;
using System.Collections;

namespace SimpleFPS
{
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

		

		public ScreenRectArray[] screenSetup;
        public Camera[] playerCameras;
		public Camera[] playerCameraOverlays;

		int localPlayers = 0;

        public int firstPersonLayerStart = 23;
        public int deadPlayerLayer = 31;

		private void Awake()
		{
			Instance = this;
		}


		public void LocalPlayerAdded()
        {
			
			foreach (var overlay in playerCameraOverlays)
			{
				if (overlay != null)
					overlay.gameObject.SetActive(false);
			}

				localPlayers++;
            for (int i = 0; i < playerCameras.Length; i++)
            {
				var camera = playerCameras[i];
				if (i >= localPlayers)
                {
                    camera.gameObject.SetActive(false);
				}
                else
                {
					
                    camera.gameObject.SetActive(true);
					camera.enabled = false;
					var screen = screenSetup[localPlayers - 1].screenRectValues[i];
					camera.rect = new Rect(screen.x, screen.y, screen.width, screen.height);
					camera.enabled = true;

					var overlay = playerCameraOverlays[i];
					if (overlay != null)
						overlay.gameObject.SetActive(true);

				}
			}
			StartCoroutine(CameraUtility.ForceRefreshNextFrame());

			


		}


	}
}

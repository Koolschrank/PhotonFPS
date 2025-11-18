using Fusion;
using UnityEngine;

namespace SimpleFPS
{
    public class PlayerUIManager : MonoBehaviour
    {
        public PlayerUI[] playerViews;

        public void SetActiveUIs(int playerCount)
        {
            for (int i = 0; i < playerViews.Length; i++)
            {
                if (i < playerCount)
                {
                    playerViews[i].gameObject.SetActive(true);
                }
                else
                {
                    playerViews[i].gameObject.SetActive(false);
                }
			}
		}

        public void InitilizeAllUIs(NetworkRunner runner)
        {
            for (int i = 0; i < playerViews.Length; i++)
            {
                playerViews[i].Initialize(runner);
			}
		}
	}
}

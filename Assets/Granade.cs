using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Android.AndroidGame;

namespace SimpleFPS
{
    public class Granade : NetworkBehaviour
	{
		[SerializeField]
		private GranadeProjectile _projectilePrefab;

		public int startUses = 3;
		public int maxUses = 5;
		public float throwForce = 10f;

		[Networked, HideInInspector]
		public int uses { get; set; }


		private SceneObjects _sceneObjects;


		public void AddUses(int usesToAdd)
        {
            uses += usesToAdd;
        }

		public override void Spawned()
		{
			if (HasStateAuthority)
			{
				uses = Mathf.Clamp(startUses, 0, maxUses);
			}

			_sceneObjects = Runner.GetSingleton<SceneObjects>();
		}


		public bool Throw(Vector3 throwPosition, Vector3 throwDirection)
		{
			if (uses <= 0)
				return false;

			if (HasStateAuthority)
			{
				Quaternion rotation = Quaternion.LookRotation(throwDirection, Vector3.up);

				var projectile = Runner.Spawn(_projectilePrefab, throwPosition, rotation, Object.InputAuthority) as GranadeProjectile;
				projectile.Throw(throwPosition, rotation, throwForce);
			}

			

			uses--;
			return true;
		}

		
	}
}

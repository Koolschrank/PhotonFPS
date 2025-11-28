using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace SimpleFPS
{
	[RequireComponent(typeof(NetworkRigidbody3D))]
	public class GranadeProjectile : NetworkBehaviour
	{

		private NetworkRigidbody3D _rigidbody;
		private Collider _collider;

		private void Awake()
		{
			_rigidbody = GetComponent<NetworkRigidbody3D>();
			_collider = GetComponent<Collider>();
		}

		// Update is called once per frame
		void Update()
        {
        
        }

        public void Throw(Vector3 position, Quaternion rotation, float impulse)
		{
			_rigidbody.Teleport(position, rotation);
			_rigidbody.Rigidbody.isKinematic = false;
			_rigidbody.Rigidbody.AddForce(transform.forward * impulse, ForceMode.Impulse);
		}




		

	}
}

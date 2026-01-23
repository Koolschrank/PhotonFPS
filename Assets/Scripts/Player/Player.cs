using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

using System;
using Unity.Cinemachine;

namespace SimpleFPS
{
	/// <summary>
	/// Main player script which handles input, movement, and visuals.
	/// Supports PlayerKey for multiple local players per device.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public class Player : NetworkBehaviour
	{
		[Header("Identity")]
		[Networked, OnChangedRender(nameof(LocalIndex_OnChangedRender))]
		public int LocalIndex { get; set; } = -1;




		[Header("Components")]
		public SimpleKCC KCC;
		public Weapons Weapons;
		public Health Health;
		public Animator Animator;
		public HitboxRoot HitboxRoot;
		public CinemachineCamera thirdPersonCamera;
		public RagdollSpawner RagdollSpawner;

		[Header("Setup")]
		public float MoveSpeed = 6f;
		public float JumpForce = 10f;
		public AudioSource JumpSound;
		public AudioClip[] JumpClips;
		public Transform CameraHandle;
		public GameObject FirstPersonRoot;
		public GameObject ThirdPersonRoot;
		public NetworkObject SprayPrefab;

		[Header("Movement")]
		public float UpGravity = 15f;
		public float DownGravity = 25f;
		public float GroundAcceleration = 55f;
		public float GroundDeceleration = 25f;
		public float AirAcceleration = 25f;
		public float AirDeceleration = 1.3f;

		[Networked] private NetworkButtons _previousButtons { get; set; }
		[Networked] private int _jumpCount { get; set; }
		[Networked] private Vector3 _moveVelocity { get; set; }

		private int _visibleJumpCount;
		private SceneObjects _sceneObjects;
		[NonSerialized]
		public bool isSpawned = false;

		#region Spawn & Networked Lifecycle

		public override void Spawned()
		{

			isSpawned = true;
			SetVisuals(HasInputAuthority, true);

			if (!HasInputAuthority)
			{
				var virtualCams = GetComponentsInChildren<CinemachineCamera>(true);
				foreach (var cam in virtualCams)
					cam.enabled = false;
			}

			_sceneObjects = Runner.GetSingleton<SceneObjects>();



			if (LocalIndex >= 0)
				LocalIndex_OnChangedRender();
		}


		private void LocalIndex_OnChangedRender()
		{
			if (HasInputAuthority && isSpawned)
			{
				
				var screenManager = ScreenManager.Instance;
				int cameraLayer = screenManager.firstPersonLayerStart + LocalIndex *2;
				var virtualCam = CameraHandle.GetComponentInChildren<CinemachineCamera>(true);
				virtualCam.gameObject.layer = cameraLayer;
				virtualCam.OutputChannel = (OutputChannels)(1 << (LocalIndex +1 ));
				thirdPersonCamera.OutputChannel = (OutputChannels)(1 << (LocalIndex + 1));

				int thirdPersonLayer = cameraLayer + 1;
				LayerTools.SetLayerRecursively(FirstPersonRoot, cameraLayer);
				LayerTools.SetLayerRecursively(ThirdPersonRoot, thirdPersonLayer);
				ThirdPersonRoot.SetActive(true);

				Weapons.SetFirstPersonLayer(cameraLayer);
				Weapons.SetThirdPersonLayer(thirdPersonLayer);
				SetVisuals(true, true);
				
				_sceneObjects.Device.LocalPlayerObjectSpawned(this);
				name = $"{Object.InputAuthority} (LocalIndex {LocalIndex})";
			}
		}

		

		public void SwitchToThirdPersonCamera()
		{
			var virtualCam = CameraHandle.GetComponentInChildren<CinemachineCamera>(true);
			virtualCam.gameObject.SetActive(false);

			thirdPersonCamera.gameObject.SetActive(true);

			var ragdoll = RagdollSpawner.RagdollInstance.GetComponent<RagdollController>().cameraFollowTarget;

			thirdPersonCamera.GetComponent<CinemachineCamera>().LookAt = ragdoll;

			var screenManager = ScreenManager.Instance;
			LayerTools.SetLayerRecursively(ThirdPersonRoot, 0);//screenManager.deadPlayerLayer);

		}


		public override void FixedUpdateNetwork()
		{
			if (_sceneObjects.Gameplay.State == EGameplayState.Finished)
			{
				MovePlayer();
				return;
			}

			if (!Health.IsAlive)
			{
				MovePlayer();
				KCC.SetColliderLayer(LayerMask.NameToLayer("Ignore Raycast"));
				KCC.SetCollisionLayerMask(LayerMask.GetMask("Default"));
				HitboxRoot.HitboxRootActive = false;
				
				return;
			}

			// Get input
			if (GetInput(out NetworkedInput netInput))
			{
				var inputSlot = netInput[LocalIndex]; // pick the slot based on LocalIndex
				ProcessInput(inputSlot);
			}
			else
			{
				MovePlayer();
				RefreshCamera();
			}

			
		}

		public override void Render()
		{
			if (_sceneObjects.Gameplay.State == EGameplayState.Finished) return;

			var moveVel = GetAnimationMoveVelocity();

			Animator.SetFloat(AnimatorId.LocomotionTime, Time.time * 2f);
			Animator.SetBool(AnimatorId.IsGrounded, KCC.IsGrounded);
			Animator.SetBool(AnimatorId.IsReloading, Weapons.CurrentWeapon.IsReloading);
			Animator.SetFloat(AnimatorId.MoveX, moveVel.x, 0.05f, Time.deltaTime);
			Animator.SetFloat(AnimatorId.MoveZ, moveVel.z, 0.05f, Time.deltaTime);
			Animator.SetFloat(AnimatorId.MoveSpeed, moveVel.magnitude);
			Animator.SetFloat(AnimatorId.Look, -KCC.GetLookRotation(true, false).x / 90f);

			if (_visibleJumpCount < _jumpCount)
			{
				Animator.SetTrigger("Jump");
				JumpSound.clip = JumpClips[UnityEngine.Random.Range(0, JumpClips.Length)];
				JumpSound.Play();
			}

			_visibleJumpCount = _jumpCount;

			if (!Health.IsAlive)
			{
				if (!RagdollSpawner.IsRagdollSpawned)
				{
					RagdollSpawner.bulletImpact = Health.ragdollBulletImpact;
					RagdollSpawner.SpawnRagdoll();
					SetVisuals(false, !RagdollSpawner.IsRagdollSpawned);
					SwitchToThirdPersonCamera();
				}


				
			}
			else
			{

				// resets to alive in case player died but host disagreed with death
				if (RagdollSpawner.IsRagdollSpawned)
				{
					RagdollSpawner.bulletImpact = Health.ragdollBulletImpact;
					RagdollSpawner.CancelRagdoll();
					SetVisuals(HasInputAuthority, true);
					// TODO: switch to first person camera still needed
				}
			}


		}

		private void LateUpdate()
		{
			if (HasInputAuthority)
				RefreshCamera();
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			isSpawned = false;
		}

		#endregion

		#region Input & Movement

		private void ProcessInput(NetworkedInputPlayer input)
		{
			KCC.AddLookRotation(new Vector2(-input.LookRotationDelta.y, input.LookRotationDelta.x)  , -89f, 89f);
			KCC.SetGravity(KCC.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

			Vector3 moveDir = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
			float jumpImpulse = 0f;

			if (input.Buttons.WasPressed(_previousButtons, EInputButton.Jump) && KCC.IsGrounded)
				jumpImpulse = JumpForce;

			MovePlayer(moveDir * MoveSpeed, jumpImpulse);
			RefreshCamera();

			if (KCC.HasJumped) _jumpCount++;

			ProcessWeaponInput(input);

			

			if (input.Buttons.WasPressed(_previousButtons, EInputButton.Spray) && HasStateAuthority)
			{
				if (Runner.GetPhysicsScene().Raycast(CameraHandle.position, KCC.LookDirection,
					out var hit, 2.5f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
				{
					Quaternion sprayRot = hit.normal.y > 0.9f ? KCC.TransformRotation : Quaternion.identity;
					Runner.Spawn(SprayPrefab, hit.point, sprayRot * Quaternion.LookRotation(-hit.normal));
				}
			}

			_previousButtons = input.Buttons;
		}

		public void ProcessWeaponInput(NetworkedInputPlayer input)
		{
			
		}

		private void MovePlayer(Vector3 desiredMoveVelocity = default, float jumpImpulse = 0f)
		{
			float acceleration = desiredMoveVelocity == Vector3.zero
				? (KCC.IsGrounded ? GroundDeceleration : AirDeceleration)
				: (KCC.IsGrounded ? GroundAcceleration : AirAcceleration);

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
			KCC.Move(_moveVelocity, jumpImpulse);
		}

		#endregion

		#region Utilities

		private void RefreshCamera()
		{
			Vector2 pitch = KCC.GetLookRotation(true, false);
			CameraHandle.localRotation = Quaternion.Euler(pitch);
		}

		private void SetVisuals(bool firstPerson, bool thirdPerson)
		{
			FirstPersonRoot.SetActive(firstPerson);
			ThirdPersonRoot.SetActive(thirdPerson);
			Weapons.SetFirstPersonVisuals(firstPerson);
			Weapons.SetThirdPersonVisuals(thirdPerson);
		}

		private Vector3 GetAnimationMoveVelocity()
		{
			if (KCC.RealSpeed < 0.01f) return Vector3.zero;
			Vector3 velocity = KCC.RealVelocity;
			velocity.y = 0;
			if (velocity.sqrMagnitude > 1f) velocity.Normalize();
			return transform.InverseTransformVector(velocity);
		}

		public void PlayFireEffect()
		{
			if (Mathf.Abs(GetAnimationMoveVelocity().x) > 0.2f) return;
			Animator.SetTrigger("Fire");
		}

		#endregion
	}
}

using Unity.Mathematics;
using UnityEngine;
using Voxels.World;

namespace Voxels.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(50)]
    public sealed class FlatPlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float sprintMultiplier = 1.65f;
        [SerializeField] float jumpHeight = 1.15f;
        [SerializeField] float gravity = 24f;
        [SerializeField] LayerMask groundMask = ~0;

        CharacterController controller;
        Transform viewTransform;
        WorldScroller scroller;
        HexChunkManager chunkManager;
        WorldSettings settings;
        float verticalVelocity;
        HexCoord lastNotifiedHex;

        public bool IsGrounded { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Initialize(
            WorldSettings worldSettings,
            WorldScroller worldScroller,
            HexChunkManager chunks,
            Transform view)
        {
            settings = worldSettings;
            scroller = worldScroller;
            chunkManager = chunks;
            viewTransform = view != null ? view : GetComponentInChildren<Camera>()?.transform;
            ConfigureCapsule();
            lastNotifiedHex = scroller != null ? scroller.PlayerWorldHex : HexCoord.Zero;
        }

        void ConfigureCapsule()
        {
            if (controller == null || settings == null)
            {
                return;
            }

            float height = math.max(1f, settings.PlayerHeight);
            controller.height = height;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            controller.stepOffset = math.min(0.4f, height * 0.35f);
            controller.slopeLimit = 55f;
        }

        void Update()
        {
            if (!isActiveAndEnabled || controller == null || settings == null)
            {
                return;
            }

            Vector3 positionBefore = transform.position;
            HandleMovement();
            Vector3 displacement = transform.position - positionBefore;
            if (scroller != null)
            {
                scroller.AddWorldOffset(displacement);
            }

            NotifyHexChange();
        }

        void NotifyHexChange()
        {
            if (scroller == null || chunkManager == null)
            {
                return;
            }

            HexCoord hex = scroller.PlayerWorldHex;
            if (hex == lastNotifiedHex)
            {
                return;
            }

            lastNotifiedHex = hex;
            chunkManager.RefreshAroundPlayer();
        }

        void HandleMovement()
        {
            IsGrounded = controller.isGrounded;
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (IsGrounded && GameInput.WasJumpPressed())
            {
                verticalVelocity = math.sqrt(jumpHeight * 2f * gravity);
            }

            verticalVelocity -= gravity * Time.deltaTime;

            Vector2 input = GameInput.ReadMoveAxes();
            Vector3 move = Vector3.zero;
            if (input.sqrMagnitude > 0.0001f)
            {
                Transform facing = viewTransform != null ? viewTransform : transform;
                input = Vector2.ClampMagnitude(input, 1f);
                Vector3 forward = Vector3.ProjectOnPlane(facing.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float speed = moveSpeed * (GameInput.IsSprintHeld() ? sprintMultiplier : 1f);
                move = (forward * input.y + right * input.x) * speed;
            }

            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }

        public void SnapToGround()
        {
            if (controller == null)
            {
                return;
            }

            Vector3 origin = transform.position + Vector3.up * 4f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 12f, groundMask, QueryTriggerInteraction.Ignore))
            {
                controller.enabled = false;
                transform.position = hit.point + Vector3.up * controller.radius;
                controller.enabled = true;
                verticalVelocity = 0f;
            }
        }
    }
}

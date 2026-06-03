using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(50)]
    public sealed class FlatPlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float sprintMultiplier = 1.65f;
        [SerializeField] float creativeSpeedMultiplier = 2.2f;
        [SerializeField] float jumpHeight = 1.15f;
        [SerializeField] float gravity = 24f;
        [SerializeField] float coyoteTime = 0.12f;
        [SerializeField] float jumpBufferTime = 0.12f;
        [SerializeField] float creativeVerticalSpeed = 6f;
        [SerializeField] float swimVerticalSpeed = 3f;
        [SerializeField] LayerMask groundMask = ~0;

        CharacterController controller;
        Transform viewTransform;
        WorldScroller scroller;
        HexChunkManager chunkManager;
        WorldSettings settings;
        HexWorld hexWorld;
        float verticalVelocity;
        float coyoteTimer;
        float jumpBufferTimer;
        HexCoord lastNotifiedHex;
        WaterDepthTier waterDepth;

        public bool IsGrounded { get; private set; }
        public bool IsSwimming { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Initialize(
            WorldSettings worldSettings,
            WorldScroller worldScroller,
            HexChunkManager chunks,
            Transform view,
            HexWorld world = null)
        {
            settings = worldSettings;
            scroller = worldScroller;
            chunkManager = chunks;
            hexWorld = world ?? worldScroller?.HexWorld;
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
            if (IsCreativeMode())
            {
                HandleCreativeMovement();
            }
            else if (IsSwimming)
            {
                HandleSwimMovement();
            }
            else
            {
                HandleSurvivalMovement();
            }

            Vector3 displacement = transform.position - positionBefore;
            if (scroller != null)
            {
                scroller.AddWorldOffset(displacement);
            }

            NotifyHexChange();
        }

        bool IsCreativeMode() =>
            PlayerGameplayState.Instance != null && PlayerGameplayState.Instance.CreativeMode;

        void HandleSwimMovement()
        {
            IsGrounded = false;
            verticalVelocity = 0f;

            if (GameInput.WasJumpPressed())
            {
                verticalVelocity = swimVerticalSpeed;
            }
            else if (GameInput.IsDescendHeld())
            {
                verticalVelocity = -swimVerticalSpeed;
            }

            Vector2 input = GameInput.ReadMoveAxes();
            Vector3 move = Vector3.up * verticalVelocity;
            if (input.sqrMagnitude > 0.0001f)
            {
                Transform facing = viewTransform != null ? viewTransform : transform;
                input = Vector2.ClampMagnitude(input, 1f);
                Vector3 forward = Vector3.ProjectOnPlane(facing.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float speed = settings.SwimSpeed * (waterDepth == WaterDepthTier.Deep ? 1.15f : 0.85f) *
                    (GameInput.IsSprintHeld() ? sprintMultiplier : 1f);
                move += (forward * input.y + right * input.x) * speed;
            }

            controller.Move(move * Time.deltaTime);
        }

        void HandleCreativeMovement()
        {
            IsGrounded = false;
            IsSwimming = false;
            verticalVelocity = 0f;

            if (GameInput.WasJumpPressed())
            {
                verticalVelocity = creativeVerticalSpeed;
            }
            else if (GameInput.IsDescendHeld())
            {
                verticalVelocity = -creativeVerticalSpeed;
            }

            Vector2 input = GameInput.ReadMoveAxes();
            Vector3 move = Vector3.up * verticalVelocity;
            if (input.sqrMagnitude > 0.0001f)
            {
                Transform facing = viewTransform != null ? viewTransform : transform;
                input = Vector2.ClampMagnitude(input, 1f);
                Vector3 forward = Vector3.ProjectOnPlane(facing.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float speed = moveSpeed * creativeSpeedMultiplier *
                    (GameInput.IsSprintHeld() ? sprintMultiplier : 1f);
                move += (forward * input.y + right * input.x) * speed;
            }

            controller.Move(move * Time.deltaTime);
        }

        void HandleSurvivalMovement()
        {
            waterDepth = FluidHelper.GetPlayerWaterDepth(hexWorld, settings, scroller, transform);
            IsSwimming = waterDepth != WaterDepthTier.None;
            if (IsSwimming)
            {
                return;
            }

            bool wasGrounded = IsGrounded;
            IsGrounded = controller.isGrounded;

            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }

                if (jumpBufferTimer > 0f)
                {
                    verticalVelocity = math.sqrt(jumpHeight * 2f * gravity);
                    jumpBufferTimer = 0f;
                }
            }
            else if (wasGrounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer = math.max(0f, coyoteTimer - Time.deltaTime);
            }

            if (GameInput.WasJumpPressed())
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer = math.max(0f, jumpBufferTimer - Time.deltaTime);
            }

            if (coyoteTimer > 0f && jumpBufferTimer > 0f)
            {
                verticalVelocity = math.sqrt(jumpHeight * 2f * gravity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            float gravityScale = waterDepth == WaterDepthTier.Deep ? 0.35f : 1f;
            verticalVelocity -= gravity * gravityScale * Time.deltaTime;

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

using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Sphere;
using Voxels.World;

namespace Voxels.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAnchor))]
    [DefaultExecutionOrder(50)]
    public sealed class SurfacePlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float sprintMultiplier = 1.65f;
        [SerializeField] float jumpHeight = 1.15f;
        [SerializeField] float gravity = 24f;
        [SerializeField] float groundStickVelocity = 2f;
        [SerializeField] float groundProbeDistance = 0.45f;
        [SerializeField] float surfaceAlignSpeed = 14f;
        [SerializeField] LayerMask groundMask = ~0;

        CharacterController controller;
        PlayerAnchor anchor;
        Transform viewTransform;
        Transform planetTransform;
        SurfaceSpawnCamera surfaceCamera;
        PlanetWorld planetWorld;

        int currentCellIndex = -1;
        float radialVelocity;
        Vector3 planetLocalPosition;
        Quaternion planetLocalBodyRotation = Quaternion.identity;
        bool planetLocalStateValid;

        public bool IsGrounded { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            anchor = GetComponent<PlayerAnchor>();
        }

        public void Initialize(PlanetWorld world, Transform planet, Transform view, SurfaceSpawnCamera camera)
        {
            planetWorld = world;
            planetTransform = planet != null ? planet : transform.parent;
            viewTransform = view != null ? view : GetComponentInChildren<Camera>()?.transform;
            surfaceCamera = camera;
            currentCellIndex = anchor != null ? anchor.SpawnCellIndex : -1;
            radialVelocity = 0f;

            if (controller != null)
            {
                controller.enabled = true;
            }

            ConfigureCapsuleFromSettings();
            SnapToGround();
            AlignOrientationToSurface();
            RefreshCellTracking();
            SyncPlanetLocalState();
            ApplyPlanetRotationCarry();
        }

        void ConfigureCapsuleFromSettings()
        {
            if (controller == null || planetWorld == null)
            {
                return;
            }

            float height = math.max(1f, planetWorld.Settings.PlayerHeight);
            controller.height = height;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            controller.stepOffset = math.min(0.4f, height * 0.35f);
            controller.slopeLimit = 55f;
        }

        void Update()
        {
            if (!isActiveAndEnabled || planetWorld == null || controller == null || planetTransform == null)
            {
                return;
            }

            if (surfaceCamera != null && surfaceCamera.IsOrbitMode)
            {
                return;
            }

            ApplyPlanetRotationCarry();
            HandleMovement();
            AlignOrientationToSurface();
        }

        void LateUpdate()
        {
            if (!isActiveAndEnabled || planetWorld == null || planetTransform == null)
            {
                return;
            }

            if (surfaceCamera != null && surfaceCamera.IsOrbitMode)
            {
                return;
            }

            ApplyPlanetRotationCarry();
            RefreshCellTracking();
        }

        void ApplyPlanetRotationCarry()
        {
            if (!planetLocalStateValid || planetTransform == null || controller == null)
            {
                return;
            }

            Vector3 expectedPosition = planetTransform.TransformPoint(planetLocalPosition);
            Quaternion expectedRotation = planetTransform.rotation * planetLocalBodyRotation;

            if ((transform.position - expectedPosition).sqrMagnitude <= 0.000001f &&
                Quaternion.Angle(transform.rotation, expectedRotation) <= 0.01f)
            {
                return;
            }

            controller.enabled = false;
            transform.SetPositionAndRotation(expectedPosition, expectedRotation);
            controller.enabled = true;
        }

        void SyncPlanetLocalState()
        {
            if (planetTransform == null)
            {
                planetLocalStateValid = false;
                return;
            }

            planetLocalPosition = planetTransform.InverseTransformPoint(transform.position);
            planetLocalBodyRotation = Quaternion.Inverse(planetTransform.rotation) * transform.rotation;
            planetLocalStateValid = planetLocalPosition.sqrMagnitude > 0.0001f;
        }

        void RefreshCellTracking()
        {
            if (planetWorld == null || planetTransform == null)
            {
                return;
            }

            Vector3 localPosition = planetTransform.InverseTransformPoint(transform.position);
            if (localPosition.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float3 localDirection = math.normalize((float3)localPosition);
            currentCellIndex = PlanetSurfaceLocator.RefineNearestCell(
                planetWorld.Grid,
                currentCellIndex,
                localDirection);

            ref readonly SphereHexCell cell = ref planetWorld.Grid.GetCell(currentCellIndex);
            anchor?.TrackSurface(
                currentCellIndex,
                cell.Normal,
                planetWorld.GetCellSurfaceWorldRadius(currentCellIndex));
        }

        void HandleMovement()
        {
            if (!TryGetPlanetRadial(out Vector3 radialOut, out _))
            {
                return;
            }

            IsGrounded = CheckGrounded(radialOut);
            Vector2 input = GameInput.ReadMoveAxes();
            bool jumpPressed = GameInput.WasJumpPressed();

            if (IsGrounded && input.sqrMagnitude <= 0.0001f && !jumpPressed)
            {
                radialVelocity = -groundStickVelocity;
                return;
            }

            if (IsGrounded && radialVelocity < 0f)
            {
                radialVelocity = -groundStickVelocity;
            }

            if (IsGrounded && jumpPressed)
            {
                radialVelocity = math.sqrt(jumpHeight * 2f * gravity);
            }

            radialVelocity -= gravity * Time.deltaTime;

            Vector3 tangentMove = Vector3.zero;
            if (input.sqrMagnitude > 0.0001f)
            {
                Transform facingTransform = viewTransform != null ? viewTransform : transform;
                input = Vector2.ClampMagnitude(input, 1f);
                Vector3 forward = Vector3.ProjectOnPlane(facingTransform.forward, radialOut);
                if (forward.sqrMagnitude < 0.0001f)
                {
                    forward = Vector3.ProjectOnPlane(facingTransform.up, radialOut);
                }

                forward.Normalize();
                Vector3 right = Vector3.Cross(radialOut, forward).normalized;
                float speed = moveSpeed * (GameInput.IsSprintHeld() ? sprintMultiplier : 1f);
                tangentMove = (forward * input.y + right * input.x) * speed;
            }

            Vector3 velocity = tangentMove + radialOut * radialVelocity;
            controller.Move(velocity * Time.deltaTime);
            SyncPlanetLocalState();
        }

        void AlignOrientationToSurface()
        {
            Vector3 groundNormal = TryGetGroundNormal(out Vector3 hitNormal)
                ? hitNormal
                : GetFallbackRadialOut();

            if (groundNormal.sqrMagnitude < 0.0001f)
            {
                return;
            }

            groundNormal.Normalize();
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                math.saturate(surfaceAlignSpeed * Time.deltaTime));

            if (planetTransform != null)
            {
                planetLocalBodyRotation = Quaternion.Inverse(planetTransform.rotation) * transform.rotation;
            }
        }

        void SnapToGround()
        {
            if (!TryGetPlanetRadial(out Vector3 radialOut, out _))
            {
                return;
            }

            Vector3 origin = transform.position + radialOut * 3f;
            if (Physics.Raycast(origin, -radialOut, out RaycastHit hit, 8f, groundMask, QueryTriggerInteraction.Ignore))
            {
                controller.enabled = false;
                transform.position = hit.point + hit.normal * controller.radius;
                controller.enabled = true;
                radialVelocity = 0f;
            }
        }

        bool CheckGrounded(Vector3 radialOut)
        {
            if (controller.isGrounded)
            {
                return true;
            }

            float probeDistance = groundProbeDistance + controller.skinWidth;
            Vector3 origin = transform.position + radialOut * 0.1f;
            return Physics.Raycast(origin, -radialOut, probeDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        bool TryGetGroundNormal(out Vector3 groundNormal)
        {
            groundNormal = Vector3.up;
            if (!TryGetPlanetRadial(out Vector3 radialOut, out _))
            {
                return false;
            }

            Vector3 origin = transform.position + radialOut * 0.5f;
            if (Physics.Raycast(origin, -radialOut, out RaycastHit hit, 2.5f, groundMask, QueryTriggerInteraction.Ignore))
            {
                groundNormal = hit.normal;
                return true;
            }

            return false;
        }

        bool TryGetPlanetRadial(out Vector3 radialOut, out Vector3 toCenter)
        {
            radialOut = Vector3.up;
            toCenter = Vector3.zero;
            if (planetTransform == null)
            {
                return false;
            }

            Vector3 localPosition = planetTransform.InverseTransformPoint(transform.position);
            if (localPosition.sqrMagnitude > 1f)
            {
                radialOut = planetTransform.TransformDirection(localPosition.normalized);
                toCenter = planetTransform.position - transform.position;
                return true;
            }

            if (anchor != null && math.lengthsq(anchor.SurfaceUp) > 0.0001f)
            {
                radialOut = planetTransform.TransformDirection((Vector3)anchor.SurfaceUp);
                toCenter = -radialOut;
                return true;
            }

            toCenter = planetTransform.position - transform.position;
            if (toCenter.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            radialOut = -toCenter.normalized;
            return true;
        }

        Vector3 GetFallbackRadialOut()
        {
            return TryGetPlanetRadial(out Vector3 radialOut, out _) ? radialOut : transform.up;
        }
    }
}

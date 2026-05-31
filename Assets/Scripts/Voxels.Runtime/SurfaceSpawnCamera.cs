using Unity.Mathematics;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Voxels.Core.Sphere;
using Voxels.World;

namespace Voxels.Runtime
{
    /// <summary>
    /// Spawns the camera standing on the surface so the world reads like a game, not a distant orb.
    /// Hold Tab to temporarily free-orbit; release to snap back to the surface view.
    /// </summary>
    public sealed class SurfaceSpawnCamera : MonoBehaviour
    {
        [SerializeField] bool spawnOnStart = true;
        [SerializeField] float lookPitchOffset = 4f;
        [SerializeField] float orbitSpeed = 90f;
        [SerializeField] float scrollSensitivity = 0.4f;
        [SerializeField] float minEyeRadiusOffset = 1.7f;
        [SerializeField] float maxEyeRadiusOffset = 8f;

        int spawnedCellIndex = -1;
        float surfaceRadius;
        float orbitReferenceRadius;
        float eyeRadiusOffset;
        float yaw;
        float pitch;
        float3 surfaceNormal;
        float3 surfacePosition;
        bool orbitMode;

        void Start()
        {
            if (spawnOnStart && !HasSpawned)
            {
                TrySpawnOnSurface();
            }
        }

        void LateUpdate()
        {
            if (spawnedCellIndex < 0)
            {
                return;
            }

            HandleModeToggle();
            HandleInput();
            ApplyTransform();
        }

        public Vector3 SpawnGroundPosition => (Vector3)surfacePosition;
        public bool HasSpawned => spawnedCellIndex >= 0;

        public bool TrySpawnOnSurface()
        {
            PlanetBootstrap bootstrap = FindAnyObjectByType<PlanetBootstrap>();
            if (bootstrap == null || bootstrap.PlanetWorld == null)
            {
                Debug.LogWarning("SurfaceSpawnCamera: Planet not ready yet.");
                return false;
            }

            PlanetWorld world = bootstrap.PlanetWorld;
            PlanetSettings settings = world.Settings;
            int seaLevel = settings.SeaLevelLayer;
            int bestCellIndex = -1;
            int bestScore = int.MaxValue;

            for (int attempt = 0; attempt < world.Grid.CellCount; attempt++)
            {
                int cellIndex = (attempt * 7919) % world.Grid.CellCount;
                BlockColumn column = world.Columns.GetColumn(cellIndex);
                int surfaceHeight = column.SurfaceHeight;

                if (surfaceHeight <= seaLevel)
                {
                    continue;
                }

                int heightAboveSea = surfaceHeight - seaLevel;
                int score = math.abs(heightAboveSea - 2) * 10;
                if (HasAdjacentWater(world, cellIndex, seaLevel))
                {
                    score -= 25;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCellIndex = cellIndex;
                    if (score <= -15)
                    {
                        break;
                    }
                }
            }

            if (bestCellIndex >= 0)
            {
                ConfigureSpawn(world, settings, bestCellIndex);
                FaceAdjacentWater(world, bestCellIndex, settings.SeaLevelLayer);
                return true;
            }

            for (int attempt = 0; attempt < world.Grid.CellCount; attempt++)
            {
                int cellIndex = (attempt * 7919) % world.Grid.CellCount;
                BlockColumn column = world.Columns.GetColumn(cellIndex);
                if (column.SurfaceHeight <= seaLevel)
                {
                    continue;
                }

                ConfigureSpawn(world, settings, cellIndex);
                return true;
            }

            Debug.LogWarning("SurfaceSpawnCamera: no land cell found, using planet exterior fallback.");
            spawnedCellIndex = 0;
            surfaceRadius = world.ApproximateOuterRadius;
            orbitReferenceRadius = surfaceRadius;
            surfaceNormal = new float3(0f, 1f, 0f);
            eyeRadiusOffset = settings.PlayerEyeHeight;
            surfacePosition = surfaceNormal * surfaceRadius;
            pitch = lookPitchOffset;
            ApplyTransform();
            return true;
        }

        void ConfigureSpawn(PlanetWorld world, PlanetSettings settings, int cellIndex)
        {
            ref readonly SphereHexCell cell = ref world.Grid.GetCell(cellIndex);
            spawnedCellIndex = cellIndex;
            surfaceNormal = cell.Normal;
            surfaceRadius = world.GetCellSurfaceWorldRadius(cellIndex);
            orbitReferenceRadius = surfaceRadius;
            eyeRadiusOffset = settings.PlayerEyeHeight;
            surfacePosition = surfaceNormal * surfaceRadius;

            float3 tangent = math.abs(surfaceNormal.y) < 0.95f
                ? math.normalize(math.cross(new float3(0f, 1f, 0f), surfaceNormal))
                : math.normalize(math.cross(new float3(1f, 0f, 0f), surfaceNormal));
            float3 forward = math.normalize(math.cross(surfaceNormal, tangent));
            yaw = math.degrees(math.atan2(forward.x, forward.z));
            pitch = lookPitchOffset;
            orbitMode = false;
            ApplyTransform();
        }

        static bool HasAdjacentWater(PlanetWorld world, int cellIndex, int seaLevel)
        {
            ref readonly SphereHexCell cell = ref world.Grid.GetCell(cellIndex);
            for (int i = 0; i < cell.NeighborCount; i++)
            {
                BlockColumn neighborColumn = world.Columns.GetColumn(cell.Neighbors[i]);
                if (neighborColumn.SurfaceHeight <= seaLevel)
                {
                    return true;
                }
            }

            return false;
        }

        void FaceAdjacentWater(PlanetWorld world, int cellIndex, int seaLevel)
        {
            ref readonly SphereHexCell cell = ref world.Grid.GetCell(cellIndex);
            for (int i = 0; i < cell.NeighborCount; i++)
            {
                int neighborIndex = cell.Neighbors[i];
                BlockColumn neighborColumn = world.Columns.GetColumn(neighborIndex);
                if (neighborColumn.SurfaceHeight > seaLevel)
                {
                    continue;
                }

                ref readonly SphereHexCell neighbor = ref world.Grid.GetCell(neighborIndex);
                float3 toWater = math.normalize(neighbor.Normal - surfaceNormal * math.dot(neighbor.Normal, surfaceNormal));
                if (math.lengthsq(toWater) < 0.001f)
                {
                    continue;
                }

                yaw = math.degrees(math.atan2(toWater.x, toWater.z));
                ApplyTransform();
                return;
            }
        }

        /// <summary>
        /// After the planet root is shifted so spawn ground sits at world origin, keep camera math local.
        /// </summary>
        public void ApplyPlanetRecenter()
        {
            surfacePosition = float3.zero;
            surfaceRadius = 0f;
            orbitMode = false;
            pitch = lookPitchOffset;
            ApplyTransform();
        }

        void HandleModeToggle()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                orbitMode = !orbitMode;
            }
#else
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                orbitMode = !orbitMode;
            }
#endif
        }

        void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * orbitSpeed * Time.deltaTime;
                pitch -= delta.y * orbitSpeed * Time.deltaTime;
            }

            float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                eyeRadiusOffset -= scroll * scrollSensitivity;
            }
#else
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
            }

            eyeRadiusOffset -= Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity * 10f;
#endif

            if (!orbitMode)
            {
                pitch = Mathf.Clamp(pitch, -15f, 35f);
            }

            eyeRadiusOffset = Mathf.Clamp(eyeRadiusOffset, minEyeRadiusOffset, maxEyeRadiusOffset);
        }

        void ApplyTransform()
        {
            if (orbitMode)
            {
                float shellRadius = surfaceRadius > 0f ? surfaceRadius : orbitReferenceRadius;
                float orbitDistance = shellRadius + eyeRadiusOffset;
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 offset = rotation * new Vector3(0f, 0f, -orbitDistance);
                transform.position = offset;
                transform.rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
                return;
            }

            Quaternion localLook = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 up = surfaceNormal;
            Vector3 forwardOnSurface = Vector3.ProjectOnPlane(localLook * Vector3.forward, up).normalized;
            if (forwardOnSurface.sqrMagnitude < 0.001f)
            {
                forwardOnSurface = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
            }

            transform.position = (Vector3)(surfacePosition + (float3)up * eyeRadiusOffset);
            transform.rotation = Quaternion.LookRotation(forwardOnSurface, up);
        }
    }
}

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
    /// First-person style view standing on the surface. Hold Tab for temporary orbit debug view.
    /// </summary>
    public sealed class SurfaceSpawnCamera : MonoBehaviour
    {
        [SerializeField] bool spawnOnStart;
        [SerializeField] float lookPitchDown = 8f;
        [SerializeField] float orbitSpeed = 90f;
        [SerializeField] float scrollSensitivity = 0.15f;
        [SerializeField] float minEyeHeight = 1.5f;
        [SerializeField] float maxEyeHeight = 3.5f;

        int spawnedCellIndex = -1;
        float eyeHeight;
        float yaw;
        float pitch;
        float3 surfaceUp;
        float orbitReferenceRadius;
        bool orbitMode;

        public Vector3 SpawnGroundPosition { get; private set; }
        public bool HasSpawned => spawnedCellIndex >= 0;

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
                FaceAdjacentWater(world, bestCellIndex, seaLevel);
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

            Debug.LogWarning("SurfaceSpawnCamera: no land cell found, using fallback.");
            ConfigureFallbackSpawn(world, settings);
            return true;
        }

        void ConfigureSpawn(PlanetWorld world, PlanetSettings settings, int cellIndex)
        {
            ref readonly SphereHexCell cell = ref world.Grid.GetCell(cellIndex);
            spawnedCellIndex = cellIndex;
            surfaceUp = math.normalize(cell.Normal);
            orbitReferenceRadius = world.GetCellSurfaceWorldRadius(cellIndex);
            SpawnGroundPosition = (Vector3)(surfaceUp * orbitReferenceRadius);
            eyeHeight = settings.PlayerEyeHeight;
            BuildSurfaceBasis(surfaceUp, out _, out _);
            yaw = 0f;
            pitch = lookPitchDown;
            orbitMode = false;
            ApplyTransform();
        }

        void ConfigureFallbackSpawn(PlanetWorld world, PlanetSettings settings)
        {
            spawnedCellIndex = 0;
            surfaceUp = new float3(0f, 1f, 0f);
            orbitReferenceRadius = world.ApproximateOuterRadius;
            SpawnGroundPosition = (Vector3)(surfaceUp * orbitReferenceRadius);
            eyeHeight = settings.PlayerEyeHeight;
            BuildSurfaceBasis(surfaceUp, out _, out _);
            yaw = 0f;
            pitch = lookPitchDown;
            orbitMode = false;
            ApplyTransform();
        }

        /// <summary>
        /// Planet root was shifted by -SpawnGroundPosition so the spawn tile sits at world origin.
        /// </summary>
        public void ApplyPlanetRecenter()
        {
            orbitMode = false;
            pitch = lookPitchDown;
            ApplyTransform();
        }

        void HandleModeToggle()
        {
#if ENABLE_INPUT_SYSTEM
            bool tabHeld = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
            orbitMode = tabHeld;
#else
            orbitMode = Input.GetKey(KeyCode.Tab);
#endif
        }

        void HandleInput()
        {
            if (orbitMode)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * orbitSpeed * Time.deltaTime;
                pitch += delta.y * orbitSpeed * Time.deltaTime;
            }

            float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                eyeHeight -= scroll * scrollSensitivity;
            }
#else
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                pitch += Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
            }

            eyeHeight -= Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
#endif

            pitch = Mathf.Clamp(pitch, 0f, 45f);
            eyeHeight = Mathf.Clamp(eyeHeight, minEyeHeight, maxEyeHeight);
        }

        void ApplyTransform()
        {
            if (orbitMode)
            {
                ApplyOrbitView();
                return;
            }

            ApplyPlayerView();
        }

        void ApplyPlayerView()
        {
            float3 up = math.normalize(surfaceUp);
            BuildSurfaceBasis(up, out float3 basisForward, out float3 basisRight);

            float yawRad = math.radians(yaw);
            float3 flatForward = math.normalize(basisForward * math.cos(yawRad) + basisRight * math.sin(yawRad));
            float pitchRad = math.radians(pitch);
            float3 lookForward = math.normalize(flatForward * math.cos(pitchRad) - up * math.sin(pitchRad));

            transform.position = (Vector3)(up * eyeHeight);
            transform.rotation = Quaternion.LookRotation((Vector3)lookForward, (Vector3)up);
        }

        void ApplyOrbitView()
        {
            float distance = orbitReferenceRadius + eyeHeight;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.position = offset;
            transform.rotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
        }

        static void BuildSurfaceBasis(float3 up, out float3 forward, out float3 right)
        {
            float3 worldUp = new float3(0f, 1f, 0f);
            float3 tangent = math.abs(math.dot(up, worldUp)) < 0.95f
                ? math.normalize(math.cross(worldUp, up))
                : math.normalize(math.cross(new float3(1f, 0f, 0f), up));
            right = math.normalize(math.cross(up, tangent));
            forward = math.normalize(math.cross(up, right));
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
            float3 up = math.normalize(surfaceUp);
            BuildSurfaceBasis(up, out float3 basisForward, out float3 basisRight);

            for (int i = 0; i < cell.NeighborCount; i++)
            {
                int neighborIndex = cell.Neighbors[i];
                BlockColumn neighborColumn = world.Columns.GetColumn(neighborIndex);
                if (neighborColumn.SurfaceHeight > seaLevel)
                {
                    continue;
                }

                ref readonly SphereHexCell neighbor = ref world.Grid.GetCell(neighborIndex);
                float3 toWater = neighbor.Normal - up * math.dot(neighbor.Normal, up);
                if (math.lengthsq(toWater) < 0.001f)
                {
                    continue;
                }

                toWater = math.normalize(toWater);
                yaw = math.degrees(math.atan2(math.dot(toWater, basisRight), math.dot(toWater, basisForward)));
                ApplyTransform();
                return;
            }
        }
    }
}

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
    /// First-person view on the surface (move mouse to look, scroll for eye height).
    /// Hold Tab for orbit debug: move mouse to rotate, scroll to zoom.
    /// Camera is parented to PlayerAnchor in planet-local space.
    /// </summary>
    public sealed class SurfaceSpawnCamera : MonoBehaviour
    {
        [SerializeField] bool spawnOnStart;
        [SerializeField] float lookPitchDown = 8f;
        [SerializeField] float lookSensitivity = 0.15f;
        [SerializeField] float orbitSpeed = 90f;
        [SerializeField] float scrollSensitivity = 0.15f;
        [SerializeField] bool requireMouseButtonForLook;
        [SerializeField] bool lockCursorWhileLooking = true;
        [SerializeField] float minPitch = -60f;
        [SerializeField] float maxPitch = 60f;
        [SerializeField] float minEyeHeight = 1.5f;
        [SerializeField] float maxEyeHeight = 3.5f;
        [SerializeField] float defaultOrbitDistance = 20f;
        [SerializeField] float minOrbitDistance = 5f;
        [SerializeField] float maxOrbitDistance = 400f;
        [SerializeField] float orbitScrollSensitivity = 2f;

        int spawnedCellIndex = -1;
        float eyeHeight;
        float orbitDistance;
        float yaw;
        float pitch;
        float3 surfaceUp;
        float orbitReferenceRadius;
        bool orbitMode;
        bool cursorLocked;

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
            HandleCursorLock();
            HandleInput();
            ApplyTransform();
        }

        void HandleCursorLock()
        {
            if (!lockCursorWhileLooking || orbitMode)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
                return;
            }

            if (!cursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetCursorLocked(false);
                return;
            }

            if (!cursorLocked && Input.GetMouseButtonDown(0))
            {
                SetCursorLocked(true);
            }
#endif
        }

        void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnEnable()
        {
            if (spawnedCellIndex >= 0 && lockCursorWhileLooking && !orbitMode)
            {
                SetCursorLocked(true);
            }
        }

        void OnDisable()
        {
            SetCursorLocked(false);
        }

        public bool TrySpawnOnSurface(bool allowDuringBuild = false)
        {
            PlanetBootstrap bootstrap = FindAnyObjectByType<PlanetBootstrap>();
            if (bootstrap == null || bootstrap.PlanetWorld == null)
            {
                return false;
            }

            if (!allowDuringBuild && !bootstrap.BuildComplete)
            {
                return false;
            }

            PlanetWorld world = bootstrap.PlanetWorld;
            int cellCount = math.min(world.Grid.CellCount, world.Columns.CellCount);
            if (cellCount <= 0)
            {
                Debug.LogWarning("SurfaceSpawnCamera: planet has no cells.");
                return false;
            }

            PlanetSettings settings = world.Settings;
            int seaLevel = settings.SeaLevelLayer;
            int bestCellIndex = -1;
            int bestScore = int.MinValue;

            for (int attempt = 0; attempt < cellCount; attempt++)
            {
                int cellIndex = (attempt * 7919) % cellCount;
                if (!TryScoreSpawnCell(world, cellIndex, seaLevel, requireSpawnPreference: true, out int score))
                {
                    continue;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCellIndex = cellIndex;
                }
            }

            if (bestCellIndex < 0)
            {
                for (int attempt = 0; attempt < cellCount; attempt++)
                {
                    int cellIndex = (attempt * 7919) % cellCount;
                    if (!TryScoreSpawnCell(world, cellIndex, seaLevel, requireSpawnPreference: false, out int score))
                    {
                        continue;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCellIndex = cellIndex;
                    }
                }
            }

            if (bestCellIndex >= 0)
            {
                ConfigureSpawn(world, settings, bestCellIndex, bootstrap.PlayerAnchor);
                FaceAdjacentWater(world, bestCellIndex, seaLevel, bootstrap.PlayerAnchor);
                return true;
            }

            Debug.LogWarning("SurfaceSpawnCamera: no land cell found, using fallback.");
            ConfigureFallbackSpawn(world, settings, bootstrap.PlayerAnchor);
            return true;
        }

        static bool TryScoreSpawnCell(
            PlanetWorld world,
            int cellIndex,
            int seaLevel,
            bool requireSpawnPreference,
            out int score)
        {
            score = int.MinValue;
            if (cellIndex < 0 || cellIndex >= world.Columns.CellCount)
            {
                return false;
            }

            BlockColumn column = world.Columns.GetColumn(cellIndex);
            int surfaceHeight = column.SurfaceHeight;
            if (surfaceHeight <= seaLevel)
            {
                return false;
            }

            BiomeDefinition biome = world.BiomeMap.GetBiome(cellIndex);
            int spawnPreference = biome != null ? biome.SpawnPreference : 0;
            if (requireSpawnPreference && spawnPreference <= 0)
            {
                return false;
            }

            int heightAboveSea = surfaceHeight - seaLevel;
            score = requireSpawnPreference ? spawnPreference * 100 : 50;
            score -= math.abs(heightAboveSea - 2) * (requireSpawnPreference ? 8 : 10);
            if (HasAdjacentWater(world, cellIndex, seaLevel))
            {
                score += requireSpawnPreference ? 120 : 80;
            }

            return true;
        }

        void ConfigureSpawn(PlanetWorld world, PlanetSettings settings, int cellIndex, PlayerAnchor anchor)
        {
            if (cellIndex < 0 || cellIndex >= world.Grid.CellCount)
            {
                ConfigureFallbackSpawn(world, settings, anchor);
                return;
            }

            ref readonly SphereHexCell cell = ref world.Grid.GetCell(cellIndex);
            spawnedCellIndex = cellIndex;
            surfaceUp = math.normalize(cell.Normal);
            orbitReferenceRadius = world.GetCellSurfaceWorldRadius(cellIndex);
            eyeHeight = settings.PlayerEyeHeight;
            orbitDistance = Mathf.Max(defaultOrbitDistance, eyeHeight + 8f);
            yaw = 0f;
            pitch = lookPitchDown;
            orbitMode = false;

            if (anchor != null)
            {
                anchor.Configure(cellIndex, surfaceUp, orbitReferenceRadius);
            }

            SetCursorLocked(lockCursorWhileLooking && !orbitMode);
            ApplyTransform();
        }

        void ConfigureFallbackSpawn(PlanetWorld world, PlanetSettings settings, PlayerAnchor anchor)
        {
            spawnedCellIndex = 0;
            surfaceUp = new float3(0f, 1f, 0f);
            orbitReferenceRadius = world.ApproximateOuterRadius;
            eyeHeight = settings.PlayerEyeHeight;
            orbitDistance = Mathf.Max(defaultOrbitDistance, eyeHeight + 8f);
            yaw = 0f;
            pitch = lookPitchDown;
            orbitMode = false;

            if (anchor != null)
            {
                anchor.Configure(0, surfaceUp, orbitReferenceRadius);
            }

            SetCursorLocked(lockCursorWhileLooking && !orbitMode);
            ApplyTransform();
        }

        void HandleModeToggle()
        {
            bool wasOrbitMode = orbitMode;
#if ENABLE_INPUT_SYSTEM
            bool tabHeld = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
            orbitMode = tabHeld;
#else
            orbitMode = Input.GetKey(KeyCode.Tab);
#endif

            if (wasOrbitMode && !orbitMode)
            {
                SetCursorLocked(lockCursorWhileLooking);
            }
            else if (!wasOrbitMode && orbitMode)
            {
                SetCursorLocked(false);
            }
        }

        void HandleInput()
        {
            if (orbitMode)
            {
                HandleOrbitInput();
                return;
            }

            HandlePlayerInput();
        }

        void HandlePlayerInput()
        {
            if (TryReadLookDelta(out Vector2 lookDelta))
            {
                ApplyLookDelta(lookDelta);
            }

            if (TryReadScrollDelta(out float scroll))
            {
                eyeHeight -= scroll * scrollSensitivity;
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            eyeHeight = Mathf.Clamp(eyeHeight, minEyeHeight, maxEyeHeight);
        }

        void HandleOrbitInput()
        {
            if (TryReadLookDelta(requireButton: false, out Vector2 lookDelta))
            {
                ApplyLookDelta(lookDelta);
            }

            if (TryReadScrollDelta(out float scroll))
            {
                orbitDistance -= scroll * orbitScrollSensitivity;
            }

            pitch = Mathf.Clamp(pitch, -15f, 85f);
            orbitDistance = Mathf.Clamp(orbitDistance, minOrbitDistance, maxOrbitDistance);
        }

        void ApplyLookDelta(Vector2 lookDelta)
        {
#if ENABLE_INPUT_SYSTEM
            float sensitivity = lookSensitivity;
#else
            float sensitivity = orbitSpeed;
#endif
            yaw += lookDelta.x * sensitivity;
            pitch -= lookDelta.y * sensitivity;
        }

        bool TryReadLookDelta(out Vector2 delta) => TryReadLookDelta(requireMouseButtonForLook, out delta);

        bool TryReadLookDelta(bool requireButton, out Vector2 delta)
        {
            delta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return false;
            }

            if (requireButton)
            {
                bool lookButtonHeld = Mouse.current.rightButton.isPressed || Mouse.current.leftButton.isPressed;
                if (!lookButtonHeld)
                {
                    return false;
                }
            }

            delta = Mouse.current.delta.ReadValue();
            return delta.sqrMagnitude > 0f;
#else
            if (requireButton && !Input.GetMouseButton(1) && !Input.GetMouseButton(0))
            {
                return false;
            }

            delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            return delta.sqrMagnitude > 0f;
#endif
        }

        bool TryReadScrollDelta(out float scroll)
        {
            scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                scroll = Mouse.current.scroll.ReadValue().y;
            }
#elif !ENABLE_INPUT_SYSTEM
            scroll = Input.GetAxis("Mouse ScrollWheel");
#endif
            return Mathf.Abs(scroll) > 0.001f;
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
            // PlayerAnchor already aligns local Y to the surface normal; stay in anchor space.
            transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        void ApplyOrbitView()
        {
            Vector3 pivot = new Vector3(0f, eyeHeight, 0f);
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = orbitRotation * new Vector3(0f, 0f, -orbitDistance);
            transform.localPosition = pivot + offset;

            Vector3 lookDirection = pivot - transform.localPosition;
            transform.localRotation = lookDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDirection, Vector3.up)
                : orbitRotation;
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
                int neighborIndex = cell.Neighbors[i];
                if (neighborIndex < 0 || neighborIndex >= world.Columns.CellCount)
                {
                    continue;
                }

                BlockColumn neighborColumn = world.Columns.GetColumn(neighborIndex);
                if (neighborColumn.SurfaceHeight <= seaLevel)
                {
                    return true;
                }
            }

            return false;
        }

        void FaceAdjacentWater(PlanetWorld world, int cellIndex, int seaLevel, PlayerAnchor anchor)
        {
            ref readonly SphereHexCell cell = ref world.Grid.GetCell(cellIndex);
            float3 up = math.normalize(surfaceUp);

            for (int i = 0; i < cell.NeighborCount; i++)
            {
                int neighborIndex = cell.Neighbors[i];
                if (neighborIndex < 0 || neighborIndex >= world.Columns.CellCount)
                {
                    continue;
                }

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
                if (anchor != null)
                {
                    Vector3 localToWater = anchor.transform.InverseTransformDirection((Vector3)toWater);
                    yaw = math.degrees(math.atan2(localToWater.x, localToWater.z));
                }
                else
                {
                    BuildSurfaceBasis(up, out float3 basisForward, out float3 basisRight);
                    yaw = math.degrees(math.atan2(math.dot(toWater, basisRight), math.dot(toWater, basisForward)));
                }

                ApplyTransform();
                return;
            }
        }
    }
}

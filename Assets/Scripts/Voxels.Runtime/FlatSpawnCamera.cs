using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class FlatSpawnCamera : MonoBehaviour
    {
        [SerializeField] float lookPitchDown = 8f;
        [SerializeField] float lookSensitivity = 0.15f;
        [SerializeField] bool lockCursorWhileLooking = true;
        [SerializeField] float minPitch = -60f;
        [SerializeField] float maxPitch = 60f;

        float yaw;
        float pitch;
        bool cursorLocked;
        HexCoord spawnHex = HexCoord.Zero;
        bool hasSpawned;
        WorldSettings spawnSettings;

        public bool HasSpawned => hasSpawned;
        public HexCoord SpawnHex => spawnHex;

        void LateUpdate()
        {
            if (!hasSpawned)
            {
                return;
            }

            HandleCursorLock();
            HandleLook();
            ApplyTransform();
        }

        public bool TrySpawn(HexWorld world, WorldSettings settings, Transform player)
        {
            if (world == null || settings == null || player == null)
            {
                return false;
            }

            spawnSettings = settings;
            spawnHex = FindSpawnHex(world, settings);
            float surfaceY = world.GetSurfaceWorldY(spawnHex);
            player.position = new Vector3(0f, surfaceY + settings.PlayerHeight * 0.5f, 0f);
            hasSpawned = true;
            transform.SetParent(player, false);
            yaw = 0f;
            pitch = lookPitchDown;
            ApplyTransform();
            return true;
        }

        static HexCoord FindSpawnHex(HexWorld world, WorldSettings settings)
        {
            int radius = math.min(12, settings.WorldHexRadius);
            var candidates = new List<(HexCoord hex, int score)>();

            for (int q = -radius; q <= radius; q++)
            {
                for (int r = -radius; r <= radius; r++)
                {
                    var hex = new HexCoord(q, r);
                    if (!world.IsInsideWorld(hex) || !world.Columns.TryGetColumn(hex, out BlockColumn column))
                    {
                        continue;
                    }

                    if (column.SurfaceHeight < settings.SeaLevelLayer)
                    {
                        continue;
                    }

                    BiomeDefinition biome = world.BiomeMap.GetBiome(hex);
                    int score = biome != null ? biome.SpawnPreference : 0;
                    candidates.Add((hex, score));
                }
            }

            if (candidates.Count == 0)
            {
                return HexCoord.Zero;
            }

            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            return candidates[0].hex;
        }

        void HandleLook()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return;
            }

            Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;
            yaw += delta.x;
            pitch -= delta.y;
#else
            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
#endif
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        void ApplyTransform()
        {
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            float eyeLocalY = spawnSettings.PlayerEyeHeight - spawnSettings.PlayerHeight * 0.5f;
            transform.localPosition = new Vector3(0f, eyeLocalY, 0f);
        }

        void HandleCursorLock()
        {
            if (!lockCursorWhileLooking)
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
            if (hasSpawned && lockCursorWhileLooking)
            {
                SetCursorLocked(true);
            }
        }

        void OnDisable()
        {
            SetCursorLocked(false);
        }
    }
}

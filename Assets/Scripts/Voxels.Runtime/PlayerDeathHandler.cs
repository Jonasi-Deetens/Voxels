using UnityEngine;
using Voxels.Core.Hex;

namespace Voxels.Runtime
{
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] float respawnHealthFraction = 0.5f;

        PlayerStatsController stats;
        FlatPlayerController movement;
        WorldScroller scroller;
        WorldSettings settings;
        HexWorld hexWorld;
        PlayerDeathOverlayView deathOverlay;
        bool subscribed;
        bool awaitingRespawn;

        public void Initialize(
            PlayerStatsController playerStats,
            FlatPlayerController playerMovement,
            WorldScroller worldScroller,
            WorldSettings worldSettings,
            HexWorld world,
            PlayerDeathOverlayView overlay = null)
        {
            stats = playerStats;
            movement = playerMovement;
            scroller = worldScroller;
            settings = worldSettings;
            hexWorld = world;
            deathOverlay = overlay;
            if (deathOverlay != null)
            {
                deathOverlay.RespawnRequested -= OnRespawnRequested;
                deathOverlay.RespawnRequested += OnRespawnRequested;
            }

            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            if (stats != null && subscribed)
            {
                stats.Died -= OnDied;
                subscribed = false;
            }

            if (deathOverlay != null)
            {
                deathOverlay.RespawnRequested -= OnRespawnRequested;
            }
        }

        void Subscribe()
        {
            if (stats == null || subscribed)
            {
                return;
            }

            stats.Died -= OnDied;
            stats.Died += OnDied;
            subscribed = true;
        }

        void OnDied()
        {
            if (stats == null || awaitingRespawn)
            {
                return;
            }

            awaitingRespawn = true;
            movement?.SetInputLocked(true);
            if (deathOverlay != null)
            {
                deathOverlay.Show();
            }
            else
            {
                PerformRespawn();
            }
        }

        void OnRespawnRequested() => PerformRespawn();

        void PerformRespawn()
        {
            if (stats == null || scroller == null || settings == null || hexWorld == null)
            {
                awaitingRespawn = false;
                return;
            }

            HexCoord spawnHex = FlatWorldSpawn.FindSpawnHex(hexWorld, settings);
            float y = hexWorld.GetSurfaceWorldY(spawnHex) + settings.PlayerHeight * 0.5f;
            var offset = Unity.Mathematics.FlatHexGrid.AxialToWorld(spawnHex, settings.BlockSize);
            scroller.SetWorldHex(spawnHex, offset);
            transform.position = new Vector3(0f, y, 0f);
            movement?.SnapToGround();
            stats.RespawnAfterDeath(respawnHealthFraction);
            movement?.SetInputLocked(false);
            deathOverlay?.Hide();
            awaitingRespawn = false;
        }
    }
}

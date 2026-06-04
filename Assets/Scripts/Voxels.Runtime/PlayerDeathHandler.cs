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
        bool subscribed;

        public void Initialize(
            PlayerStatsController playerStats,
            FlatPlayerController playerMovement,
            WorldScroller worldScroller,
            WorldSettings worldSettings,
            HexWorld world)
        {
            stats = playerStats;
            movement = playerMovement;
            scroller = worldScroller;
            settings = worldSettings;
            hexWorld = world;
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
        }

        void Subscribe()
        {
            if (stats == null || subscribed)
            {
                return;
            }

            stats.Died += OnDied;
            subscribed = true;
        }

        void OnDied()
        {
            if (stats == null || scroller == null || settings == null || hexWorld == null)
            {
                return;
            }

            HexCoord spawnHex = FlatWorldSpawn.FindSpawnHex(hexWorld, settings);
            float y = hexWorld.GetSurfaceWorldY(spawnHex) + settings.PlayerHeight * 0.5f;
            var offset = Unity.Mathematics.FlatHexGrid.AxialToWorld(spawnHex, settings.BlockSize);
            scroller.SetWorldHex(spawnHex, offset);
            transform.position = new Vector3(0f, y, 0f);
            movement?.SnapToGround();
            stats.RespawnAfterDeath(respawnHealthFraction);
            UnityEngine.Debug.Log("You died and respawned at the world spawn.");
        }
    }
}

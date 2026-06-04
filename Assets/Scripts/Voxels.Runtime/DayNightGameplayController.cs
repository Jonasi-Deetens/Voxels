using UnityEngine;
using Voxels.World;
using Voxels.World.Generation;

namespace Voxels.Runtime
{
    public sealed class DayNightGameplayController : MonoBehaviour
    {
        CelestialSystem celestial;
        NightCreatureSpawner creatureSpawner;
        WorldScroller scroller;

        public int LightLevel { get; private set; }
        public bool IsNight => LightLevel <= 7;

        public void Initialize(CelestialSystem celestialSystem, NightCreatureSpawner spawner, WorldScroller worldScroller = null)
        {
            celestial = celestialSystem;
            creatureSpawner = spawner;
            scroller = worldScroller;
        }

        void Update()
        {
            if (celestial == null)
            {
                return;
            }

            int sunLight = Mathf.RoundToInt(Mathf.Lerp(0f, 15f, celestial.SunHeight));
            if (scroller?.HexWorld != null)
            {
                LightLevel = BlockLightSampler.SamplePlayerLightLevel(scroller.HexWorld, scroller.PlayerWorldHex, sunLight);
            }
            else
            {
                LightLevel = sunLight;
            }

            if (IsNight)
            {
                creatureSpawner?.Tick(true, LightLevel);
            }
            else
            {
                creatureSpawner?.DespawnAll();
            }
        }
    }
}

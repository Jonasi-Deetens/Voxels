using UnityEngine;
using Voxels.World;

namespace Voxels.Runtime
{
  public sealed class DayNightGameplayController : MonoBehaviour
  {
    CelestialSystem celestial;
    NightCreatureSpawner creatureSpawner;

    public int LightLevel { get; private set; }
    public bool IsNight => LightLevel <= 7;

    public void Initialize(CelestialSystem celestialSystem, NightCreatureSpawner spawner)
    {
      celestial = celestialSystem;
      creatureSpawner = spawner;
    }

    void Update()
    {
      if (celestial == null)
      {
        return;
      }

      LightLevel = Mathf.RoundToInt(Mathf.Lerp(0f, 15f, celestial.SunHeight));
      creatureSpawner?.Tick(IsNight, LightLevel);
    }
  }
}

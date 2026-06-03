using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
  public sealed class NightCreatureSpawner : MonoBehaviour
  {
    [SerializeField] int maxCreatures = 12;
    [SerializeField] float spawnRadius = 24f;
    [SerializeField] float spawnInterval = 4f;

    WorldScroller scroller;
    WorldSettings settings;
    float spawnTimer;
    int activeCount;

    public void Initialize(WorldScroller worldScroller, WorldSettings worldSettings)
    {
      scroller = worldScroller;
      settings = worldSettings;
    }

    public void Tick(bool isNight, int lightLevel)
    {
      if (!isNight || scroller?.HexWorld == null)
      {
        return;
      }

      spawnTimer -= Time.deltaTime;
      if (spawnTimer > 0f || activeCount >= maxCreatures)
      {
        return;
      }

      BiomeDefinition biome = scroller.HexWorld.GetBiome(scroller.PlayerWorldHex);
      if (biome == null || lightLevel > biome.NightCreatureLightThreshold)
      {
        return;
      }

      spawnTimer = spawnInterval;
      TrySpawnCreature(biome);
    }

    void TrySpawnCreature(BiomeDefinition biome)
    {
      Vector2 offset = Random.insideUnitCircle * spawnRadius;
      var spawnPos = new Vector3(offset.x, 2f, offset.y);
      var creature = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      creature.name = $"NightCreature_{biome.name}";
      creature.transform.position = spawnPos;
      creature.transform.localScale = Vector3.one * 0.35f;

      var renderer = creature.GetComponent<Renderer>();
      if (renderer != null)
      {
        renderer.material.color = biome.NightAmbientColor * 1.4f;
      }

      var rb = creature.AddComponent<Rigidbody>();
      rb.useGravity = false;
      creature.AddComponent<NightCreatureLifetime>().Initialize(this, 18f);
      activeCount++;
    }

    public void NotifyDespawned() => activeCount = Mathf.Max(0, activeCount - 1);
  }

  sealed class NightCreatureLifetime : MonoBehaviour
  {
    NightCreatureSpawner spawner;
    float life;

    public void Initialize(NightCreatureSpawner owner, float seconds)
    {
      spawner = owner;
      life = seconds;
    }

    void Update()
    {
      life -= Time.deltaTime;
      if (life <= 0f)
      {
        spawner?.NotifyDespawned();
        Destroy(gameObject);
      }
    }
  }
}

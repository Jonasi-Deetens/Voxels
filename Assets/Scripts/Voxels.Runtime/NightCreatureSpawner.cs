using System.Collections.Generic;
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

    readonly List<GameObject> activeCreatures = new List<GameObject>();

    WorldScroller scroller;
    WorldSettings settings;
    float spawnTimer;

    public void Initialize(WorldScroller worldScroller, WorldSettings worldSettings)
    {
      scroller = worldScroller;
      settings = worldSettings;
    }

    public void Tick(bool isNight, int lightLevel)
    {
      if (!isNight)
      {
        return;
      }

      if (scroller?.HexWorld == null)
      {
        return;
      }

      spawnTimer -= Time.deltaTime;
      if (spawnTimer > 0f || activeCreatures.Count >= maxCreatures)
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

    public void DespawnAll()
    {
      for (int i = activeCreatures.Count - 1; i >= 0; i--)
      {
        if (activeCreatures[i] != null)
        {
          Destroy(activeCreatures[i]);
        }
      }

      activeCreatures.Clear();
    }

    void TrySpawnCreature(BiomeDefinition biome)
    {
      HexCoord playerHex = scroller.PlayerWorldHex;
      Vector2 offset = Random.insideUnitCircle * spawnRadius;
      float surfaceY = scroller.HexWorld.GetSurfaceWorldY(playerHex) + 1.2f;
      Transform worldRoot = scroller.WorldRoot;
      Vector3 local = new Vector3(offset.x, surfaceY, offset.y);
      Vector3 spawnPos = worldRoot != null ? worldRoot.TransformPoint(local) : local;

      var creature = GameObject.CreatePrimitive(PrimitiveType.Capsule);
      creature.name = $"NightCreature_{biome.name}";
      creature.transform.position = spawnPos;
      creature.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);

      var renderer = creature.GetComponent<Renderer>();
      if (renderer != null)
      {
        renderer.material.color = biome.NightAmbientColor * 1.4f;
      }

      var collider = creature.GetComponent<CapsuleCollider>();
      if (collider != null)
      {
        collider.isTrigger = true;
      }

      var rb = creature.AddComponent<Rigidbody>();
      rb.useGravity = false;
      creature.AddComponent<NightCreatureLifetime>().Initialize(this, 18f);
      activeCreatures.Add(creature);
    }

    internal void NotifyDespawned(GameObject creature)
    {
      activeCreatures.Remove(creature);
    }
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
        spawner?.NotifyDespawned(gameObject);
        Destroy(gameObject);
      }
    }
  }
}

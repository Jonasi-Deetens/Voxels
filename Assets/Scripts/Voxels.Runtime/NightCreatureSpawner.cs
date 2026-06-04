using System.Collections.Generic;
using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
  public sealed class NightCreatureSpawner : MonoBehaviour
  {
    [SerializeField] int maxCreatures = 10;
    [SerializeField] float spawnRadius = 22f;
    [SerializeField] float spawnInterval = 5f;
    [SerializeField] float contactDamage = 4f;

    readonly List<NightCreatureBehaviour> activeCreatures = new List<NightCreatureBehaviour>();

    WorldScroller scroller;
    WorldSettings settings;
    Transform playerTransform;
    float spawnTimer;

    public void Initialize(WorldScroller worldScroller, WorldSettings worldSettings, Transform player)
    {
      scroller = worldScroller;
      settings = worldSettings;
      playerTransform = player;
    }

    public void Tick(bool isNight, int lightLevel)
    {
      if (!isNight || scroller?.HexWorld == null)
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
          Destroy(activeCreatures[i].gameObject);
        }
      }

      activeCreatures.Clear();
    }

    void TrySpawnCreature(BiomeDefinition biome)
    {
      HexCoord playerHex = scroller.PlayerWorldHex;
      Vector2 offset = Random.insideUnitCircle * spawnRadius;
      float surfaceY = scroller.HexWorld.GetSurfaceWorldY(playerHex) + 1.1f;
      Transform worldRoot = scroller.WorldRoot;
      Vector3 local = new Vector3(offset.x, surfaceY, offset.y);
      Vector3 spawnPos = worldRoot != null ? worldRoot.TransformPoint(local) : local;

      var creature = new GameObject($"NightCreature_{biome.name}");
      creature.transform.position = spawnPos;
      BuildCreatureMesh(creature, biome);

      var collider = creature.AddComponent<CapsuleCollider>();
      collider.height = 1.2f;
      collider.radius = 0.35f;
      collider.isTrigger = true;

      var behaviour = creature.AddComponent<NightCreatureBehaviour>();
      behaviour.Initialize(this, playerTransform, contactDamage, 22f);
      activeCreatures.Add(behaviour);
    }

    static void BuildCreatureMesh(GameObject root, BiomeDefinition biome)
    {
      Color bodyColor = biome.NightAmbientColor * 1.6f;
      var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
      body.name = "Body";
      body.transform.SetParent(root.transform, false);
      body.transform.localScale = new Vector3(0.5f, 0.65f, 0.5f);
      Object.Destroy(body.GetComponent<Collider>());
      body.GetComponent<Renderer>().material.color = bodyColor;

      var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      head.name = "Head";
      head.transform.SetParent(root.transform, false);
      head.transform.localPosition = new Vector3(0f, 0.75f, 0f);
      head.transform.localScale = Vector3.one * 0.32f;
      Object.Destroy(head.GetComponent<Collider>());
      head.GetComponent<Renderer>().material.color = bodyColor * 0.85f;
    }

    internal void NotifyDespawned(NightCreatureBehaviour creature) => activeCreatures.Remove(creature);
  }

  public sealed class NightCreatureBehaviour : MonoBehaviour
  {
    NightCreatureSpawner spawner;
    Transform player;
    float damage;
    float speed;
    float life = 24f;

    public void Initialize(NightCreatureSpawner owner, Transform playerTarget, float contactDamage, float moveSpeed)
    {
      spawner = owner;
      player = playerTarget;
      damage = contactDamage;
      speed = moveSpeed;
    }

    void Update()
    {
      life -= Time.deltaTime;
      if (life <= 0f)
      {
        Despawn();
        return;
      }

      if (player == null)
      {
        return;
      }

      Vector3 toPlayer = player.position - transform.position;
      toPlayer.y = 0f;
      if (toPlayer.sqrMagnitude > 0.5f)
      {
        Vector3 step = toPlayer.normalized * (speed * Time.deltaTime);
        transform.position += step;
        transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
      }
    }

    void OnTriggerStay(Collider other)
    {
      if (player != null && other.transform == player)
      {
        PlayerStatsController stats = player.GetComponent<PlayerStatsController>();
        if (stats != null)
        {
            stats.ApplyDamage(damage * Time.deltaTime);
        }
        else
        {
            player.GetComponent<PlayerHealth>()?.TakeDamage(damage * Time.deltaTime);
        }
      }
    }

    void Despawn()
    {
      spawner?.NotifyDespawned(this);
      Destroy(gameObject);
    }
  }
}

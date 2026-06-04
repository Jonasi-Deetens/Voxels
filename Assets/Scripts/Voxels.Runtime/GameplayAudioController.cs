using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class GameplayAudioController : MonoBehaviour
    {
        [SerializeField] float footstepInterval = 0.42f;

        AudioSource sfxSource;
        AudioSource footstepSource;
        WorldScroller scroller;
        FlatPlayerController movement;
        float footstepTimer;
        readonly System.Collections.Generic.Dictionary<int, AudioClip> clipCache = new();
        BlockMaterialCategory lastFootCategory = BlockMaterialCategory.Other;

        public static GameplayAudioController Instance { get; private set; }

        public void Initialize(WorldScroller worldScroller, FlatPlayerController playerMovement)
        {
            scroller = worldScroller;
            movement = playerMovement;
            if (sfxSource == null)
            {
                sfxSource = CreateSource("Sfx", 0.55f);
                footstepSource = CreateSource("Footsteps", 0.35f);
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            if (movement == null || !movement.IsGrounded || movement.IsSwimming)
            {
                footstepTimer = 0f;
                return;
            }

            footstepTimer -= Time.deltaTime;
            if (footstepTimer > 0f)
            {
                return;
            }

            Vector3 velocity = movement.GetComponent<CharacterController>()?.velocity ?? Vector3.zero;
            velocity.y = 0f;
            if (velocity.sqrMagnitude < 0.8f)
            {
                return;
            }

            footstepTimer = footstepInterval;
            TryPlayFootstep(ResolveFootMaterial());
        }

        public void PlayBlockBreak(BlockDefinition definition, Vector3 worldPosition)
        {
            PlayBlockSfx(definition, worldPosition, 0.22f, 180f);
        }

        public void PlayBlockPlace(BlockDefinition definition, Vector3 worldPosition)
        {
            PlayBlockSfx(definition, worldPosition, 0.16f, 240f);
        }

        void PlayBlockSfx(BlockDefinition definition, Vector3 worldPosition, float volume, float baseHz)
        {
            if (sfxSource == null)
            {
                return;
            }

            BlockMaterialCategory category = definition != null
                ? definition.MaterialCategory
                : BlockMaterialCategory.Other;
            float pitch = CategoryPitch(category);
            sfxSource.transform.position = worldPosition;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(GetClip(Mathf.RoundToInt(baseHz * pitch), 0.08f), volume);
        }

        void TryPlayFootstep(BlockMaterialCategory category)
        {
            if (footstepSource == null)
            {
                return;
            }

            lastFootCategory = category;
            footstepSource.pitch = CategoryPitch(category);
            footstepSource.PlayOneShot(GetClip(90 + (int)category * 12, 0.06f), 0.28f);
        }

        BlockMaterialCategory ResolveFootMaterial()
        {
            if (scroller?.HexWorld == null)
            {
                return BlockMaterialCategory.Soil;
            }

            HexCoord hex = scroller.PlayerWorldHex;
            if (!scroller.HexWorld.TryGetColumn(hex, out BlockColumn column))
            {
                return BlockMaterialCategory.Soil;
            }

            BlockId surfaceId = column.GetBlock(column.SurfaceHeight);
            if (scroller.HexWorld.BlockRegistry.TryGetDefinition(surfaceId, out BlockDefinition definition))
            {
                return definition.MaterialCategory;
            }

            return BlockMaterialCategory.Soil;
        }

        static float CategoryPitch(BlockMaterialCategory category) => category switch
        {
            BlockMaterialCategory.Stone => 0.85f,
            BlockMaterialCategory.Soft => 1.1f,
            BlockMaterialCategory.Fluid => 1.25f,
            BlockMaterialCategory.Soil => 1f,
            _ => 0.95f,
        };

        AudioSource CreateSource(string name, float volume)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.loop = false;
            source.volume = volume;
            return source;
        }

        AudioClip GetClip(int key, float durationSeconds)
        {
            if (!clipCache.TryGetValue(key, out AudioClip clip))
            {
                clip = CreateNoiseClip(key, durationSeconds);
                clipCache[key] = clip;
            }

            return clip;
        }

        static AudioClip CreateNoiseClip(float frequency, float durationSeconds)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSeconds));
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)sampleCount);
                float noise = (Random.value * 2f - 1f) * 0.35f;
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.25f;
                samples[i] = (noise + tone) * envelope;
            }

            var clip = AudioClip.Create("sfx", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

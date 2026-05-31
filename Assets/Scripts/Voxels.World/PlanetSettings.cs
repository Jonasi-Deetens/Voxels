using UnityEngine;

namespace Voxels.World
{
    [CreateAssetMenu(menuName = "Voxels/Planet Settings", fileName = "Planet_")]
    public sealed class PlanetSettings : ScriptableObject
    {
        [SerializeField] int subdivisionLevel = 6;
        [SerializeField] int seed = 42;
        [SerializeField] BiomeDefinition biome;
        [SerializeField] int cellsPerChunk = 256;
        [Tooltip("Scales planet radius while keeping 1-block hex spacing (2 = double radius, adds one icosphere subdivision step).")]
        [SerializeField] float planetRadiusScale = 2f;

        [Header("Player Scale")]
        [SerializeField] float blockSize = 1f;
        [Tooltip("Eye height in block units while standing on the surface.")]
        [SerializeField] float playerEyeHeight = 1.7f;
        [Tooltip("Player height in block units.")]
        [SerializeField] float playerHeight = 2f;

        [Header("Geological Shell (radial block layers from base radius)")]
        [SerializeField] int coreLayerCount = 4;
        [SerializeField] int mantleLayerCount = 48;
        [SerializeField] int crustLayerCount = 28;
        [SerializeField] int seaLevelOffsetFromCrust = 6;

        public int SubdivisionLevel => subdivisionLevel;
        public float PlanetRadiusScale => planetRadiusScale;
        public int Seed => seed;
        public BiomeDefinition Biome => biome;
        public int CellsPerChunk => cellsPerChunk;
        public float BlockSize => blockSize;
        public float PlayerEyeHeight => playerEyeHeight;
        public float PlayerHeight => playerHeight;
        public int CoreLayerCount => coreLayerCount;
        public int MantleLayerCount => mantleLayerCount;
        public int CrustLayerCount => crustLayerCount;
        public int SeaLevelOffsetFromCrust => seaLevelOffsetFromCrust;

        public int CrustTopLayer => coreLayerCount + mantleLayerCount + crustLayerCount;

        public int SeaLevelLayer => CrustTopLayer + seaLevelOffsetFromCrust;

        /// <summary>
        /// Extra icosphere subdivision when radius scale doubles, so surface hexes stay ~1 block wide.
        /// </summary>
        public int ResolveSubdivisionLevel()
        {
            int extra = 0;
            float scale = planetRadiusScale;
            while (scale >= 2f)
            {
                extra++;
                scale *= 0.5f;
            }

            return subdivisionLevel + extra;
        }

        /// <summary>
        /// Inner shell radius so adjacent hex columns are spaced one <see cref="BlockSize"/> apart on the surface.
        /// </summary>
        public float ResolveShellRadius(float averageNeighborArc) => blockSize / averageNeighborArc;

        public float LayerToWorldRadius(float shellRadius, int layer) => shellRadius + layer * blockSize;

        public float ApproximateOuterRadius(float shellRadius, BiomeDefinition biomeDefinition)
        {
            float maxLayer = SeaLevelLayer + biomeDefinition.MountainAmplitude + biomeDefinition.DetailAmplitude + 8f;
            return LayerToWorldRadius(shellRadius, (int)maxLayer);
        }
    }
}

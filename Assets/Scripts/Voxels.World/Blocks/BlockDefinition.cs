using UnityEngine;
using Voxels.Core.Blocks;

namespace Voxels.World
{
    [CreateAssetMenu(menuName = "Voxels/Block Definition", fileName = "Block_")]
    public sealed class BlockDefinition : ScriptableObject
    {
        [SerializeField] ushort id = 1;
        [SerializeField] string displayName = "Block";
        [SerializeField] Material material;
        [SerializeField] bool isSolid = true;
        [SerializeField] bool isOpaque = true;
        [SerializeField] bool isFluid;
        [SerializeField] BlockMaterialCategory materialCategory = BlockMaterialCategory.Other;
        [SerializeField] float breakTime = 0.35f;

        public BlockId BlockId => new(id);
        public string DisplayName => displayName;
        public Material Material => material;
        public bool IsSolid => isSolid;
        public bool IsOpaque => isOpaque;
        public bool IsFluid => isFluid;
        public BlockMaterialCategory MaterialCategory => materialCategory;
        public float BreakTime => breakTime;
    }
}

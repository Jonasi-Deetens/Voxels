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

        public BlockId BlockId => new(id);
        public string DisplayName => displayName;
        public Material Material => material;
        public bool IsSolid => isSolid;
        public bool IsOpaque => isOpaque;
    }
}

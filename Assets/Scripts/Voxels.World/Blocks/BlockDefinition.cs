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

        [Header("Food (eat from hotbar with E)")]
        [SerializeField] float hungerRestore;
        [SerializeField] float healthRestoreOnEat;

        [Header("Light (reduces night spawns nearby)")]
        [SerializeField] bool isEmissive;
        [SerializeField, Range(0, 15)] int lightEmission;

        [Header("Held equipment (selected hotbar slot)")]
        [SerializeField] float heldDefensePercent;
        [SerializeField] float heldMiningMultiplier = 1f;

        public BlockId BlockId => new(id);
        public string DisplayName => displayName;
        public Material Material => material;
        public bool IsSolid => isSolid;
        public bool IsOpaque => isOpaque;
        public bool IsFluid => isFluid;
        public BlockMaterialCategory MaterialCategory => materialCategory;
        public float BreakTime => breakTime;
        public float HungerRestore => hungerRestore;
        public float HealthRestoreOnEat => healthRestoreOnEat;
        public float HeldDefensePercent => heldDefensePercent;
        public float HeldMiningMultiplier => heldMiningMultiplier;

        public bool IsEdible => hungerRestore > 0f;
        public bool IsEmissive => isEmissive;
        public int LightEmission => lightEmission;

        public bool HasHeldBonus => heldDefensePercent > 0f || heldMiningMultiplier > 1.01f;
    }
}

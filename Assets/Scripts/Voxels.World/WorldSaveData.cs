using System;
using System.Collections.Generic;

namespace Voxels.World
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public const int CurrentVersion = 4;

        public int version = CurrentVersion;
        public int seed;
        public int playerQ;
        public int playerR;
        public float playerY;
        public int hotbarIndex;
        public int activeTool;
        public int weatherKind;
        public int weatherTargetKind;
        public float weatherTransition;
        public float playerHealth = 20f;
        public float playerStamina = 100f;
        public float playerHunger = 100f;
        public float playerBreath = 100f;
        public List<SavedBiome> biomes = new();
        public List<SavedColumn> columns = new();
    }

    [Serializable]
    public sealed class SavedBiome
    {
        public int q;
        public int r;
        public string biomeName;
    }

    [Serializable]
    public sealed class SavedColumn
    {
        public int q;
        public int r;
        public int surfaceHeight;
        public List<SavedBlock> blocks = new();
    }

    [Serializable]
    public sealed class SavedBlock
    {
        public int layer;
        public ushort blockId;
    }
}

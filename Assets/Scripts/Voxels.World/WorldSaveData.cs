using System;
using System.Collections.Generic;

namespace Voxels.World
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public int seed;
        public List<SavedColumn> columns = new();
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

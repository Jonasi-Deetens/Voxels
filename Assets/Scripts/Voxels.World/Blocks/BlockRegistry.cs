using System.Collections.Generic;
using Voxels.Core.Blocks;

namespace Voxels.World
{
    public sealed class BlockRegistry
    {
        readonly BlockDefinition[] definitionsById = new BlockDefinition[ushort.MaxValue];
        readonly Dictionary<string, BlockDefinition> definitionsByName = new();

        public void Register(BlockDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            definitionsById[definition.BlockId.Value] = definition;
            definitionsByName[definition.DisplayName] = definition;
        }

        public void RegisterRange(IEnumerable<BlockDefinition> definitions)
        {
            foreach (BlockDefinition definition in definitions)
            {
                Register(definition);
            }
        }

        public BlockDefinition GetDefinition(BlockId blockId)
        {
            return definitionsById[blockId.Value];
        }

        public bool TryGetDefinition(BlockId blockId, out BlockDefinition definition)
        {
            definition = definitionsById[blockId.Value];
            return definition != null;
        }
    }
}

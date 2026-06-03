using System;
using System.IO;
using UnityEngine;

namespace Voxels.World.Modding
{
    [Serializable]
    public sealed class VoxelsModConfigData
    {
        public string[] hotbarBlockNames;
        public float structureDensity = -1f;
    }

    public static class VoxelsModConfig
    {
        const string ConfigFileName = "voxels_config.json";
        static VoxelsModConfigData cached;

        public static VoxelsModConfigData Load()
        {
            if (cached != null)
            {
                return cached;
            }

            string path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
            if (!File.Exists(path))
            {
                cached = new VoxelsModConfigData();
                return cached;
            }

            cached = JsonUtility.FromJson<VoxelsModConfigData>(File.ReadAllText(path)) ?? new VoxelsModConfigData();
            return cached;
        }

        public static float ResolveStructureDensity(WorldSettings settings)
        {
            float mod = Load().structureDensity;
            return mod >= 0f ? mod : settings.StructureDensity;
        }
    }
}

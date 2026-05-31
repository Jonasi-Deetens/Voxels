using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voxels.Rendering;
using Voxels.Runtime;
using Voxels.World;

namespace Voxels.EditorTools
{
    public static class VoxelsSetupMenu
    {
        const string DataRoot = "Assets/Data";
        const string MaterialsRoot = "Assets/Materials";

        [MenuItem("Voxels/Setup Default Content")]
        public static void SetupDefaultContent()
        {
            EnsureFolder("Assets/Data/Blocks");
            EnsureFolder("Assets/Data/Biomes");
            EnsureFolder(MaterialsRoot);

            Material grassMat = CreateMaterial("Block_Grass", new Color(0.28f, 0.62f, 0.24f));
            Material dirtMat = CreateMaterial("Block_Dirt", new Color(0.45f, 0.32f, 0.18f));
            Material stoneMat = CreateMaterial("Block_Stone", new Color(0.48f, 0.5f, 0.52f));
            Material coreMat = CreateMaterial("Block_Core", new Color(0.95f, 0.35f, 0.08f));
            Material mantleMat = CreateMaterial("Block_Mantle", new Color(0.32f, 0.18f, 0.14f));
            Material waterMat = CreateTransparentMaterial("Block_Water", new Color(0.15f, 0.45f, 0.82f, 0.55f));
            Material sandMat = CreateMaterial("Block_Sand", new Color(0.82f, 0.76f, 0.52f));

            BlockDefinition grass = CreateBlock("Assets/Data/Blocks/Block_Grass.asset", 1, "Grass", grassMat, true, true);
            BlockDefinition dirt = CreateBlock("Assets/Data/Blocks/Block_Dirt.asset", 2, "Dirt", dirtMat, true, true);
            BlockDefinition stone = CreateBlock("Assets/Data/Blocks/Block_Stone.asset", 3, "Stone", stoneMat, true, true);
            BlockDefinition core = CreateBlock("Assets/Data/Blocks/Block_Core.asset", 4, "Core", coreMat, true, true);
            BlockDefinition mantle = CreateBlock("Assets/Data/Blocks/Block_Mantle.asset", 5, "Mantle", mantleMat, true, true);
            BlockDefinition water = CreateBlock("Assets/Data/Blocks/Block_Water.asset", 6, "Water", waterMat, false, false);
            BlockDefinition sand = CreateBlock("Assets/Data/Blocks/Block_Sand.asset", 7, "Sand", sandMat, true, true);

            BiomeDefinition biome = CreateOrLoad<BiomeDefinition>("Assets/Data/Biomes/Biome_Grass.asset");
            SerializedObject biomeObject = new SerializedObject(biome);
            biomeObject.FindProperty("surfaceBlock").objectReferenceValue = grass;
            biomeObject.FindProperty("subsoilBlock").objectReferenceValue = dirt;
            biomeObject.FindProperty("underwaterSurfaceBlock").objectReferenceValue = sand;
            biomeObject.FindProperty("waterBlock").objectReferenceValue = water;
            biomeObject.FindProperty("coreBlock").objectReferenceValue = core;
            biomeObject.FindProperty("mantleBlock").objectReferenceValue = mantle;
            biomeObject.FindProperty("bedrockBlock").objectReferenceValue = stone;
            biomeObject.FindProperty("dirtDepth").intValue = 4;
            biomeObject.FindProperty("continentalFrequency").floatValue = 0.45f;
            biomeObject.FindProperty("continentalThreshold").floatValue = 0.35f;
            biomeObject.FindProperty("continentalBlendWidth").floatValue = 0.3f;
            biomeObject.FindProperty("oceanDepthMin").intValue = 6;
            biomeObject.FindProperty("oceanDepthMax").intValue = 14;
            biomeObject.FindProperty("terrainTypeFrequency").floatValue = 0.75f;
            biomeObject.FindProperty("plainsUpperThreshold").floatValue = -0.1f;
            biomeObject.FindProperty("hillsUpperThreshold").floatValue = 0.25f;
            biomeObject.FindProperty("plainsRoughness").floatValue = 2f;
            biomeObject.FindProperty("hillsAmplitude").floatValue = 6f;
            biomeObject.FindProperty("mountainAmplitude").floatValue = 18f;
            biomeObject.FindProperty("mountainFrequency").floatValue = 1.8f;
            biomeObject.FindProperty("detailAmplitude").floatValue = 4f;
            biomeObject.FindProperty("detailFrequency").floatValue = 5.5f;
            biomeObject.FindProperty("ridgeFrequency").floatValue = 3.2f;
            biomeObject.FindProperty("ridgeAmplitude").floatValue = 6f;
            biomeObject.FindProperty("caveFrequency").floatValue = 2.5f;
            biomeObject.FindProperty("caveThreshold").floatValue = 0.62f;
            biomeObject.FindProperty("caveMinLayerAboveCore").intValue = 6;
            biomeObject.FindProperty("caveMaxDepthBelowSurface").intValue = 5;
            biomeObject.ApplyModifiedPropertiesWithoutUndo();

            PlanetSettings planet = CreateOrLoad<PlanetSettings>("Assets/Data/Planet_Default.asset");
            SerializedObject planetObject = new SerializedObject(planet);
            planetObject.FindProperty("subdivisionLevel").intValue = 6;
            planetObject.FindProperty("seed").intValue = 42;
            planetObject.FindProperty("biome").objectReferenceValue = biome;
            planetObject.FindProperty("cellsPerChunk").intValue = 256;
            planetObject.FindProperty("planetRadiusScale").floatValue = 2f;
            planetObject.FindProperty("blockSize").floatValue = 1f;
            planetObject.FindProperty("playerEyeHeight").floatValue = 1.7f;
            planetObject.FindProperty("playerHeight").floatValue = 2f;
            planetObject.FindProperty("coreLayerCount").intValue = 8;
            planetObject.FindProperty("mantleLayerCount").intValue = 96;
            planetObject.FindProperty("crustLayerCount").intValue = 56;
            planetObject.FindProperty("seaLevelOffsetFromCrust").intValue = 12;
            planetObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Voxels default content created under Assets/Data and Assets/Materials.");
        }

        [MenuItem("Voxels/Setup Sample Scene")]
        public static void SetupSampleScene()
        {
            SetupDefaultContent();

            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SetupPlanetInScene(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Sample scene configured with Planet and OrbitCamera.");
        }

        static void SetupPlanetInScene(Scene scene)
        {
            PlanetBootstrap bootstrap = Object.FindFirstObjectByType<PlanetBootstrap>();
            GameObject planetObject;

            if (bootstrap == null)
            {
                planetObject = new GameObject("Planet");
                bootstrap = planetObject.AddComponent<PlanetBootstrap>();
            }
            else
            {
                planetObject = bootstrap.gameObject;
            }

            Transform chunkRoot = planetObject.transform.Find("Chunks");
            if (chunkRoot == null)
            {
                var chunkRootObject = new GameObject("Chunks");
                chunkRootObject.transform.SetParent(planetObject.transform, false);
                chunkRoot = chunkRootObject.transform;
            }

            PlanetSettings settings = AssetDatabase.LoadAssetAtPath<PlanetSettings>("Assets/Data/Planet_Default.asset");
            BlockDefinition grass = AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Grass.asset");
            BlockDefinition dirt = AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Dirt.asset");
            BlockDefinition stone = AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Stone.asset");
            BlockDefinition core = AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Core.asset");
            BlockDefinition mantle = AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Mantle.asset");
            BlockDefinition water = AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Water.asset");
            BlockDefinition sand = AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Sand.asset");

            SerializedObject bootstrapObject = new SerializedObject(bootstrap);
            bootstrapObject.FindProperty("settings").objectReferenceValue = settings;
            bootstrapObject.FindProperty("chunkRoot").objectReferenceValue = chunkRoot;
            bootstrapObject.FindProperty("blockDefinitions").arraySize = 7;
            bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = grass;
            bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = dirt;
            bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(2).objectReferenceValue = stone;
            bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(3).objectReferenceValue = core;
            bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(4).objectReferenceValue = mantle;
            bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(5).objectReferenceValue = water;
            bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(6).objectReferenceValue = sand;
            bootstrapObject.ApplyModifiedPropertiesWithoutUndo();

            Camera camera = Camera.main;
            if (camera != null)
            {
                OrbitCamera orbit = camera.GetComponent<OrbitCamera>();
                if (orbit != null)
                {
                    orbit.enabled = false;
                }

                SurfaceSpawnCamera surface = camera.GetComponent<SurfaceSpawnCamera>();
                if (surface == null)
                {
                    surface = camera.gameObject.AddComponent<SurfaceSpawnCamera>();
                }

                SerializedObject surfaceObject = new SerializedObject(surface);
                surfaceObject.FindProperty("spawnOnStart").boolValue = false;
                surfaceObject.FindProperty("lookPitchDown").floatValue = 8f;
                surfaceObject.FindProperty("minEyeHeight").floatValue = 1.5f;
                surfaceObject.FindProperty("maxEyeHeight").floatValue = 3.5f;
                surfaceObject.ApplyModifiedPropertiesWithoutUndo();
                surface.enabled = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsRoot}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material CreateTransparentMaterial(string name, Color color)
        {
            Material material = CreateMaterial(name, color);
            BlockMaterialUtility.ConfigureTransparent(material);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.9f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static BlockDefinition CreateBlock(
            string path,
            ushort id,
            string displayName,
            Material material,
            bool isSolid,
            bool isOpaque)
        {
            BlockDefinition block = CreateOrLoad<BlockDefinition>(path);
            SerializedObject blockObject = new SerializedObject(block);
            blockObject.FindProperty("id").intValue = id;
            blockObject.FindProperty("displayName").stringValue = displayName;
            blockObject.FindProperty("material").objectReferenceValue = material;
            blockObject.FindProperty("isSolid").boolValue = isSolid;
            blockObject.FindProperty("isOpaque").boolValue = isOpaque;
            blockObject.ApplyModifiedPropertiesWithoutUndo();
            return block;
        }

        static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}

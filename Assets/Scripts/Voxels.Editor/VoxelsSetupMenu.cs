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
        static void AssignBiomeAmbientLoops()
        {
            string[] biomePaths = Directory.GetFiles("Assets/Data/Biomes", "Biome_*.asset", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < biomePaths.Length; i++)
            {
                var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(biomePaths[i]);
                if (biome == null)
                {
                    continue;
                }

                string clipPath = $"Assets/Data/Audio/Ambient_{biome.name}.asset";
                EnsureFolder("Assets/Data/Audio");
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null)
                {
                    clip = CreateAmbientLoopClip($"Ambient_{biome.name}", 110f + i * 7f);
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                SerializedObject biomeObject = new SerializedObject(biome);
                biomeObject.FindProperty("ambientLoop").objectReferenceValue = clip;
                biomeObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static AudioClip CreateAmbientLoopClip(string clipName, float baseFrequency)
        {
            const int sampleRate = 22050;
            const float durationSeconds = 2f;
            int sampleCount = Mathf.RoundToInt(sampleRate * durationSeconds);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float tone = Mathf.Sin(2f * Mathf.PI * baseFrequency * t);
                float wobble = Mathf.Sin(2f * Mathf.PI * (baseFrequency * 0.5f) * t) * 0.35f;
                samples[i] = (tone + wobble) * 0.04f;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

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
            Material snowMat = CreateMaterial("Block_Snow", new Color(0.92f, 0.94f, 0.98f));
            Material iceMat = CreateTransparentMaterial("Block_Ice", new Color(0.7f, 0.88f, 0.95f, 0.65f));
            Material darkGrassMat = CreateMaterial("Block_DarkGrass", new Color(0.16f, 0.42f, 0.14f));
            Material yellowGrassMat = CreateMaterial("Block_YellowGrass", new Color(0.72f, 0.68f, 0.28f));
            Material gravelMat = CreateMaterial("Block_Gravel", new Color(0.58f, 0.58f, 0.6f));
            Material crystalMat = CreateMaterial("Block_Crystal", new Color(0.55f, 0.82f, 0.95f));
            Material fungusMat = CreateMaterial("Block_Fungus", new Color(0.62f, 0.28f, 0.72f));
            Material ashMat = CreateMaterial("Block_Ash", new Color(0.35f, 0.32f, 0.3f));

            BlockDefinition grass = CreateBlock("Assets/Data/Blocks/Block_Grass.asset", 1, "Grass", grassMat, true, true);
            BlockDefinition dirt = CreateBlock("Assets/Data/Blocks/Block_Dirt.asset", 2, "Dirt", dirtMat, true, true);
            BlockDefinition stone = CreateBlock("Assets/Data/Blocks/Block_Stone.asset", 3, "Stone", stoneMat, true, true);
            BlockDefinition core = CreateBlock("Assets/Data/Blocks/Block_Core.asset", 4, "Core", coreMat, true, true);
            BlockDefinition mantle = CreateBlock("Assets/Data/Blocks/Block_Mantle.asset", 5, "Mantle", mantleMat, true, true);
            BlockDefinition water = CreateBlock("Assets/Data/Blocks/Block_Water.asset", 6, "Water", waterMat, false, false);
            BlockDefinition sand = CreateBlock("Assets/Data/Blocks/Block_Sand.asset", 7, "Sand", sandMat, true, true);
            BlockDefinition snow = CreateBlock("Assets/Data/Blocks/Block_Snow.asset", 8, "Snow", snowMat, true, true);
            BlockDefinition ice = CreateBlock("Assets/Data/Blocks/Block_Ice.asset", 9, "Ice", iceMat, false, false);
            BlockDefinition darkGrass = CreateBlock("Assets/Data/Blocks/Block_DarkGrass.asset", 10, "Dark Grass", darkGrassMat, true, true);
            BlockDefinition yellowGrass = CreateBlock("Assets/Data/Blocks/Block_YellowGrass.asset", 11, "Yellow Grass", yellowGrassMat, true, true);
            BlockDefinition gravel = CreateBlock("Assets/Data/Blocks/Block_Gravel.asset", 12, "Gravel", gravelMat, true, true);
            BlockDefinition crystal = CreateBlock("Assets/Data/Blocks/Block_Crystal.asset", 13, "Crystal", crystalMat, true, true);
            BlockDefinition fungus = CreateBlock("Assets/Data/Blocks/Block_Fungus.asset", 14, "Fungus", fungusMat, true, true);
            BlockDefinition ash = CreateBlock("Assets/Data/Blocks/Block_Ash.asset", 15, "Ash", ashMat, true, true);

            BiomeDefinition grassland = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Grassland.asset",
                grass, dirt, sand, water, core, mantle, stone,
                spawnPreference: 100);
            MigrateLegacyGrassBiome("Assets/Data/Biomes/Biome_Grass.asset", grassland);

            BiomeDefinition forest = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Forest.asset",
                darkGrass, dirt, sand, water, core, mantle, stone,
                spawnPreference: 90);
            BiomeDefinition beach = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Beach.asset",
                sand, sand, sand, water, core, mantle, stone,
                spawnPreference: 80,
                mountainAmplitude: 8f,
                detailAmplitude: 2f);
            BiomeDefinition savanna = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Savanna.asset",
                yellowGrass, dirt, sand, water, core, mantle, stone);
            BiomeDefinition desert = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Desert.asset",
                sand, sand, sand, water, core, mantle, stone,
                mountainAmplitude: 10f,
                detailAmplitude: 2f,
                continentalThreshold: 0.32f);
            BiomeDefinition tundra = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Tundra.asset",
                snow, gravel, sand, water, core, mantle, stone);
            BiomeDefinition taiga = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Taiga.asset",
                snow, dirt, sand, water, core, mantle, stone);
            BiomeDefinition swamp = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Swamp.asset",
                darkGrass, dirt, sand, water, core, mantle, stone);
            BiomeDefinition alpine = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Alpine.asset",
                snow, stone, sand, water, core, mantle, stone,
                alpineElevationThreshold: 12,
                mountainAmplitude: 22f);
            BiomeDefinition ocean = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Ocean.asset",
                sand, sand, sand, water, core, mantle, stone,
                continentalThreshold: 0.5f);
            BiomeDefinition crystalWastes = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_CrystalWastes.asset",
                crystal, stone, sand, water, core, mantle, stone,
                mountainAmplitude: 14f);
            BiomeDefinition fungalBloom = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_FungalBloom.asset",
                fungus, dirt, sand, water, core, mantle, stone);
            BiomeDefinition ashlands = CreateTerrainBiome(
                "Assets/Data/Biomes/Biome_Ashlands.asset",
                ash, ash, sand, water, core, mantle, stone,
                mountainAmplitude: 12f);

            BiomeCatalog catalog = CreateOrLoad<BiomeCatalog>("Assets/Data/BiomeCatalog_Default.asset");
            SerializedObject catalogObject = new SerializedObject(catalog);
            catalogObject.FindProperty("terrainProfile").objectReferenceValue = grassland;
            catalogObject.FindProperty("fallbackBiome").objectReferenceValue = grassland;
            catalogObject.FindProperty("oceanBiome").objectReferenceValue = ocean;
            catalogObject.FindProperty("alpineBiome").objectReferenceValue = alpine;

            SerializedProperty rulesProperty = catalogObject.FindProperty("rules");
            rulesProperty.arraySize = 11;
            SetRule(rulesProperty, 0, beach, 25, -0.2f, 0.8f, 0.45f, 1f, 0f, 1f, 0, 40, 0, 2);
            SetRule(rulesProperty, 1, crystalWastes, 30, -1f, 0.15f, 0f, 1f, 0.78f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 2, fungalBloom, 28, -0.3f, 0.8f, 0.72f, 1f, 0.55f, 0.88f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 3, ashlands, 27, 0.35f, 1f, 0f, 0.32f, 0.72f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 4, swamp, 20, 0.15f, 1f, 0.78f, 1f, 0f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 5, desert, 18, 0.3f, 1f, 0f, 0.3f, 0f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 6, taiga, 16, -1f, 0.05f, 0.45f, 1f, 0f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 7, tundra, 14, -1f, 0.1f, 0f, 0.48f, 0f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 8, savanna, 12, 0.2f, 0.85f, 0.22f, 0.58f, 0f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 9, forest, 10, -0.2f, 0.55f, 0.52f, 1f, 0f, 1f, 0, 9999, 0, 9999);
            SetRule(rulesProperty, 10, grassland, 5, -0.25f, 0.5f, 0.28f, 0.68f, 0f, 1f, 0, 9999, 0, 9999);

            catalogObject.ApplyModifiedPropertiesWithoutUndo();

            WorldSettings world = CreateOrLoad<WorldSettings>("Assets/Data/World_Default.asset");
            SerializedObject worldObject = new SerializedObject(world);
            worldObject.FindProperty("seed").intValue = 42;
            worldObject.FindProperty("biomeCatalog").objectReferenceValue = catalog;
            worldObject.FindProperty("biome").objectReferenceValue = grassland;
            worldObject.FindProperty("worldHexRadius").intValue = 80;
            worldObject.FindProperty("blockSize").floatValue = 1f;
            worldObject.FindProperty("maxDepthBelowSurface").intValue = 80;
            worldObject.FindProperty("maxHeightAboveSurface").intValue = 30;
            worldObject.FindProperty("seaLevelLayer").intValue = 72;
            worldObject.FindProperty("chunkSizeHex").intValue = 16;
            worldObject.FindProperty("viewRadiusChunks").intValue = 3;
            worldObject.FindProperty("playerEyeHeight").floatValue = 1.7f;
            worldObject.FindProperty("playerHeight").floatValue = 2f;
            worldObject.FindProperty("dayLengthSeconds").floatValue = 900f;
            worldObject.FindProperty("orbitRadiusMultiplier").floatValue = 4f;
            worldObject.FindProperty("sunAngularSize").floatValue = 2.4f;
            worldObject.FindProperty("moonAngularSize").floatValue = 1.08f;
            worldObject.FindProperty("moonOrbitPhaseOffset").floatValue = 0.45f;
            worldObject.FindProperty("buildFrameBudgetMs").floatValue = 16f;
            worldObject.FindProperty("createTerrainColliders").boolValue = true;
            worldObject.FindProperty("floatingOriginRecenterDistance").floatValue = 1000f;
            worldObject.FindProperty("columnCacheMaxCells").intValue = 8192;
            worldObject.FindProperty("chunkMeshPadding").intValue = 1;
            worldObject.FindProperty("showWorldBoundary").boolValue = true;
            worldObject.FindProperty("boundaryWallHeight").floatValue = 96f;
            worldObject.FindProperty("useBackgroundMeshBuild").boolValue = true;
            worldObject.ApplyModifiedPropertiesWithoutUndo();

            EnsurePlayerStatsProfile();
            AssignBiomeAmbientLoops();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Voxels default content created: 13 biomes, flat world settings (hexRadius=80).");
        }

        [MenuItem("Voxels/Setup Sample Scene")]
        public static void SetupSampleScene()
        {
            SetupDefaultContent();

            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SetupWorldInScene(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Sample scene configured with flat hex world.");
        }

        static void SetupWorldInScene(Scene scene)
        {
            WorldBootstrap bootstrap = Object.FindFirstObjectByType<WorldBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = Object.FindFirstObjectByType<PlanetBootstrap>();
            }

            GameObject worldObject;
            if (bootstrap == null)
            {
                worldObject = new GameObject("World");
                bootstrap = worldObject.AddComponent<WorldBootstrap>();
            }
            else
            {
                worldObject = bootstrap.gameObject;
                worldObject.name = "World";
            }

            Transform chunkRoot = worldObject.transform.Find("Chunks");
            if (chunkRoot == null)
            {
                var chunkRootObject = new GameObject("Chunks");
                chunkRootObject.transform.SetParent(worldObject.transform, false);
                chunkRoot = chunkRootObject.transform;
            }

            WorldSettings settings = AssetDatabase.LoadAssetAtPath<WorldSettings>("Assets/Data/World_Default.asset");
            BlockDefinition[] blocks =
            {
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Grass.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Dirt.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Stone.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Core.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Mantle.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Water.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Sand.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Snow.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Ice.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_DarkGrass.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_YellowGrass.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Gravel.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Crystal.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Fungus.asset"),
                AssetDatabase.LoadAssetAtPath<BlockDefinition>("Assets/Data/Blocks/Block_Ash.asset"),
            };

            Light directionalLight = Object.FindFirstObjectByType<Light>();
            if (directionalLight == null || directionalLight.type != LightType.Directional)
            {
                var lightObject = new GameObject("Directional Light");
                directionalLight = lightObject.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            SerializedObject bootstrapObject = new SerializedObject(bootstrap);
            bootstrapObject.FindProperty("settings").objectReferenceValue = settings;
            bootstrapObject.FindProperty("chunkRoot").objectReferenceValue = chunkRoot;
            bootstrapObject.FindProperty("directionalLight").objectReferenceValue = directionalLight;
            bootstrapObject.FindProperty("blockDefinitions").arraySize = blocks.Length;
            for (int i = 0; i < blocks.Length; i++)
            {
                bootstrapObject.FindProperty("blockDefinitions").GetArrayElementAtIndex(i).objectReferenceValue = blocks[i];
            }

            SerializedProperty statsProfileProp = bootstrapObject.FindProperty("statsProfile");
            if (statsProfileProp != null)
            {
                statsProfileProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerStatsProfile>("Assets/Data/PlayerStats_Survival.asset");
            }

            bootstrapObject.ApplyModifiedPropertiesWithoutUndo();

            Camera camera = Camera.main;
            if (camera != null)
            {
                FlatSpawnCamera flatCamera = camera.GetComponent<FlatSpawnCamera>();
                if (flatCamera == null)
                {
                    flatCamera = camera.gameObject.AddComponent<FlatSpawnCamera>();
                }

                SerializedObject surfaceObject = new SerializedObject(flatCamera);
                surfaceObject.FindProperty("lookPitchDown").floatValue = 8f;
                surfaceObject.ApplyModifiedPropertiesWithoutUndo();
                flatCamera.enabled = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        static BiomeDefinition CreateTerrainBiome(
            string path,
            BlockDefinition surface,
            BlockDefinition subsoil,
            BlockDefinition underwater,
            BlockDefinition water,
            BlockDefinition core,
            BlockDefinition mantle,
            BlockDefinition bedrock,
            int spawnPreference = 0,
            int alpineElevationThreshold = 14,
            float mountainAmplitude = 18f,
            float detailAmplitude = 4f,
            float continentalThreshold = 0.35f)
        {
            BiomeDefinition biome = CreateOrLoad<BiomeDefinition>(path);
            SerializedObject biomeObject = new SerializedObject(biome);
            biomeObject.FindProperty("surfaceBlock").objectReferenceValue = surface;
            biomeObject.FindProperty("subsoilBlock").objectReferenceValue = subsoil;
            biomeObject.FindProperty("underwaterSurfaceBlock").objectReferenceValue = underwater;
            biomeObject.FindProperty("waterBlock").objectReferenceValue = water;
            biomeObject.FindProperty("coreBlock").objectReferenceValue = core;
            biomeObject.FindProperty("mantleBlock").objectReferenceValue = mantle;
            biomeObject.FindProperty("bedrockBlock").objectReferenceValue = bedrock;
            biomeObject.FindProperty("dirtDepth").intValue = 4;
            biomeObject.FindProperty("continentalFrequency").floatValue = 0.45f;
            biomeObject.FindProperty("continentalThreshold").floatValue = continentalThreshold;
            biomeObject.FindProperty("continentalBlendWidth").floatValue = 0.3f;
            biomeObject.FindProperty("oceanDepthMin").intValue = 6;
            biomeObject.FindProperty("oceanDepthMax").intValue = 14;
            biomeObject.FindProperty("terrainTypeFrequency").floatValue = 0.75f;
            biomeObject.FindProperty("plainsUpperThreshold").floatValue = -0.1f;
            biomeObject.FindProperty("hillsUpperThreshold").floatValue = 0.25f;
            biomeObject.FindProperty("plainsRoughness").floatValue = 2f;
            biomeObject.FindProperty("hillsAmplitude").floatValue = 6f;
            biomeObject.FindProperty("mountainAmplitude").floatValue = mountainAmplitude;
            biomeObject.FindProperty("mountainFrequency").floatValue = 1.8f;
            biomeObject.FindProperty("detailAmplitude").floatValue = detailAmplitude;
            biomeObject.FindProperty("detailFrequency").floatValue = 5.5f;
            biomeObject.FindProperty("ridgeFrequency").floatValue = 3.2f;
            biomeObject.FindProperty("ridgeAmplitude").floatValue = 6f;
            biomeObject.FindProperty("caveFrequency").floatValue = 2.5f;
            biomeObject.FindProperty("caveThreshold").floatValue = 0.62f;
            biomeObject.FindProperty("caveMinLayerAboveCore").intValue = 6;
            biomeObject.FindProperty("caveMaxDepthBelowSurface").intValue = 5;
            biomeObject.FindProperty("alpineElevationThreshold").intValue = alpineElevationThreshold;
            biomeObject.FindProperty("spawnPreference").intValue = spawnPreference;
            biomeObject.ApplyModifiedPropertiesWithoutUndo();
            return biome;
        }

        static void SetRule(
            SerializedProperty rulesProperty,
            int index,
            BiomeDefinition biome,
            int priority,
            float minTemp,
            float maxTemp,
            float minHumidity,
            float maxHumidity,
            float minLeyLine,
            float maxLeyLine,
            int minElevation,
            int maxElevation,
            int minCoastDistance,
            int maxCoastDistance)
        {
            SerializedProperty element = rulesProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("biome").objectReferenceValue = biome;
            element.FindPropertyRelative("priority").intValue = priority;
            element.FindPropertyRelative("minTemperature").floatValue = minTemp;
            element.FindPropertyRelative("maxTemperature").floatValue = maxTemp;
            element.FindPropertyRelative("minHumidity").floatValue = minHumidity;
            element.FindPropertyRelative("maxHumidity").floatValue = maxHumidity;
            element.FindPropertyRelative("minLeyLine").floatValue = minLeyLine;
            element.FindPropertyRelative("maxLeyLine").floatValue = maxLeyLine;
            element.FindPropertyRelative("minElevationAboveSea").intValue = minElevation;
            element.FindPropertyRelative("maxElevationAboveSea").intValue = maxElevation;
            element.FindPropertyRelative("minCoastDistance").intValue = minCoastDistance;
            element.FindPropertyRelative("maxCoastDistance").intValue = maxCoastDistance;
            element.FindPropertyRelative("requiresLand").boolValue = true;
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
            ApplyBlockGameplay(blockObject, displayName);
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

        static void ApplyBlockGameplay(SerializedObject blockObject, string displayName)
        {
            bool isWater = displayName == "Water";
            blockObject.FindProperty("isFluid").boolValue = isWater;

            BlockMaterialCategory category = BlockMaterialCategory.Other;
            float breakTime = 0.35f;
            if (isWater)
            {
                category = BlockMaterialCategory.Fluid;
                breakTime = 0.2f;
            }
            else if (displayName == "Stone" || displayName == "Core" || displayName == "Mantle" ||
                     displayName == "Crystal" || displayName == "Gravel")
            {
                category = BlockMaterialCategory.Stone;
                breakTime = 0.55f;
            }
            else if (displayName == "Dirt" || displayName == "Sand" || displayName == "Ash")
            {
                category = BlockMaterialCategory.Soil;
                breakTime = 0.3f;
            }
            else if (displayName.Contains("Grass") || displayName == "Snow" || displayName == "Fungus")
            {
                category = BlockMaterialCategory.Soft;
                breakTime = 0.25f;
            }

            blockObject.FindProperty("materialCategory").enumValueIndex = (int)category;
            blockObject.FindProperty("breakTime").floatValue = breakTime;
            ApplyBlockSurvival(blockObject, displayName);
        }

        static void ApplyBlockSurvival(SerializedObject blockObject, string displayName)
        {
            float hunger = 0f;
            float health = 0f;
            float defense = 0f;
            float mining = 1f;

            switch (displayName)
            {
                case "Fungus":
                    hunger = 35f;
                    health = 5f;
                    break;
                case "Yellow Grass":
                    hunger = 20f;
                    health = 3f;
                    break;
                case "Grass":
                case "Dark Grass":
                    hunger = 15f;
                    health = 2f;
                    break;
                case "Crystal":
                    defense = 0.12f;
                    mining = 1.15f;
                    break;
                case "Ash":
                    defense = 0.08f;
                    break;
            }

            blockObject.FindProperty("hungerRestore").floatValue = hunger;
            blockObject.FindProperty("healthRestoreOnEat").floatValue = health;
            blockObject.FindProperty("heldDefensePercent").floatValue = defense;
            blockObject.FindProperty("heldMiningMultiplier").floatValue = mining;
        }

        static void EnsurePlayerStatsProfile()
        {
            EnsureFolder("Assets/Data");
            const string path = "Assets/Data/PlayerStats_Survival.asset";
            var profile = CreateOrLoad<PlayerStatsProfile>(path);
            SerializedObject profileObject = new SerializedObject(profile);
            profileObject.FindProperty("maxHealth").floatValue = 20f;
            profileObject.FindProperty("maxStamina").floatValue = 100f;
            profileObject.FindProperty("maxHunger").floatValue = 100f;
            profileObject.FindProperty("maxBreath").floatValue = 100f;
            profileObject.FindProperty("fungusHungerRestore").floatValue = 35f;
            profileObject.FindProperty("yellowGrassHungerRestore").floatValue = 20f;
            profileObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
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

        static void MigrateLegacyGrassBiome(string legacyPath, BiomeDefinition grassland)
        {
            BiomeDefinition legacy = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(legacyPath);
            if (legacy != null && legacy != grassland)
            {
                AssetDatabase.DeleteAsset(legacyPath);
            }
        }
    }
}

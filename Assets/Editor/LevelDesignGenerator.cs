using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelDesignGenerator
{
    private const string GeneratedRoot = "Assets/Generated";
    private const string PrefabRoot = GeneratedRoot + "/Prefabs";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string SceneRoot = "Assets/Scenes";
    private const string FurnitureModels =
        "Assets/ThirdParty/Kenney/FurnitureKit/Models/FBX format/";

    private static readonly Color DirtColor = new Color(0.46f, 0.29f, 0.16f);
    private static readonly Color GrassColor = new Color(0.20f, 0.47f, 0.16f);
    private static readonly Color WaterColor = new Color(0.12f, 0.43f, 0.68f);
    private static readonly Color RockColor = new Color(0.30f, 0.32f, 0.34f);

    [MenuItem("Eco das Patas/Gerar quatro fases")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder(GeneratedRoot, "Prefabs");
        EnsureFolder(GeneratedRoot, "Materials");

        Material dirtMaterial = CreateMaterial("Terra", DirtColor, 0.15f);
        Material grassMaterial = CreateMaterial("Mato", GrassColor, 0.05f);
        Material waterMaterial = CreateMaterial("Agua", WaterColor, 0.65f);
        Material rockMaterial = CreateMaterial("Pedra", RockColor, 0.25f);
        Material apartmentFloorMaterial = CreateMaterial(
            "Piso_Apartamento",
            new Color(0.64f, 0.43f, 0.25f),
            0.32f);

        GameObject dirtPrefab = CreateTerrainPrefab("Terreno_Terra", TerrainType.Dirt, dirtMaterial);
        GameObject grassPrefab = CreateTerrainPrefab("Terreno_Mato", TerrainType.Grass, grassMaterial);
        GameObject waterPrefab = CreateTerrainPrefab("Terreno_Agua", TerrainType.Water, waterMaterial);
        GameObject rockPrefab = CreateRockPrefab(rockMaterial);
        GameObject apartmentFloorPrefab = CreateTerrainPrefab(
            "Terreno_Piso_Apartamento",
            TerrainType.Dirt,
            apartmentFloorMaterial);

        GenerateScene(
            0,
            "Apartamento",
            "Fase0_Apartamento",
            apartmentFloorPrefab,
            grassPrefab,
            waterPrefab,
            rockPrefab,
            ApartmentTerrain,
            new Vector2Int[0],
            new Vector2Int(1, 1),
            new Vector2Int(8, 2));
        DecorateApartmentScene();

        GenerateScene(
            1,
            "Arredores",
            "Fase1_Arredores",
            dirtPrefab,
            grassPrefab,
            waterPrefab,
            rockPrefab,
            PhaseOneTerrain,
            new[]
            {
                new Vector2Int(2, 2), new Vector2Int(2, 3),
                new Vector2Int(7, 6), new Vector2Int(8, 6)
            },
            new Vector2Int(1, 1),
            new Vector2Int(8, 8));

        GenerateScene(
            2,
            "Bosque Fechado",
            "Fase2_Bosque",
            dirtPrefab,
            grassPrefab,
            waterPrefab,
            rockPrefab,
            PhaseTwoTerrain,
            new[]
            {
                new Vector2Int(1, 6), new Vector2Int(2, 6),
                new Vector2Int(6, 2), new Vector2Int(6, 3),
                new Vector2Int(8, 7)
            },
            new Vector2Int(1, 1),
            new Vector2Int(8, 8));

        GenerateScene(
            3,
            "Pântano",
            "Fase3_Pantano",
            dirtPrefab,
            grassPrefab,
            waterPrefab,
            rockPrefab,
            PhaseThreeTerrain,
            new[]
            {
                new Vector2Int(2, 4), new Vector2Int(3, 4),
                new Vector2Int(5, 6), new Vector2Int(6, 6),
                new Vector2Int(7, 2), new Vector2Int(8, 2)
            },
            new Vector2Int(0, 0),
            new Vector2Int(9, 9));

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SceneRoot + "/Fase0_Apartamento.unity", true),
            new EditorBuildSettingsScene(SceneRoot + "/Fase1_Arredores.unity", true),
            new EditorBuildSettingsScene(SceneRoot + "/Fase2_Bosque.unity", true),
            new EditorBuildSettingsScene(SceneRoot + "/Fase3_Pantano.unity", true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/Kenney/GraveyardKit/Models/FBX format/rocks.fbx") != null)
        {
            KenneyAssetIntegrator.GenerateAndApply();
        }

        Debug.Log("Level design gerado: apartamento e três fases externas.");
    }

    private static void DecorateApartmentScene()
    {
        const string scenePath = SceneRoot + "/Fase0_Apartamento.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        if (grid == null) return;

        GameObject oldApartment = GameObject.Find("ApartamentoKenney");
        if (oldApartment != null) Object.DestroyImmediate(oldApartment);

        GameObject apartment = new GameObject("ApartamentoKenney");
        Transform architecture = CreateGroup("Arquitetura", apartment.transform);
        Transform livingRoom = CreateGroup("Sala", apartment.transform);
        Transform bedroom = CreateGroup("Quarto", apartment.transform);
        Transform kitchen = CreateGroup("Cozinha", apartment.transform);
        Transform diningRoom = CreateGroup("Jantar", apartment.transform);
        Transform decoration = CreateGroup("Decoracao", apartment.transform);

        CreateApartmentWalls(scene, grid, architecture);

        PlaceFurniture(scene, grid, livingRoom, "loungeSofaLong.fbx", "Sofa",
            new Vector2Int(3, 2), 0f, 10f, 6f, true);
        PlaceFurniture(scene, grid, livingRoom, "tableCoffee.fbx", "MesaDeCentro",
            new Vector2Int(3, 3), 0f, 5.5f, 4f, true);
        PlaceFurniture(scene, grid, livingRoom, "televisionModern.fbx", "Televisao",
            new Vector2Int(3, 5), 180f, 5.5f, 6f, true);
        PlaceFurniture(scene, grid, livingRoom, "loungeChair.fbx", "Poltrona",
            new Vector2Int(1, 4), 90f, 5.5f, 6f, true);
        PlaceFurniture(scene, grid, livingRoom, "loungeChair.fbx", "PoltronaDireita",
            new Vector2Int(5, 4), 270f, 5.5f, 6f, true);
        PlaceFurniture(scene, grid, decoration, "rugRectangle.fbx", "TapeteSala",
            new Vector2Int(3, 4), 0f, 22f, 1f, false, Vector3.up * 0.08f);

        PlaceFurniture(scene, grid, bedroom, "bedDouble.fbx", "Cama",
            new Vector2Int(1, 8), 90f, 8.5f, 6f, true);
        PlaceFurniture(scene, grid, bedroom, "sideTableDrawers.fbx", "CriadoMudo",
            new Vector2Int(3, 8), 0f, 4.5f, 5f, true);
        PlaceFurniture(scene, grid, bedroom, "bookcaseOpen.fbx", "Estante",
            new Vector2Int(4, 8), 180f, 6.5f, 8f, true);
        PlaceFurniture(scene, grid, decoration, "lampRoundTable.fbx", "Abajur",
            new Vector2Int(3, 8), 0f, 2.5f, 4f, false, new Vector3(0f, 2.8f, 0f));
        PlaceFurniture(scene, grid, decoration, "rugSquare.fbx", "TapeteQuarto",
            new Vector2Int(2, 7), 0f, 15f, 1f, false, Vector3.up * 0.08f);

        PlaceFurniture(scene, grid, kitchen, "kitchenSink.fbx", "Pia",
            new Vector2Int(6, 8), 180f, 7.5f, 6f, true);
        PlaceFurniture(scene, grid, kitchen, "kitchenStove.fbx", "Fogao",
            new Vector2Int(7, 8), 180f, 7f, 6f, true);
        PlaceFurniture(scene, grid, kitchen, "kitchenFridge.fbx", "Geladeira",
            new Vector2Int(8, 8), 180f, 7f, 9f, true);
        PlaceFurniture(scene, grid, kitchen, "kitchenCabinet.fbx", "Balcao",
            new Vector2Int(8, 7), 90f, 7f, 6f, true);

        PlaceFurniture(scene, grid, diningRoom, "tableRound.fbx", "MesaDeJantar",
            new Vector2Int(7, 5), 0f, 7f, 6f, true);
        PlaceFurniture(scene, grid, diningRoom, "chairCushion.fbx", "CadeiraOeste",
            new Vector2Int(6, 5), 90f, 4.5f, 6f, true);
        PlaceFurniture(scene, grid, diningRoom, "chairCushion.fbx", "CadeiraSul",
            new Vector2Int(7, 4), 0f, 4.5f, 6f, true);
        PlaceFurniture(scene, grid, diningRoom, "chairCushion.fbx", "CadeiraLeste",
            new Vector2Int(8, 5), 270f, 4.5f, 6f, true);
        PlaceFurniture(scene, grid, decoration, "rugRound.fbx", "TapeteJantar",
            new Vector2Int(7, 5), 0f, 13f, 1f, false, Vector3.up * 0.08f);

        PlaceFurniture(scene, grid, decoration, "pottedPlant.fbx", "PlantaSala",
            new Vector2Int(1, 6), 0f, 4f, 6f, false);
        PlaceFurniture(scene, grid, decoration, "coatRackStanding.fbx", "Cabideiro",
            new Vector2Int(8, 1), 0f, 4f, 7f, false);
        PlaceFurniture(scene, grid, decoration, "radio.fbx", "Radio",
            new Vector2Int(3, 5), 0f, 2.5f, 3f, false, new Vector3(0f, 3f, 0f));

        CreateWarmLight(apartment.transform, "LuzSala", CellToWorld(grid, new Vector2Int(3, 4)));
        CreateWarmLight(apartment.transform, "LuzCozinha", CellToWorld(grid, new Vector2Int(7, 7)));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateApartmentWalls(Scene scene, GridManager grid, Transform parent)
    {
        float northZ = grid.origin.z + grid.height * grid.cellSize + 0.2f;
        float eastX = grid.origin.x + grid.width * grid.cellSize + 0.2f;

        for (int x = 0; x < grid.width; x++)
        {
            string model = x == 2 || x == 7
                ? "wallWindow.fbx"
                : x == 5 ? "wallDoorwayWide.fbx" : "wall.fbx";
            Vector3 position = new Vector3(
                grid.origin.x + (x + 0.5f) * grid.cellSize,
                grid.origin.y,
                northZ);
            PlaceApartmentWall(scene, model, $"ParedeNorte_{x}", parent, position, 0f);
        }

        for (int y = 0; y < grid.height; y++)
        {
            string model = y == 2
                ? "wallDoorwayWide.fbx"
                : y == 6 ? "wallWindow.fbx" : "wall.fbx";
            Vector3 position = new Vector3(
                eastX,
                grid.origin.y,
                grid.origin.z + (y + 0.5f) * grid.cellSize);
            PlaceApartmentWall(scene, model, $"ParedeLeste_{y}", parent, position, 90f);
        }

        // Divisória entre o quarto e a cozinha, com passagem central.
        float partitionX = grid.origin.x + 5f * grid.cellSize;
        for (int y = 6; y <= 8; y++)
        {
            string model = y == 6 ? "wallDoorway.fbx" : "wall.fbx";
            Vector3 position = new Vector3(
                partitionX,
                grid.origin.y,
                grid.origin.z + (y + 0.5f) * grid.cellSize);
            PlaceApartmentWall(scene, model, $"DivisoriaQuarto_{y}", parent, position, 90f);
        }
    }

    private static void PlaceApartmentWall(
        Scene scene,
        string modelName,
        string objectName,
        Transform parent,
        Vector3 position,
        float yaw)
    {
        PlaceKitModel(
            scene,
            FurnitureModels + modelName,
            objectName,
            parent,
            position,
            yaw,
            10.1f,
            7.2f,
            false);
    }

    private static void PlaceFurniture(
        Scene scene,
        GridManager grid,
        Transform parent,
        string modelName,
        string objectName,
        Vector2Int cell,
        float yaw,
        float targetWidth,
        float targetHeight,
        bool blocksCell,
        Vector3 offset = default)
    {
        GameObject furniture = PlaceKitModel(
            scene,
            FurnitureModels + modelName,
            objectName,
            parent,
            CellToWorld(grid, cell) + offset,
            yaw,
            targetWidth,
            targetHeight);
        if (furniture == null || !blocksCell) return;

        Obstacle obstacle = furniture.AddComponent<Obstacle>();
        obstacle.gridPosition = cell;
        EditorUtility.SetDirty(obstacle);
    }

    private static GameObject PlaceKitModel(
        Scene scene,
        string modelPath,
        string objectName,
        Transform parent,
        Vector3 groundPosition,
        float yaw,
        float targetWidth,
        float targetHeight,
        bool preserveAspect = true)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
        {
            Debug.LogWarning($"Modelo residencial não encontrado: {modelPath}");
            return null;
        }

        GameObject instance = new GameObject(objectName);
        instance.transform.SetParent(parent, false);
        instance.transform.position = groundPosition;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        visual.name = "Visual";
        visual.transform.SetParent(instance.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return instance;

        Bounds bounds = GetBounds(renderers);
        float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
        float horizontalScale = targetWidth / Mathf.Max(horizontalSize, 0.001f);
        float verticalScale = targetHeight / Mathf.Max(bounds.size.y, 0.001f);
        Vector3 originalScale = visual.transform.localScale;
        if (preserveAspect)
        {
            float uniformScale = Mathf.Min(horizontalScale, verticalScale);
            visual.transform.localScale = originalScale * uniformScale;
        }
        else
        {
            visual.transform.localScale = new Vector3(
                originalScale.x * horizontalScale,
                originalScale.y * verticalScale,
                originalScale.z * horizontalScale);
        }

        bounds = GetBounds(renderers);
        visual.transform.position += new Vector3(
            groundPosition.x - bounds.center.x,
            groundPosition.y - bounds.min.y,
            groundPosition.z - bounds.center.z);
        return instance;
    }

    private static Bounds GetBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static void CreateWarmLight(Transform parent, string name, Vector3 floorPosition)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = floorPosition + Vector3.up * 12f;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.78f, 0.55f);
        light.intensity = 300f;
        light.range = 25f;
    }

    private static void GenerateScene(
        int phaseNumber,
        string phaseName,
        string sceneFileName,
        GameObject dirtPrefab,
        GameObject grassPrefab,
        GameObject waterPrefab,
        GameObject rockPrefab,
        System.Func<int, int, TerrainType> terrainSelector,
        IReadOnlyCollection<Vector2Int> rockPositions,
        Vector2Int playerStart,
        Vector2Int enemyStart)
    {
        Scene scene = EditorSceneManager.OpenScene(SceneRoot + "/SampleScene.unity", OpenSceneMode.Single);

        foreach (Obstacle oldObstacle in Object.FindObjectsByType<Obstacle>(
                     FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(oldObstacle.gameObject);
        }

        GameObject oldEnvironment = GameObject.Find("CenarioGerado");
        if (oldEnvironment != null)
        {
            Object.DestroyImmediate(oldEnvironment);
        }

        foreach (LevelSceneInfo oldInfo in Object.FindObjectsByType<LevelSceneInfo>(
                     FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(oldInfo.gameObject);
        }

        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        GameObject environment = new GameObject("CenarioGerado");
        GameObject terrainRoot = new GameObject("Terrenos");
        terrainRoot.transform.SetParent(environment.transform);
        GameObject obstacleRoot = new GameObject("Obstaculos");
        obstacleRoot.transform.SetParent(environment.transform);

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                TerrainType type = terrainSelector(x, y);
                GameObject prefab = type == TerrainType.Grass
                    ? grassPrefab
                    : type == TerrainType.Water ? waterPrefab : dirtPrefab;

                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                tile.name = $"{type}_{x}_{y}";
                tile.transform.SetParent(terrainRoot.transform);
                tile.transform.position = CellToWorld(grid, new Vector2Int(x, y)) + Vector3.down * 0.035f;
                tile.transform.localScale = new Vector3(
                    grid.cellSize * 0.96f,
                    0.08f,
                    grid.cellSize * 0.96f);

                TerrainTile terrainTile = tile.GetComponent<TerrainTile>();
                terrainTile.gridPosition = new Vector2Int(x, y);
                terrainTile.terrainType = type;
                EditorUtility.SetDirty(terrainTile);
            }
        }

        foreach (Vector2Int position in rockPositions)
        {
            GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, scene);
            rock.name = $"Rochas_{position.x}_{position.y}";
            rock.transform.SetParent(obstacleRoot.transform);
            rock.transform.position = CellToWorld(grid, position);

            Obstacle obstacle = rock.GetComponent<Obstacle>();
            obstacle.gridPosition = position;
            EditorUtility.SetDirty(obstacle);
        }

        Unit[] units = Object.FindObjectsByType<Unit>(FindObjectsInactive.Include);
        Unit player = units.FirstOrDefault(unit => !(unit is Enemy));
        Enemy enemy = units.OfType<Enemy>().FirstOrDefault();
        SetUnitStart(player, playerStart, grid);
        SetUnitStart(enemy, enemyStart, grid);

        GameObject infoObject = new GameObject($"Fase {phaseNumber} - {phaseName}");
        LevelSceneInfo info = infoObject.AddComponent<LevelSceneInfo>();
        info.phaseNumber = phaseNumber;
        info.phaseName = phaseName;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SceneRoot + "/" + sceneFileName + ".unity");
    }

    private static void SetUnitStart(Unit unit, Vector2Int start, GridManager grid)
    {
        if (unit == null) return;
        unit.startPosition = start;
        unit.transform.position = CellToWorld(grid, start);
        EditorUtility.SetDirty(unit);
    }

    private static Vector3 CellToWorld(GridManager grid, Vector2Int position)
    {
        return new Vector3(
            grid.origin.x + position.x * grid.cellSize + grid.cellSize * 0.5f,
            grid.origin.y,
            grid.origin.z + position.y * grid.cellSize + grid.cellSize * 0.5f);
    }

    private static TerrainType ApartmentTerrain(int x, int y)
    {
        return TerrainType.Dirt;
    }

    private static TerrainType PhaseOneTerrain(int x, int y)
    {
        if (x == 4 && y >= 1 && y <= 8) return TerrainType.Water;
        if ((x <= 2 && y >= 6) || (x >= 7 && y <= 3)) return TerrainType.Grass;
        return TerrainType.Dirt;
    }

    private static TerrainType PhaseTwoTerrain(int x, int y)
    {
        if (y == 4 && x >= 1 && x <= 8) return TerrainType.Water;
        if ((x + y) % 4 == 0 || (x >= 3 && x <= 5 && y >= 6)) return TerrainType.Grass;
        return TerrainType.Dirt;
    }

    private static TerrainType PhaseThreeTerrain(int x, int y)
    {
        if ((x >= 2 && x <= 3) || (x >= 6 && x <= 7))
        {
            return y >= 1 && y <= 8 ? TerrainType.Water : TerrainType.Dirt;
        }

        if ((x + y) % 3 == 0 || (y >= 4 && y <= 6)) return TerrainType.Grass;
        return TerrainType.Dirt;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateTerrainPrefab(
        string name,
        TerrainType type,
        Material material)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = name;
        tile.transform.localScale = new Vector3(1f, 0.08f, 1f);
        Object.DestroyImmediate(tile.GetComponent<Collider>());
        tile.GetComponent<MeshRenderer>().sharedMaterial = material;
        TerrainTile terrainTile = tile.AddComponent<TerrainTile>();
        terrainTile.terrainType = type;

        string path = PrefabRoot + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tile, path);
        Object.DestroyImmediate(tile);
        return prefab;
    }

    private static GameObject CreateRockPrefab(Material material)
    {
        GameObject root = new GameObject("Rochas_Blocos");
        root.AddComponent<Obstacle>();

        AddRockBlock(root.transform, material, new Vector3(-0.18f, 0.20f, 0.05f),
            new Vector3(0.55f, 0.40f, 0.48f), new Vector3(0f, 18f, 4f));
        AddRockBlock(root.transform, material, new Vector3(0.20f, 0.16f, -0.10f),
            new Vector3(0.48f, 0.32f, 0.58f), new Vector3(7f, -14f, 0f));
        AddRockBlock(root.transform, material, new Vector3(0.06f, 0.38f, 0.10f),
            new Vector3(0.36f, 0.34f, 0.38f), new Vector3(-5f, 8f, 10f));

        string path = PrefabRoot + "/Rochas_Blocos.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AddRockBlock(
        Transform parent,
        Material material,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 eulerAngles)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "BlocoDePedra";
        block.transform.SetParent(parent);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;
        block.transform.localEulerAngles = eulerAngles;
        block.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}

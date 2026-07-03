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

    private static readonly Color DirtColor = new Color(0.46f, 0.29f, 0.16f);
    private static readonly Color GrassColor = new Color(0.20f, 0.47f, 0.16f);
    private static readonly Color WaterColor = new Color(0.12f, 0.43f, 0.68f);
    private static readonly Color RockColor = new Color(0.30f, 0.32f, 0.34f);

    [MenuItem("Eco das Patas/Gerar três fases")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder(GeneratedRoot, "Prefabs");
        EnsureFolder(GeneratedRoot, "Materials");

        Material dirtMaterial = CreateMaterial("Terra", DirtColor, 0.15f);
        Material grassMaterial = CreateMaterial("Mato", GrassColor, 0.05f);
        Material waterMaterial = CreateMaterial("Agua", WaterColor, 0.65f);
        Material rockMaterial = CreateMaterial("Pedra", RockColor, 0.25f);

        GameObject dirtPrefab = CreateTerrainPrefab("Terreno_Terra", TerrainType.Dirt, dirtMaterial);
        GameObject grassPrefab = CreateTerrainPrefab("Terreno_Mato", TerrainType.Grass, grassMaterial);
        GameObject waterPrefab = CreateTerrainPrefab("Terreno_Agua", TerrainType.Water, waterMaterial);
        GameObject rockPrefab = CreateRockPrefab(rockMaterial);

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

        Debug.Log("Level design gerado: três fases, terrenos e prefab de rochas.");
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

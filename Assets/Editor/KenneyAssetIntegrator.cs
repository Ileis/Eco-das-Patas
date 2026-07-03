using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KenneyAssetIntegrator
{
    private const float CellScale = 10f;
    private const float CatLocalScale = 0.5f;
    private const float CatLocalVerticalOffset = -0.45f;
    private const float ZombieLocalScale = 0.5f;
    private const float ZombieLocalVerticalOffset = 0.2f;
    private const string NatureModels =
        "Assets/ThirdParty/Kenney/NatureKit/Models/FBX format/";
    private const string GraveyardModels =
        "Assets/ThirdParty/Kenney/GraveyardKit/Models/FBX format/";
    private const string CatRoot = "Assets/Cartoon Cat/fbx/";
    private const string GeneratedRoot = "Assets/Generated/Kenney";
    private const string PrefabRoot = GeneratedRoot + "/Prefabs";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string RockPrefabPath = "Assets/Generated/Prefabs/Rochas_Blocos.prefab";

    private static readonly Dictionary<Material, Material> ConvertedMaterials =
        new Dictionary<Material, Material>();

    [MenuItem("Eco das Patas/Integrar assets Kenney")]
    public static void GenerateAndApply()
    {
        EnsureFolder("Assets/Generated", "Kenney");
        EnsureFolder(GeneratedRoot, "Prefabs");
        EnsureFolder(GeneratedRoot, "Materials");
        ConvertedMaterials.Clear();

        GameObject rockPrefab = CreateObstaclePrefab(
            GraveyardModels + "rocks.fbx",
            RockPrefabPath,
            0.66f * CellScale,
            0.48f * CellScale);
        GameObject grassPrefab = CreateVisualPrefab(
            NatureModels + "grass_large.fbx",
            PrefabRoot + "/Mato_Decorativo.prefab",
            0.52f * CellScale,
            0.28f * CellScale);
        GameObject treePrefab = CreateVisualPrefab(
            NatureModels + "tree_default.fbx",
            PrefabRoot + "/Arvore.prefab",
            0.72f * CellScale,
            1.85f * CellScale);
        GameObject gravePrefab = CreateVisualPrefab(
            GraveyardModels + "gravestone-bevel.fbx",
            PrefabRoot + "/Lapide.prefab",
            0.48f * CellScale,
            0.75f * CellScale);
        ConfigureZombieModelImporter();
        RuntimeAnimatorController zombieController = CreateZombieController();
        Avatar zombieAvatar = AssetDatabase
            .LoadAllAssetsAtPath(GraveyardModels + "character-zombie.fbx")
            .OfType<Avatar>()
            .FirstOrDefault();
        GameObject zombiePrefab = CreateVisualPrefab(
            GraveyardModels + "character-zombie.fbx",
            PrefabRoot + "/Zumbi_Visual.prefab",
            0.48f * CellScale,
            1.05f * CellScale,
            zombieController,
            -0.04f * CellScale,
            zombieAvatar);
        Avatar catAvatar = ConfigureCatModelImporters();
        RuntimeAnimatorController catController = CreateCatIdleController();
        GameObject catPrefab = CreateVisualPrefab(
            CatRoot + "cat_Idle.fbx",
            PrefabRoot + "/Gato_Visual.prefab",
            0.52f * CellScale,
            0.62f * CellScale,
            catController,
            -0.04f * CellScale,
            catAvatar);
        Texture2D catTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CatRoot + "Texture.png");
        ApplyTexture(catPrefab, catTexture);

        if (rockPrefab == null || grassPrefab == null || treePrefab == null
            || gravePrefab == null || zombiePrefab == null || catPrefab == null)
        {
            throw new FileNotFoundException(
                "Um ou mais modelos Kenney não foram importados. Aguarde a importação dos FBX e tente novamente.");
        }

        const string apartmentScene = "Assets/Scenes/Fase0_Apartamento.unity";
        if (File.Exists(apartmentScene))
        {
            ApplyToScene(
                apartmentScene,
                treePrefab,
                grassPrefab,
                gravePrefab,
                zombiePrefab,
                catPrefab,
                new Vector3[0],
                new Vector3[0],
                new Vector3[0]);
        }

        ApplyToScene(
            "Assets/Scenes/Fase1_Arredores.unity",
            treePrefab,
            grassPrefab,
            gravePrefab,
            zombiePrefab,
            catPrefab,
            new[]
            {
                new Vector3(0f, 0f, 8f),
                new Vector3(9f, 0f, 2f)
            },
            new[]
            {
                new Vector3(1f, 0f, 7f),
                new Vector3(8f, 0f, 1f),
                new Vector3(7f, 0f, 2f)
            },
            new Vector3[0]);

        ApplyToScene(
            "Assets/Scenes/Fase2_Bosque.unity",
            treePrefab,
            grassPrefab,
            gravePrefab,
            zombiePrefab,
            catPrefab,
            new[]
            {
                new Vector3(0f, 0f, 8f),
                new Vector3(9f, 0f, 1f),
                new Vector3(4f, 0f, 8f)
            },
            new[]
            {
                new Vector3(0f, 0f, 4f),
                new Vector3(4f, 0f, 0f),
                new Vector3(9f, 0f, 3f)
            },
            new Vector3[0]);

        ApplyToScene(
            "Assets/Scenes/Fase3_Pantano.unity",
            treePrefab,
            grassPrefab,
            gravePrefab,
            zombiePrefab,
            catPrefab,
            new[]
            {
                new Vector3(0f, 0f, 8f),
                new Vector3(9f, 0f, 1f)
            },
            new[]
            {
                new Vector3(0f, 0f, 6f),
                new Vector3(4f, 0f, 5f),
                new Vector3(8f, 0f, 4f)
            },
            new[]
            {
                new Vector3(1f, 0f, 8f),
                new Vector3(4f, 0f, 8f),
                new Vector3(8f, 0f, 8f)
            });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Assets Kenney integrados ao apartamento e às três fases externas.");
    }

    private static GameObject CreateObstaclePrefab(
        string sourcePath,
        string destinationPath,
        float targetWidth,
        float targetHeight)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (model == null) return null;

        GameObject root = new GameObject("Rochas_Kenney");
        root.AddComponent<Obstacle>();
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        PrepareVisual(visual, targetWidth, targetHeight, "Rochas");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, destinationPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateVisualPrefab(
        string sourcePath,
        string destinationPath,
        float targetWidth,
        float targetHeight,
        RuntimeAnimatorController animatorController = null,
        float groundInset = 0f,
        Avatar animatorAvatar = null)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (model == null) return null;

        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(destinationPath));
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        PrepareVisual(visual, targetWidth, targetHeight, root.name);
        visual.transform.localPosition += Vector3.up * groundInset;
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
            if (animatorAvatar != null) animator.avatar = animatorAvatar;
            animator.applyRootMotion = false;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, destinationPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void PrepareVisual(
        GameObject visual,
        float targetWidth,
        float targetHeight,
        string materialPrefix)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = GetBounds(renderers);
        float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
        float scale = Mathf.Min(
            targetWidth / Mathf.Max(horizontalSize, 0.001f),
            targetHeight / Mathf.Max(bounds.size.y, 0.001f));
        visual.transform.localScale *= scale;

        bounds = GetBounds(renderers);
        visual.transform.position += new Vector3(
            -bounds.center.x,
            -bounds.min.y,
            -bounds.center.z);

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = ConvertMaterial(materials[i], materialPrefix, i);
            }
            renderer.sharedMaterials = materials;
        }
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

    private static void ApplyTexture(GameObject prefab, Texture texture)
    {
        if (prefab == null || texture == null) return;

        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                material.SetTexture("_BaseMap", texture);
                material.SetTexture("_MainTex", texture);
                EditorUtility.SetDirty(material);
            }
        }
    }

    private static RuntimeAnimatorController CreateCatIdleController()
    {
        const string controllerPath = GeneratedRoot + "/Gato_Idle.controller";
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            stateMachine.RemoveState(childState.state);
        }

        (string StateName, string FileName, bool ReturnsToIdle)[] animations =
        {
            ("Idle", "cat_Idle.fbx", false),
            ("Walk", "cat_Walk.fbx", false),
            ("Jump", "cat_Jump.fbx", true),
            ("Eat", "cat_Eat.fbx", true),
            ("sound", "cat_Sound.fbx", true)
        };

        AnimatorState idleState = null;
        List<(AnimatorState State, bool ReturnsToIdle)> states =
            new List<(AnimatorState State, bool ReturnsToIdle)>();

        foreach ((string stateName, string fileName, bool returnsToIdle) in animations)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(CatRoot + fileName)
                .OfType<AnimationClip>()
                .FirstOrDefault(animation => !animation.name.StartsWith("__preview__"));
            if (clip == null)
            {
                throw new FileNotFoundException(
                    $"O clipe {stateName} do gato não foi importado de {fileName}.");
            }

            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            states.Add((state, returnsToIdle));

            if (stateName == "Idle")
            {
                idleState = state;
                stateMachine.defaultState = state;
            }
        }

        foreach ((AnimatorState state, bool returnsToIdle) in states)
        {
            if (!returnsToIdle) continue;

            AnimatorStateTransition returnToIdle = state.AddTransition(idleState);
            returnToIdle.hasExitTime = true;
            returnToIdle.exitTime = 0.95f;
            returnToIdle.duration = 0.1f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;
    }

    private static Avatar ConfigureCatModelImporters()
    {
        ConfigureCatModelImporter("cat_Idle.fbx", "Idle", true, null);
        Avatar idleAvatar = AssetDatabase.LoadAllAssetsAtPath(CatRoot + "cat_Idle.fbx")
            .OfType<Avatar>()
            .FirstOrDefault();

        ConfigureCatModelImporter("cat_Walk.fbx", "Walk", true, idleAvatar);
        ConfigureCatModelImporter("cat_Jump.fbx", "Jump", false, idleAvatar);
        ConfigureCatModelImporter("cat_Eat.fbx", "Eat", false, idleAvatar);
        ConfigureCatModelImporter("cat_Sound.fbx", "sound", false, idleAvatar);
        return idleAvatar;
    }

    private static void ConfigureCatModelImporter(
        string fileName,
        string clipName,
        bool shouldLoop,
        Avatar sourceAvatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(CatRoot + fileName) as ModelImporter;
        if (importer == null) return;

        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = sourceAvatar == null
            ? ModelImporterAvatarSetup.CreateFromThisModel
            : ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = sourceAvatar;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips.Length == 0) clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.name = clipName;
            clip.loopTime = shouldLoop;
            clip.loopPose = shouldLoop;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static RuntimeAnimatorController CreateZombieController()
    {
        const string controllerPath = GeneratedRoot + "/Zumbi.controller";
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            stateMachine.RemoveState(childState.state);
        }

        AnimationClip[] clips = AssetDatabase
            .LoadAllAssetsAtPath(GraveyardModels + "character-zombie.fbx")
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .OrderBy(clip => clip.name)
            .ToArray();

        if (clips.Length == 0)
        {
            throw new FileNotFoundException(
                "Nenhum clipe de animação foi importado de character-zombie.fbx.");
        }

        AnimationClip idleClip = clips.FirstOrDefault(
            clip => clip.name.Equals("idle", System.StringComparison.OrdinalIgnoreCase));
        AnimatorState idleState = null;
        if (idleClip != null)
        {
            idleState = stateMachine.AddState("Idle");
            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;
        }

        foreach (AnimationClip clip in clips)
        {
            if (clip == idleClip) continue;

            string stateName = GetZombieStateName(clip.name);
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;

            if (idleState == null)
            {
                idleState = state;
                stateMachine.defaultState = state;
            }

            if (stateName.StartsWith("attack-", System.StringComparison.OrdinalIgnoreCase))
            {
                AnimatorStateTransition returnToIdle = state.AddTransition(idleState);
                returnToIdle.hasExitTime = true;
                returnToIdle.exitTime = 0.95f;
                returnToIdle.duration = 0.1f;
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;
    }

    private static void ConfigureZombieModelImporter()
    {
        string modelPath = GraveyardModels + "character-zombie.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null) return;

        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            bool shouldLoop =
                clip.name.Equals("idle", System.StringComparison.OrdinalIgnoreCase)
                || clip.name.Equals("walk", System.StringComparison.OrdinalIgnoreCase);
            clip.loopTime = shouldLoop;
            clip.loopPose = shouldLoop;
        }

        // Persist all takes explicitly instead of depending on Unity's implicit
        // default import, which previously left clipAnimations empty in the meta.
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static string GetZombieStateName(string clipName)
    {
        if (clipName.Equals("idle", System.StringComparison.OrdinalIgnoreCase)) return "Idle";
        if (clipName.Equals("walk", System.StringComparison.OrdinalIgnoreCase)) return "Walk";
        if (clipName.Equals("jump", System.StringComparison.OrdinalIgnoreCase)) return "Jump";
        if (clipName.Equals("die", System.StringComparison.OrdinalIgnoreCase)) return "die";
        return clipName;
    }

    private static Material ConvertMaterial(Material source, string prefix, int index)
    {
        if (source != null && ConvertedMaterials.TryGetValue(source, out Material cached))
        {
            return cached;
        }

        string sourceName = source != null ? source.name : "SemMaterial";
        string safeName = MakeSafeFileName($"{prefix}_{sourceName}_{index}");
        string path = MaterialRoot + "/" + safeName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        Color color = source != null && source.HasProperty("_Color")
            ? source.color
            : Color.white;
        Texture texture = source != null && source.HasProperty("_MainTex")
            ? source.mainTexture
            : null;

        material.SetColor("_BaseColor", color);
        if (texture != null) material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Smoothness", 0.12f);
        EditorUtility.SetDirty(material);

        if (source != null) ConvertedMaterials[source] = material;
        return material;
    }

    private static void ApplyToScene(
        string scenePath,
        GameObject treePrefab,
        GameObject grassPrefab,
        GameObject gravePrefab,
        GameObject zombiePrefab,
        GameObject catPrefab,
        IReadOnlyList<Vector3> treePositions,
        IReadOnlyList<Vector3> grassPositions,
        IReadOnlyList<Vector3> gravePositions)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject oldDecor = GameObject.Find("DecoracaoKenney");
        if (oldDecor != null) Object.DestroyImmediate(oldDecor);

        GameObject decor = new GameObject("DecoracaoKenney");
        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        AddDecorations(scene, decor.transform, treePrefab, treePositions, "Arvore", grid, true);
        AddDecorations(scene, decor.transform, grassPrefab, grassPositions, "Mato", grid, false);
        AddDecorations(scene, decor.transform, gravePrefab, gravePositions, "Lapide", grid, true);

        Enemy enemy = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include)
            .FirstOrDefault();
        if (enemy != null)
        {
            Transform oldVisual = enemy.transform.Find("VisualKenney");
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

            MeshRenderer placeholder = enemy.GetComponent<MeshRenderer>();
            if (placeholder != null) placeholder.enabled = false;

            GameObject zombie = (GameObject)PrefabUtility.InstantiatePrefab(zombiePrefab, scene);
            zombie.name = "VisualKenney";
            AttachZombieVisual(zombie.transform, enemy.transform);
        }

        Unit player = Object.FindObjectsByType<Unit>(FindObjectsInactive.Include)
            .FirstOrDefault(unit => !(unit is Enemy));
        if (player != null)
        {
            Transform oldVisual = player.transform.Find("VisualGato");
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

            MeshRenderer placeholder = player.GetComponent<MeshRenderer>();
            if (placeholder != null) placeholder.enabled = false;

            GameObject cat = (GameObject)PrefabUtility.InstantiatePrefab(catPrefab, scene);
            cat.name = "VisualGato";
            AttachCatVisual(cat.transform, player.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AttachZombieVisual(Transform zombie, Transform enemy)
    {
        zombie.SetParent(enemy, false);
        zombie.localPosition = new Vector3(0f, ZombieLocalVerticalOffset, 0f);
        zombie.localRotation = Quaternion.identity;
        zombie.localScale = Vector3.one * ZombieLocalScale;
    }

    private static void AttachCatVisual(Transform cat, Transform player)
    {
        cat.SetParent(player, false);
        cat.localPosition = new Vector3(0f, CatLocalVerticalOffset, 0f);
        cat.localRotation = Quaternion.identity;
        cat.localScale = Vector3.one * CatLocalScale;
    }

    private static void AddDecorations(
        Scene scene,
        Transform parent,
        GameObject prefab,
        IReadOnlyList<Vector3> positions,
        string name,
        GridManager grid,
        bool blocksCell)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject decoration = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            decoration.name = $"{name}_{i + 1}";
            decoration.transform.SetParent(parent);
            Vector2Int gridPosition = new Vector2Int(
                Mathf.RoundToInt(positions[i].x),
                Mathf.RoundToInt(positions[i].z));
            decoration.transform.position = GridToWorld(grid, gridPosition);
            decoration.transform.rotation = Quaternion.Euler(0f, i * 67f, 0f);

            if (blocksCell)
            {
                Obstacle obstacle = decoration.AddComponent<Obstacle>();
                obstacle.gridPosition = gridPosition;
            }
        }
    }

    private static Vector3 GridToWorld(GridManager grid, Vector2Int position)
    {
        return new Vector3(
            grid.origin.x + position.x * grid.cellSize + grid.cellSize * 0.5f,
            grid.origin.y,
            grid.origin.z + position.y * grid.cellSize + grid.cellSize * 0.5f);
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }
        return name.Replace('/', '_').Replace('\\', '_');
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

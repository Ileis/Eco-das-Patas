using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TurnHudCanvasInstaller
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/Fase1_Arredores.unity",
        "Assets/Scenes/Fase2_Bosque.unity",
        "Assets/Scenes/Fase3_Pantano.unity"
    };

    [MenuItem("Eco das Patas/Adicionar HUD de turnos ao Canvas")]
    public static void InstallAllScenes()
    {
        foreach (string scenePath in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning($"Canvas não encontrado em {scenePath}.");
                continue;
            }

            EnsureTurnHud(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("HUD de turnos adicionado ao Canvas das cenas.");
    }

    private static void EnsureTurnHud(Canvas canvas)
    {
        Transform existing = canvas.transform.Find("TurnHudRoot");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        Button templateButton = canvas.GetComponentInChildren<Button>(true);
        Text templateText = templateButton != null
            ? templateButton.GetComponentInChildren<Text>(true)
            : canvas.GetComponentInChildren<Text>(true);
        Font font = templateText != null ? templateText.font : null;
        Sprite sprite = templateButton != null ? templateButton.image.sprite : null;

        GameObject root = new("TurnHudRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.one;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = Vector2.one;
        rootRect.anchoredPosition = new Vector2(-26f, -28f);
        rootRect.sizeDelta = new Vector2(190f, 42f);

        GameObject labelObject = new(
            "TurnCounterText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        labelObject.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(0f, 18f);

        Text label = labelObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = 14;
        label.alignment = TextAnchor.UpperLeft;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = "Turno 1";

        GameObject backgroundObject = new(
            "TurnProgressBackground",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        backgroundObject.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = new Vector2(1f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        backgroundRect.sizeDelta = new Vector2(0f, 14f);

        Image background = backgroundObject.GetComponent<Image>();
        background.sprite = sprite;
        background.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        background.raycastTarget = false;

        GameObject fillObject = new(
            "TurnProgressFill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        Image fill = fillObject.GetComponent<Image>();
        fill.sprite = sprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.color = new Color(0.88f, 0.24f, 0.24f, 1f);
        fill.raycastTarget = false;
    }
}

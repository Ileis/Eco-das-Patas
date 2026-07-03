using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnGameStart()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("GameFlowManager");
            go.AddComponent<GameFlowManager>();
        }
    }

    private readonly string[] _phaseScenes = {
        "Fase0_Apartamento",
        "Fase1_Arredores",
        "Fase2_Bosque",
        "Fase3_Pantano"
    };

    private GameObject _gameFlowCanvasGo;
    private CanvasGroup _transitionCanvasGroup;
    private Text _transitionText;
    private GameObject _victoryPanel;
    private GameObject _defeatPanel;

    private Text _victoryTitle;
    private Text _victorySubtitle;
    private Button _victoryButton;
    private Text _victoryButtonText;

    private Text _defeatTitle;
    private Text _defeatSubtitle;
    private Button _defeatButton;
    private Text _defeatButtonText;

    private bool _isTransitioning = false;
    private bool _isGameOver = false;

    private Font _cachedFont;
    private Sprite _cachedButtonSprite;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFlow();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeFlow()
    {
        CacheTemplates();
        BuildCanvasAndPanels();
        StartCoroutine(FadeOutTransition(0.8f));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isGameOver = false;
        _isTransitioning = false;

        CacheTemplates();
        UpdateCanvasTemplates();

        if (_victoryPanel != null) _victoryPanel.SetActive(false);
        if (_defeatPanel != null) _defeatPanel.SetActive(false);

        StartCoroutine(FadeOutTransition(0.8f));
    }

    private void CacheTemplates()
    {
        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        foreach (var txt in texts)
        {
            if (txt != null && txt.transform.root != transform && txt.transform.root.name != "GameFlowCanvas" && txt.font != null && txt.font.name != "Arial")
            {
                _cachedFont = txt.font;
                break;
            }
        }
        if (_cachedFont == null)
        {
            foreach (var txt in texts)
            {
                if (txt != null && txt.transform.root != transform && txt.transform.root.name != "GameFlowCanvas" && txt.font != null)
                {
                    _cachedFont = txt.font;
                    break;
                }
            }
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (var btn in buttons)
        {
            if (btn != null && btn.transform.root != transform && btn.transform.root.name != "GameFlowCanvas" && btn.image != null && btn.image.sprite != null)
            {
                _cachedButtonSprite = btn.image.sprite;
                break;
            }
        }
    }

    private void BuildCanvasAndPanels()
    {
        if (_gameFlowCanvasGo != null) return;

        _gameFlowCanvasGo = new GameObject("GameFlowCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(_gameFlowCanvasGo);

        Canvas canvas = _gameFlowCanvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = _gameFlowCanvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Font fontToUse = _cachedFont != null ? _cachedFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Transition Panel
        GameObject transGo = new GameObject("TransitionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        transGo.transform.SetParent(_gameFlowCanvasGo.transform, false);

        RectTransform transRect = transGo.GetComponent<RectTransform>();
        transRect.anchorMin = Vector2.zero;
        transRect.anchorMax = Vector2.one;
        transRect.offsetMin = Vector2.zero;
        transRect.offsetMax = Vector2.zero;

        Image transImg = transGo.GetComponent<Image>();
        transImg.color = new Color(0.06f, 0.06f, 0.07f, 1f);

        _transitionCanvasGroup = transGo.GetComponent<CanvasGroup>();
        _transitionCanvasGroup.alpha = 1f;
        _transitionCanvasGroup.blocksRaycasts = true;

        GameObject transTextGo = new GameObject("TransitionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        transTextGo.transform.SetParent(transGo.transform, false);
        RectTransform transTextRect = transTextGo.GetComponent<RectTransform>();
        transTextRect.anchorMin = new Vector2(0.1f, 0.4f);
        transTextRect.anchorMax = new Vector2(0.9f, 0.6f);
        transTextRect.offsetMin = Vector2.zero;
        transTextRect.offsetMax = Vector2.zero;

        _transitionText = transTextGo.GetComponent<Text>();
        _transitionText.font = fontToUse;
        _transitionText.fontSize = 32;
        _transitionText.alignment = TextAnchor.MiddleCenter;
        _transitionText.color = Color.white;
        _transitionText.text = "Eco das Patas...";

        // 2. Victory Panel
        _victoryPanel = CreateOverlayPanel("VictoryPanel", new Color(0.05f, 0.12f, 0.08f, 0.96f), "VITÓRIA!", "Todos os zumbis foram eliminados!", "Avançar", LoadNextPhase);
        _victoryPanel.SetActive(false);

        // 3. Defeat Panel
        _defeatPanel = CreateOverlayPanel("DefeatPanel", new Color(0.18f, 0.04f, 0.04f, 0.96f), "DERROTA!", "Seus gatos foram vencidos...", "Tentar Novamente", RestartCurrentPhase);
        _defeatPanel.SetActive(false);
    }

    private GameObject CreateOverlayPanel(string name, Color bgColor, string titleText, string subtitleText, string btnText, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelGo.transform.SetParent(_gameFlowCanvasGo.transform, false);

        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = panelGo.GetComponent<Image>();
        panelImg.color = bgColor;

        GameObject containerGo = new GameObject("Container", typeof(RectTransform));
        containerGo.transform.SetParent(panelGo.transform, false);
        RectTransform containerRect = containerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.3f, 0.2f);
        containerRect.anchorMax = new Vector2(0.7f, 0.8f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        Font fontToUse = _cachedFont != null ? _cachedFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Title
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        titleGo.transform.SetParent(containerGo.transform, false);
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.7f);
        titleRect.anchorMax = new Vector2(1f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text title = titleGo.GetComponent<Text>();
        title.font = fontToUse;
        title.fontSize = 54;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        title.text = titleText;

        if (name == "VictoryPanel") _victoryTitle = title;
        else _defeatTitle = title;

        // Subtitle
        GameObject subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        subGo.transform.SetParent(containerGo.transform, false);
        RectTransform subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0.45f);
        subRect.anchorMax = new Vector2(1f, 0.65f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        Text subtitle = subGo.GetComponent<Text>();
        subtitle.font = fontToUse;
        subtitle.fontSize = 20;
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        subtitle.text = subtitleText;

        if (name == "VictoryPanel") _victorySubtitle = subtitle;
        else _defeatSubtitle = subtitle;

        // Button
        GameObject btnGo = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(containerGo.transform, false);
        RectTransform btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.25f, 0.15f);
        btnRect.anchorMax = new Vector2(0.75f, 0.35f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnGo.GetComponent<Image>();
        btnImg.sprite = _cachedButtonSprite;
        btnImg.type = _cachedButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        if (_cachedButtonSprite == null)
        {
            btnImg.color = name == "VictoryPanel" ? new Color(0.12f, 0.45f, 0.25f, 1f) : new Color(0.6f, 0.15f, 0.15f, 1f);
        }
        else
        {
            btnImg.color = Color.cornflowerBlue;
        }

        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClickAction);

        ColorBlock cb = btn.colors;
        cb.normalColor = btnImg.color;
        cb.highlightedColor = btnImg.color * 1.15f;
        cb.pressedColor = btnImg.color * 0.85f;
        cb.selectedColor = btnImg.color;
        btn.colors = cb;

        if (name == "VictoryPanel") _victoryButton = btn;
        else _defeatButton = btn;

        // Button Text
        GameObject btnTextGo = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        btnTextGo.transform.SetParent(btnGo.transform, false);
        RectTransform btnTextRect = btnTextGo.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        Text bText = btnTextGo.GetComponent<Text>();
        bText.font = fontToUse;
        bText.fontSize = 18;
        bText.alignment = TextAnchor.MiddleCenter;
        bText.color = Color.white;
        bText.text = btnText;

        if (name == "VictoryPanel") _victoryButtonText = bText;
        else _defeatButtonText = bText;

        CanvasGroup cg = panelGo.GetComponent<CanvasGroup>();
        if (cg == null) cg = panelGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = true;

        return panelGo;
    }

    private void UpdateCanvasTemplates()
    {
        if (_gameFlowCanvasGo == null) return;

        Font fontToUse = _cachedFont != null ? _cachedFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (_transitionText != null) _transitionText.font = fontToUse;

        if (_victoryTitle != null) _victoryTitle.font = fontToUse;
        if (_victorySubtitle != null) _victorySubtitle.font = fontToUse;
        if (_victoryButtonText != null) _victoryButtonText.font = fontToUse;

        if (_defeatTitle != null) _defeatTitle.font = fontToUse;
        if (_defeatSubtitle != null) _defeatSubtitle.font = fontToUse;
        if (_defeatButtonText != null) _defeatButtonText.font = fontToUse;

        UpdateButtonVisuals(_victoryButton, _victoryButtonText, "VictoryPanel");
        UpdateButtonVisuals(_defeatButton, _defeatButtonText, "DefeatPanel");
    }

    private void UpdateButtonVisuals(Button button, Text buttonText, string panelName)
    {
        if (button == null) return;
        Image btnImg = button.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.sprite = _cachedButtonSprite;
            btnImg.type = _cachedButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Color baseColor;
            if (_cachedButtonSprite == null)
            {
                baseColor = panelName == "VictoryPanel" ? new Color(0.12f, 0.45f, 0.25f, 1f) : new Color(0.6f, 0.15f, 0.15f, 1f);
            }
            else
            {
                baseColor = Color.white;
            }

            btnImg.color = baseColor;

            ColorBlock cb = button.colors;
            cb.normalColor = baseColor;
            cb.highlightedColor = baseColor * 1.15f;
            cb.pressedColor = baseColor * 0.85f;
            cb.selectedColor = baseColor;
            button.colors = cb;
        }
        if (buttonText != null)
        {
            Font fontToUse = _cachedFont != null ? _cachedFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.font = fontToUse;
        }
    }

    public void CheckGameEndConditions()
    {
        if (_isGameOver || _isTransitioning) return;

        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);
        int alivePlayers = 0;
        int aliveEnemies = 0;

        foreach (Unit unit in allUnits)
        {
            if (unit == null || unit.IsDead) continue;
            if (unit is Enemy)
            {
                aliveEnemies++;
            }
            else
            {
                alivePlayers++;
            }
        }

        if (alivePlayers == 0 && aliveEnemies == 0) return;

        if (alivePlayers == 0)
        {
            TriggerDefeat();
        }
        else if (aliveEnemies == 0)
        {
            TriggerVictory();
        }
    }

    private void TriggerVictory()
    {
        _isGameOver = true;
        Debug.Log("Vitória do Jogador!");

        string activeSceneName = SceneManager.GetActiveScene().name;
        int currentIndex = Array.IndexOf(_phaseScenes, activeSceneName);
        int nextIndex = currentIndex + 1;

        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(true);
            CanvasGroup cg = _victoryPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = _victoryPanel.AddComponent<CanvasGroup>();
            StartCoroutine(FadeCanvasGroup(cg, cg.alpha, 1f, 0.4f, null));

            if (_victorySubtitle != null)
            {
                LevelSceneInfo info = FindAnyObjectByType<LevelSceneInfo>();
                string nextName = (nextIndex >= 0 && nextIndex < _phaseScenes.Length) ? GetPhaseFriendlyName(_phaseScenes[nextIndex]) : "";
                _victorySubtitle.text = string.IsNullOrEmpty(nextName) 
                    ? "Parabéns! Você completou a última fase do jogo!"
                    : $"Fase {info?.phaseNumber + 1} concluída com sucesso!";
            }

            if (_victoryButtonText != null)
            {
                if (nextIndex >= 0 && nextIndex < _phaseScenes.Length)
                {
                    string friendlyName = GetPhaseFriendlyName(_phaseScenes[nextIndex]);
                    _victoryButtonText.text = $"Avançar para: {friendlyName}";
                }
                else
                {
                    _victoryButtonText.text = "Recomeçar do Início";
                }
            }
        }
    }

    private void TriggerDefeat()
    {
        _isGameOver = true;
        Debug.Log("Derrota do Jogador!");

        if (_defeatPanel != null)
        {
            _defeatPanel.SetActive(true);
            CanvasGroup cg = _defeatPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = _defeatPanel.AddComponent<CanvasGroup>();
            StartCoroutine(FadeCanvasGroup(cg, cg.alpha, 1f, 0.4f, null));

            if (_defeatSubtitle != null)
            {
                _defeatSubtitle.text = "Os zumbis venceram a batalha...";
            }

            if (_defeatButtonText != null)
            {
                _defeatButtonText.text = "Tentar Novamente";
            }
        }
    }

    public void LoadNextPhase()
    {
        if (_isTransitioning) return;

        string activeSceneName = SceneManager.GetActiveScene().name;
        int currentIndex = Array.IndexOf(_phaseScenes, activeSceneName);
        int nextIndex = currentIndex + 1;

        if (nextIndex >= 0 && nextIndex < _phaseScenes.Length)
        {
            StartCoroutine(TransitionToScene(_phaseScenes[nextIndex]));
        }
        else
        {
            StartCoroutine(TransitionToScene(_phaseScenes[0]));
        }
    }

    public void RestartCurrentPhase()
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionToScene(SceneManager.GetActiveScene().name));
    }

    private string GetPhaseFriendlyName(string sceneName)
    {
        switch (sceneName)
        {
            case "Fase0_Apartamento": return "Apartamento";
            case "Fase1_Arredores": return "Arredores";
            case "Fase2_Bosque": return "Bosque";
            case "Fase3_Pantano": return "Pântano";
            default: return sceneName;
        }
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        _isTransitioning = true;

        if (_gameFlowCanvasGo != null)
        {
            Transform trans = _gameFlowCanvasGo.transform.Find("TransitionPanel");
            if (trans != null) trans.gameObject.SetActive(true);
        }

        if (_transitionText != null)
        {
            string friendlyName = GetPhaseFriendlyName(sceneName);
            _transitionText.text = $"Carregando: {friendlyName}...";
        }

        if (_transitionCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(_transitionCanvasGroup, _transitionCanvasGroup.alpha, 1f, 0.5f, null));
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator FadeOutTransition(float duration)
    {
        if (_transitionCanvasGroup != null)
        {
            _transitionCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(_transitionCanvasGroup, _transitionCanvasGroup.alpha, 0f, duration, () => {
                _transitionCanvasGroup.gameObject.SetActive(false);
            }));
        }
        _isTransitioning = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration, Action onComplete)
    {
        float elapsed = 0f;
        cg.alpha = start;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
        onComplete?.Invoke();
    }
}

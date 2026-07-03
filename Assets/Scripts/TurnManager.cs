using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public event Action<Unit> TurnStarted;

    private List<Unit> _turnOrder = new();
    private int _currentIndex = -1;
    private int _turnCount = 0;

    private Canvas _hudCanvas;
    private Image _turnProgressFill;
    private Text _turnCounterText;
    private GameObject _activeTurnIndicator;
    private Font _uiFontTemplate;
    private Sprite _uiSpriteTemplate;

    private readonly Vector3 _indicatorLocalOffset = new(0f, 15f, 0f);
    private readonly Vector2 _hudAnchorOffset = new(-26f, -28f);
    private readonly Vector2 _hudSize = new(190f, 42f);

    public Unit CurrentUnit =>
        (_currentIndex >= 0 && _currentIndex < _turnOrder.Count) ? _turnOrder[_currentIndex] : null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Unit[] unitsInScene = FindObjectsByType<Unit>();
        _turnOrder = unitsInScene.OrderByDescending(u => u.initiative).ToList();
        CacheUiTemplates();
        BuildTurnHud();
        BindEndTurnButton();

        NextTurn();
    }

    private void BindEndTurnButton()
    {
        Button[] buttons = FindObjectsByType<Button>();
        foreach (Button button in buttons)
        {
            if (button == null) continue;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null || label.text != "Fim de Turno") continue;

            bool hasValidListener = false;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) != null
                    && button.onClick.GetPersistentMethodName(i)
                        == nameof(EndTurnButtonPressed))
                {
                    hasValidListener = true;
                    break;
                }
            }

            if (!hasValidListener)
            {
                button.onClick.AddListener(EndTurnButtonPressed);
            }
        }
    }

    public void RemoveUnit(Unit unit)
    {
        int removedIndex = _turnOrder.IndexOf(unit);
        _turnOrder.Remove(unit);

        if (removedIndex != -1 && removedIndex < _currentIndex)
        {
            _currentIndex--;
        }

        UpdateTurnHud();
    }

    public void NextTurn()
    {
        if (_turnOrder.Count == 0) return;

        _currentIndex++;
        if (_currentIndex >= _turnOrder.Count)
        {
            _currentIndex = 0;
        }

        _turnCount++;
        CurrentUnit.StartTurn();
        Debug.Log($"Turno de ${CurrentUnit.name} (iniciativa {CurrentUnit.initiative})");
        AttachTurnIndicator(CurrentUnit);
        UpdateTurnHud();
        TurnStarted?.Invoke(CurrentUnit);

        if (CurrentUnit is Enemy enemy)
        {
            enemy.TakeAITurn();
        }

    }

    public void EndTurnButtonPressed()
    {
        if (CurrentUnit != null && !(CurrentUnit is Enemy))
        {
            NextTurn();
        }
    }

    private void CacheUiTemplates()
    {
        Button[] buttons = FindObjectsByType<Button>();
        foreach (Button button in buttons)
        {
            if (button == null) continue;

            if (_uiSpriteTemplate == null && button.image != null)
            {
                _uiSpriteTemplate = button.image.sprite;
            }

            if (_uiFontTemplate == null)
            {
                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    _uiFontTemplate = text.font;
                }
            }

            if (_uiSpriteTemplate != null && _uiFontTemplate != null)
            {
                break;
            }
        }

        if (_uiFontTemplate == null)
        {
            Text[] texts = FindObjectsByType<Text>();
            if (texts.Length > 0)
            {
                _uiFontTemplate = texts[0].font;
            }
        }
    }

    private void BuildTurnHud()
    {
        _hudCanvas = FindAnyObjectByType<Canvas>();
        if (_hudCanvas == null) return;
        if (_hudCanvas.transform.Find("TurnHudRoot") != null)
        {
            Transform existingRoot = _hudCanvas.transform.Find("TurnHudRoot");
            _turnProgressFill = existingRoot
                .Find("TurnProgressBackground/TurnProgressFill")
                ?.GetComponent<Image>();
            _turnCounterText = existingRoot.Find("TurnCounterText")?.GetComponent<Text>();
            UpdateTurnHud();
            return;
        }

        GameObject root = new GameObject("TurnHudRoot", typeof(RectTransform));
        root.transform.SetParent(_hudCanvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = _hudAnchorOffset;
        rootRect.sizeDelta = _hudSize;

        GameObject labelGo = new GameObject("TurnCounterText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGo.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 18f);

        _turnCounterText = labelGo.GetComponent<Text>();
        _turnCounterText.font = _uiFontTemplate != null ? _uiFontTemplate : Resources.GetBuiltinResource<Font>("Arial.ttf");
        _turnCounterText.fontSize = 14;
        _turnCounterText.alignment = TextAnchor.UpperLeft;
        _turnCounterText.color = Color.white;
        _turnCounterText.raycastTarget = false;
        _turnCounterText.text = "Turno 1";

        GameObject backgroundGo = new GameObject("TurnProgressBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundGo.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = backgroundGo.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        backgroundRect.anchoredPosition = new Vector2(0f, 0f);
        backgroundRect.sizeDelta = new Vector2(0f, 14f);

        Image backgroundImage = backgroundGo.GetComponent<Image>();
        backgroundImage.sprite = _uiSpriteTemplate;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        backgroundImage.raycastTarget = false;

        GameObject fillGo = new GameObject("TurnProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(backgroundGo.transform, false);
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        _turnProgressFill = fillGo.GetComponent<Image>();
        _turnProgressFill.sprite = _uiSpriteTemplate;
        _turnProgressFill.type = Image.Type.Filled;
        _turnProgressFill.fillMethod = Image.FillMethod.Horizontal;
        _turnProgressFill.fillOrigin = 0;
        _turnProgressFill.fillAmount = 0f;
        _turnProgressFill.color = new Color(0.88f, 0.24f, 0.24f, 1f);
        _turnProgressFill.raycastTarget = false;

        UpdateTurnHud();
    }

    private void UpdateTurnHud()
    {
        if (_turnCounterText != null)
        {
            _turnCounterText.text = $"Turno {_turnCount}";
        }

        if (_turnProgressFill != null)
        {
            float progress = 0f;
            if (_turnOrder.Count > 0)
            {
                progress = (_currentIndex + 1f) / _turnOrder.Count;
            }

            _turnProgressFill.fillAmount = Mathf.Clamp01(progress);
        }
    }

    private void AttachTurnIndicator(Unit unit)
    {
        if (_activeTurnIndicator != null)
        {
            Destroy(_activeTurnIndicator);
            _activeTurnIndicator = null;
        }

        if (unit == null) return;

        _activeTurnIndicator = CreateTurnIndicator();
        _activeTurnIndicator.transform.SetParent(unit.transform, false);
        _activeTurnIndicator.transform.localPosition = _indicatorLocalOffset;
        _activeTurnIndicator.transform.localRotation = Quaternion.identity;
    }

    private GameObject CreateTurnIndicator()
    {
        GameObject indicator = new("ActiveTurnIndicator");
        TextMesh textMesh = indicator.AddComponent<TextMesh>();
        textMesh.text = "▼";
        textMesh.font = _uiFontTemplate != null
            ? _uiFontTemplate
            : Resources.GetBuiltinResource<Font>("Arial.ttf");
        textMesh.fontSize = 80;
        textMesh.characterSize = 1.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(0.92f, 0.15f, 0.15f, 1f);

        MeshRenderer renderer = indicator.GetComponent<MeshRenderer>();
        if (renderer != null && textMesh.font != null)
        {
            renderer.material = textMesh.font.material;
        }

        return indicator;
    }
}

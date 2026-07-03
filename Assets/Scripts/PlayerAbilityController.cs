using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilityController : MonoBehaviour
{
    public Unit unit;
    public UnitSelectionManager selectionManager;

    public Ability patada;
    public Ability miar;
    public Ability cuspir;
    public Ability ronronar;

    private readonly List<Button> combatButtons = new();
    private Button moveButton;

    private void Start()
    {
        if (selectionManager == null)
        {
            selectionManager = FindAnyObjectByType<UnitSelectionManager>();
        }

        CacheCombatButtons();
        CreateMoveButtonIfNeeded();
        HookTurnEvents();
        RefreshButtonVisibility();
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TurnStarted -= HandleTurnStarted;
        }
    }

    public void UsePatada() => BeginAbilityTargeting(patada);
    public void UseMiar() => BeginAbilityTargeting(miar);
    public void UseCuspir() => BeginAbilityTargeting(cuspir);
    public void UseRonronar() => BeginAbilityTargeting(ronronar);
    public void UseMove() => BeginMove();

    // public void UseRonronar()
    // {
    //     if (!IsPlayerTurn() || unit == null || ronronar == null) return;

    //     if (!unit.TryUseAbility(ronronar, unit))
    //     {
    //         Debug.Log($"Não foi possível usar {ronronar.abilityName}.");
    //     }
    // }

    private void BeginAbilityTargeting(Ability ability)
    {
        if (!IsPlayerTurn() || unit == null || ability == null) return;
        if (selectionManager == null) return;

        if (!selectionManager.BeginAbilityMode(unit, ability))
        {
            Debug.Log($"Não foi possível preparar {ability.abilityName}.");
        }
    }

    private void BeginMove()
    {
        if (!IsPlayerTurn() || unit == null) return;
        if (selectionManager == null) return;

        if (!selectionManager.BeginMoveMode(unit))
        {
            Debug.Log("Não foi possível entrar no modo de movimento.");
        }
    }

    private bool IsPlayerTurn()
    {
        return TurnManager.Instance == null
            || TurnManager.Instance.CurrentUnit == null
            || TurnManager.Instance.CurrentUnit == unit;
    }

    private void HookTurnEvents()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TurnStarted += HandleTurnStarted;
        }
    }

    private void HandleTurnStarted(Unit currentUnit)
    {
        RefreshButtonVisibility();
        if (selectionManager != null && currentUnit != unit)
        {
            selectionManager.ClearSelection();
        }
    }

    private void RefreshButtonVisibility()
    {
        bool visible = IsPlayerTurn();

        for (int i = 0; i < combatButtons.Count; i++)
        {
            if (combatButtons[i] != null)
            {
                combatButtons[i].gameObject.SetActive(visible);
            }
        }

        if (moveButton != null)
        {
            moveButton.gameObject.SetActive(visible);
        }
    }

    private void CacheCombatButtons()
    {
        combatButtons.Clear();

        AddButtonByLabel("Patada");
        AddButtonByLabel("Miar");
        AddButtonByLabel("Cuspir");
        AddButtonByLabel("Ronronar");
        AddButtonByLabel("Fim de Turno");
    }

    private void AddButtonByLabel(string label)
    {
        Button button = FindButtonByLabel(label);
        if (button != null && !combatButtons.Contains(button))
        {
            combatButtons.Add(button);
        }
    }

    private void CreateMoveButtonIfNeeded()
    {
        moveButton = FindButtonByLabel("Andar");
        if (moveButton != null) return;

        Button template = combatButtons.Count > 0 ? combatButtons[0] : FindAnyObjectByType<Button>();
        if (template == null) return;

        GameObject clone = Instantiate(template.gameObject, template.transform.parent);
        clone.name = "Move Button";

        RectTransform templateRect = template.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (templateRect != null && cloneRect != null)
        {
            cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, 38.7f);
            cloneRect.sizeDelta = templateRect.sizeDelta;
            cloneRect.anchorMin = templateRect.anchorMin;
            cloneRect.anchorMax = templateRect.anchorMax;
            cloneRect.pivot = templateRect.pivot;
            cloneRect.localScale = templateRect.localScale;
        }

        moveButton = clone.GetComponent<Button>();
        if (moveButton == null) return;

        moveButton.onClick.RemoveAllListeners();
        moveButton.onClick.AddListener(UseMove);
        SetButtonLabel(moveButton, "Andar");
        moveButton.gameObject.SetActive(IsPlayerTurn());
    }

    private Button FindButtonByLabel(string label)
    {
        Button[] buttons = FindObjectsByType<Button>();
        foreach (Button button in buttons)
        {
            if (button == null) continue;

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null && text.text == label)
            {
                return button;
            }
        }

        return null;
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }
}

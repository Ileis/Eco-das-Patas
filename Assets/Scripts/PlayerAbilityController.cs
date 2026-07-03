using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerAbilityController : MonoBehaviour
{
    public Unit unit;
    public UnitSelectionManager selectionManager;

    public Ability patada;
    public Ability miar;
    public Ability cuspir;
    public Ability ronronar;

    private readonly List<Button> actionButtons = new();

    private void Start()
    {
        if (selectionManager == null)
        {
            selectionManager = FindAnyObjectByType<UnitSelectionManager>();
        }

        EnsureMoveButton();
        CacheActionButtons();
        BindAbilityButtons();
        HookTurnEvents();

        if (unit != null)
        {
            unit.OnMovementStarted += RefreshButtonVisibility;
            unit.OnMovementFinished += RefreshButtonVisibility;
        }

        RefreshButtonVisibility();
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TurnStarted -= HandleTurnStarted;
        }

        if (unit != null)
        {
            unit.OnMovementStarted -= RefreshButtonVisibility;
            unit.OnMovementFinished -= RefreshButtonVisibility;
        }
    }

    public void UsePatada() => BeginAbilityTargeting(patada);
    public void UseMiar() => BeginAbilityTargeting(miar);
    public void UseCuspir() => BeginAbilityTargeting(cuspir);
    public void UseRonronar() => BeginAbilityTargeting(ronronar);
    public void UseMove() => BeginMove();

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
        return (TurnManager.Instance == null
            || TurnManager.Instance.CurrentUnit == null
            || TurnManager.Instance.CurrentUnit == unit)
            && (unit == null || !unit.IsMoving);
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

        for (int i = 0; i < actionButtons.Count; i++)
        {
            if (actionButtons[i] != null)
            {
                actionButtons[i].gameObject.SetActive(visible);
            }
        }
    }

    private void CacheActionButtons()
    {
        actionButtons.Clear();

        AddButtonByLabel("Andar");
        AddButtonByLabel("Patada");
        AddButtonByLabel("Miar");
        AddButtonByLabel("Cuspir");
        AddButtonByLabel("Ronronar");
        AddButtonByLabel("Fim de Turno");
    }

    private void AddButtonByLabel(string label)
    {
        Button button = FindButtonByLabel(label);
        if (button != null && !actionButtons.Contains(button))
        {
            actionButtons.Add(button);
        }
    }

    private void BindAbilityButtons()
    {
        BindButton("Patada", nameof(UsePatada), UsePatada);
        BindButton("Miar", nameof(UseMiar), UseMiar);
        BindButton("Cuspir", nameof(UseCuspir), UseCuspir);
        BindButton("Ronronar", nameof(UseRonronar), UseRonronar);
    }

    private void BindButton(string label, string methodName, UnityAction action)
    {
        Button button = FindButtonByLabel(label);
        if (button == null) return;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this
                && button.onClick.GetPersistentMethodName(i) == methodName)
            {
                return;
            }
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void EnsureMoveButton()
    {
        Button moveButton = FindButtonByLabel("Andar");
        bool wasCreated = moveButton == null;

        if (wasCreated)
        {
            Button template = FindButtonByLabel("Patada") ?? FindAnyObjectByType<Button>();
            if (template == null) return;

            GameObject clone = Instantiate(template.gameObject, template.transform.parent);
            clone.name = "Move Button";

            RectTransform templateRect = template.GetComponent<RectTransform>();
            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            if (templateRect != null && cloneRect != null)
            {
                cloneRect.anchoredPosition3D = new Vector3(-880f, -300f, 0f);
                cloneRect.sizeDelta = templateRect.sizeDelta;
                cloneRect.anchorMin = templateRect.anchorMin;
                cloneRect.anchorMax = templateRect.anchorMax;
                cloneRect.pivot = templateRect.pivot;
                cloneRect.localScale = templateRect.localScale;
            }

            moveButton = clone.GetComponent<Button>();
        }

        if (moveButton == null) return;

        RectTransform moveRect = moveButton.GetComponent<RectTransform>();
        if (moveRect != null)
        {
            moveRect.anchoredPosition3D = new Vector3(-880f, -300f, 0f);
        }

        if (wasCreated)
        {
            moveButton.onClick.RemoveAllListeners();
        }
        else
        {
            moveButton.onClick.RemoveListener(UseMove);
        }
        moveButton.onClick.AddListener(UseMove);
        SetButtonLabel(moveButton, "Andar");
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

using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    private enum SelectionMode
    {
        None,
        Move,
        AbilityTarget
    }

    public GridInputHandler gridInputHandler;
    public GameObject rangeHighlightPrefab;
    public GameObject pathHighlightPrefab;

    public Vector3 highlightRotarionEuler = new Vector3(90f, 0f, 0f);

    private Unit selectedUnit;
    private Ability selectedAbility;
    private SelectionMode currentMode = SelectionMode.None;

    private List<GameObject> activeRangeHighlights = new List<GameObject>();
    private List<GameObject> activePathHighlights = new List<GameObject>();

    private readonly List<Vector2Int> currentHighlightCells = new List<Vector2Int>();
    private Dictionary<Vector2Int, Vector2Int> currentCameFrom = new Dictionary<Vector2Int, Vector2Int>();

    private void OnEnable()
    {
        if (gridInputHandler != null)
        {
            gridInputHandler.OnCellClicked.AddListener(HandleCellClicked);
            gridInputHandler.OnCellHovered.AddListener(HandleCellHovered);
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TurnStarted += HandleTurnStarted;
        }
    }

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            HandleTurnStarted(TurnManager.Instance.CurrentUnit);
        }
    }

    private void OnDisable()
    {
        if (gridInputHandler != null)
        {
            gridInputHandler.OnCellClicked.RemoveListener(HandleCellClicked);
            gridInputHandler.OnCellHovered.RemoveListener(HandleCellHovered);
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TurnStarted -= HandleTurnStarted;
        }
    }

    private void HandleCellClicked(Vector2Int gridPos)
    {
        GridCell clickedCell = GridManager.Instance.GetCell(gridPos);
        if (clickedCell == null) return;
        if (selectedUnit == null) return;

        // if (selectedUnit == null)
        // {
        //     if (clickedCell.occupant is Unit unit)
        //     {
        //         TrySelectCurrentUnit(unit);
        //     }
        //     return;
        // }

        if (currentMode == SelectionMode.AbilityTarget)
        {
            if (currentHighlightCells.Contains(gridPos))
            {
                if (selectedUnit.TryUseAbility(selectedAbility, clickedCell.occupant))
                {
                    if (selectedUnit != null && !selectedUnit.IsDead && CanContinueActing())
                    {
                        BeginAbilityMode(selectedUnit, selectedAbility);
                    }
                    else
                    {
                        ClearSelection();
                    }
                }
                return;
            }

            // if (clickedCell.occupant is Unit abilityUnit && TrySelectCurrentUnit(abilityUnit))
            // {
            //     return;
            // }

            return;
        }

        if (currentMode == SelectionMode.Move)
        {
            if (currentHighlightCells.Contains(gridPos) && !clickedCell.isOccupied)
            {
                selectedUnit.MoveTo(gridPos);
                ClearSelection();
                return;
            }

            if (clickedCell.occupant is Unit moveUnit && TrySelectCurrentUnit(moveUnit))
            {
                return;
            }

            return;
        }

        if (gridPos == selectedUnit.GridPosition)
        {
            ClearSelection();
            return;
        }

        if (clickedCell.occupant is Unit otherUnit)
        {
            TrySelectCurrentUnit(otherUnit);
            return;
        }

        ClearSelection();
    }

    private void HandleCellHovered(Vector2Int gridPos)
    {
        ClearPathHighlights();

        if (selectedUnit == null) return;
        if (currentMode != SelectionMode.Move) return;
        if (!currentHighlightCells.Contains(gridPos)) return;

        List<Vector2Int> path = Pathfinding.ReconstructPath(selectedUnit.GridPosition, gridPos, currentCameFrom);
        ShowPath(path);
    }

    public bool TrySelectCurrentUnit(Unit unit)
    {
        if (unit == null || unit.IsDead) return false;
        if (TurnManager.Instance != null && unit != TurnManager.Instance.CurrentUnit) return false;
        if (unit is Enemy) return false;

        selectedUnit = unit;
        selectedAbility = null;
        currentMode = SelectionMode.None;
        RefreshHighlights();
        return true;
    }

    public bool BeginMoveMode(Unit unit)
    {
        if (!TrySelectCurrentUnit(unit)) return false;
        if (selectedUnit != null && selectedUnit.HasMovedThisTurn) return false;
        currentMode = SelectionMode.Move;
        selectedAbility = null;
        RefreshHighlights();
        return true;
    }

    public bool BeginAbilityMode(Unit unit, Ability ability)
    {
        if (unit == null || ability == null || unit.IsDead) return false;
        if (TurnManager.Instance != null && unit != TurnManager.Instance.CurrentUnit) return false;
        if (unit is Enemy) return false;
        if (!unit.CanUseAbility(ability)) return false;

        selectedUnit = unit;
        selectedAbility = ability;
        currentMode = SelectionMode.AbilityTarget;
        RefreshHighlights();
        return true;
    }

    public void ClearSelection()
    {
        selectedUnit = null;
        selectedAbility = null;
        currentMode = SelectionMode.None;
        ClearRangeHighlights();
        ClearPathHighlights();
        currentCameFrom.Clear();
    }

    private void HandleTurnStarted(Unit currentUnit)
    {
        if (selectedUnit == null) return;

        if (currentUnit == null || selectedUnit != currentUnit)
        {
            ClearSelection();
            return;
        }

        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        ClearRangeHighlights();
        ClearPathHighlights();
        currentCameFrom.Clear();

        if (selectedUnit == null || selectedUnit.IsDead) return;

        switch (currentMode)
        {
            case SelectionMode.AbilityTarget:
                ShowAbilityRange(selectedUnit.GridPosition, selectedAbility != null ? selectedAbility.range : 0);
                break;
            case SelectionMode.Move:
                if (selectedUnit.HasMovedThisTurn)
                {
                    return;
                }
                ShowMoveRange(selectedUnit.GridPosition, selectedUnit.moveRange);
                break;
        }
    }

    private void ShowMoveRange(Vector2Int origin, int range)
    {
        Dictionary<Vector2Int, int> reachable = Pathfinding.GetReachableCells(origin, range, out currentCameFrom);
        currentHighlightCells.Clear();
        currentHighlightCells.AddRange(reachable.Keys);

        foreach (Vector2Int pos in currentHighlightCells)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.01f;
            GameObject highlight = Instantiate(rangeHighlightPrefab, worldPos, Quaternion.Euler(highlightRotarionEuler));
            activeRangeHighlights.Add(highlight);
        }
    }

    private void ShowAbilityRange(Vector2Int origin, int range)
    {
        currentHighlightCells.Clear();

        if (GridManager.Instance == null) return;

        for (int x = origin.x - range; x <= origin.x + range; x++)
        {
            for (int y = origin.y - range; y <= origin.y + range; y++)
            {
                if (selectedAbility.type != AbilityType.Buff &&  x == origin.x && y == origin.y) continue;

                Vector2Int pos = new(x, y);
                if (!GridManager.Instance.IsValidGridPosition(pos)) continue;

                int distance = Mathf.Abs(pos.x - origin.x) + Mathf.Abs(pos.y - origin.y);
                if (distance > range) continue;

                currentHighlightCells.Add(pos);
                Vector3 worldPos = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.01f;
                GameObject highlight = Instantiate(rangeHighlightPrefab, worldPos, Quaternion.Euler(highlightRotarionEuler));
                activeRangeHighlights.Add(highlight);
            }
        }
    }

    private void ShowPath(List<Vector2Int> path)
    {
        foreach (Vector2Int pos in path)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.02f;
            GameObject highlight = Instantiate(pathHighlightPrefab, worldPos, Quaternion.Euler(highlightRotarionEuler));
            activePathHighlights.Add(highlight);
        }
    }

    private void ClearRangeHighlights()
    {
        foreach (GameObject highlight in activeRangeHighlights)
        {
            Destroy(highlight);
        }
        activeRangeHighlights.Clear();
        currentHighlightCells.Clear();
    }

    private void ClearPathHighlights()
    {
        foreach (GameObject highlight in activePathHighlights)
        {
            Destroy(highlight);
        }
        activePathHighlights.Clear();
    }

    private bool CanContinueActing()
    {
        if (selectedUnit == null) return false;
        if (selectedUnit.IsDead) return false;
        if (selectedAbility == null) return false;
        return selectedUnit.CanUseAbility(selectedAbility);
    }
}

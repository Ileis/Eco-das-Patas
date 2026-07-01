using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public GridInputHandler gridInputHandler;
    public GameObject rangeHighlightPrefab;
    public Vector3 highlightRotarionEuler = new Vector3(90f, 0f, 0f);

    private Unit selectedUnit;
    private List<GameObject> activeHighlights = new List<GameObject>();
    private List<Vector2Int> currentRangeCells = new List<Vector2Int>();

    private void OnEnable()
    {
        if (gridInputHandler != null)
            gridInputHandler.OnCellClicked.AddListener(HandleCellClicked);
    }

    private void OnDisable()
    {
        if (gridInputHandler != null)
            gridInputHandler.OnCellClicked.RemoveListener(HandleCellClicked);
    }

    private void HandleCellClicked(Vector2Int gridPos)
    {
        
        GridCell clickedCell = GridManager.Instance.GetCell(gridPos);
        if (clickedCell == null) return;

        if (selectedUnit == null)
        {
            if (clickedCell.occupant is Unit unit)
            {
                SelectUnit(unit);
            }
            return;
        }

        if (gridPos == selectedUnit.GridPosition)
        {
            Deselect();
            return;
        }

        if (currentRangeCells.Contains(gridPos) && !clickedCell.isOccupied)
        {
            selectedUnit.MoveTo(gridPos);
            Deselect();
            return;
        }

        if (clickedCell.occupant is Unit otherUnit)
        {
            Deselect();
            SelectUnit(otherUnit);
            return;
        }

        Deselect();
    }

    private void SelectUnit(Unit unit)
    {
        selectedUnit = unit;
        ShowRange(unit.GridPosition, unit.moveRange);
    }

    private void Deselect()
    {
        selectedUnit = null;
        ClearHighlights();
    }

    private void ShowRange(Vector2Int origin, int range)
    {
        ClearHighlights();
        currentRangeCells = GetReachableCells(origin, range);

        foreach(Vector2Int pos in currentRangeCells)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.01f;
            GameObject highlight = Instantiate(rangeHighlightPrefab, worldPos, Quaternion.Euler(highlightRotarionEuler));
            activeHighlights.Add(highlight);
        }
    }

    private void ClearHighlights()
    {
        foreach (GameObject highlight in activeHighlights)
        {
            Destroy(highlight);
        }
        activeHighlights.Clear();
        currentRangeCells.Clear();
    }

    private List<Vector2Int> GetReachableCells(Vector2Int origin, int range)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int dx = -range; dx <= range; dx++)
        {
            int remaining = range - Mathf.Abs(dx);
            for (int dy = -remaining; dy <= remaining; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2Int pos = origin + new Vector2Int(dx, dy);
                if (!GridManager.Instance.IsValidGridPosition(pos)) continue;

                GridCell cell = GridManager.Instance.GetCell(pos);
                if (cell.isOccupied) continue;

                result.Add(pos);
            }
        }

        return result;
    }
}

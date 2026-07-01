using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public GridInputHandler gridInputHandler;
    public GameObject rangeHighlightPrefab;
    public GameObject pathHighlightPrefab;    
    public Vector3 highlightRotarionEuler = new Vector3(90f, 0f, 0f);

    private Unit selectedUnit;
    private List<GameObject> activeRangeHighlights = new List<GameObject>();
    private List<GameObject> activePathHighLights = new List<GameObject>();

    private List<Vector2Int> currentRangeCells = new List<Vector2Int>();
    private Dictionary<Vector2Int, Vector2Int> currentCameFrom = new Dictionary<Vector2Int, Vector2Int>();

    private void OnEnable()
    {
        if (gridInputHandler != null)
        {
            gridInputHandler.OnCellClicked.AddListener(HandleCellClicked);
            gridInputHandler.OnCellHovered.AddListener(HandleCellHovered);
        }

    }

    private void OnDisable()
    {
        if (gridInputHandler != null)
        {
            gridInputHandler.OnCellClicked.RemoveListener(HandleCellClicked);
            gridInputHandler.OnCellHovered.RemoveListener(HandleCellHovered);
        }
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

    private void HandleCellHovered(Vector2Int gridPos)
    {
        ClearPathHighlights();

        if (selectedUnit == null) return;
        if (!currentRangeCells.Contains(gridPos)) return;

        List<Vector2Int> path = Pathfinding.ReconstructPath(selectedUnit.GridPosition, gridPos, currentCameFrom);
        ShowPath(path);
    }
    private void SelectUnit(Unit unit)
    {
        selectedUnit = unit;
        ShowRange(unit.GridPosition, unit.moveRange);
    }

    private void Deselect()
    {
        selectedUnit = null;
        ClearRangeHighlights();
        ClearPathHighlights();
        currentCameFrom.Clear();
    }

    private void ShowRange(Vector2Int origin, int range)
    {
        ClearRangeHighlights();
        ClearPathHighlights();

        Dictionary<Vector2Int, int> reachable = Pathfinding.GetReachableCells(origin, range, out currentCameFrom);
        currentRangeCells = new List<Vector2Int>(reachable.Keys) ;

        foreach(Vector2Int pos in currentRangeCells)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.01f;
            GameObject highlight = Instantiate(rangeHighlightPrefab, worldPos, Quaternion.Euler(highlightRotarionEuler));
            activeRangeHighlights.Add(highlight);
        }
    }

    private void ShowPath(List<Vector2Int> path)
    {
        foreach (Vector2Int pos in path)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.02f;
            GameObject highlight = Instantiate(pathHighlightPrefab, worldPos, Quaternion.Euler(highlightRotarionEuler));
            activePathHighLights.Add(highlight);
        }
    }

    private void ClearRangeHighlights()
    {
        foreach (GameObject highlight in activeRangeHighlights)
        {
            Destroy(highlight);
        }
        activeRangeHighlights.Clear();
        currentRangeCells.Clear();
    }

    private void ClearPathHighlights()
    {
        foreach (GameObject highlight in activePathHighLights)
        {
            Destroy(highlight);
        }
        activePathHighLights.Clear();
    }
}

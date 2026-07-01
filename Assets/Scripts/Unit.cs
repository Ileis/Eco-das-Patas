using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Vector2Int startPosition;
    public int moveRange = 3;
    public Vector2Int GridPosition { get; private set; }

    private void Start()
    {
        PlaceOnGrid(startPosition);
    }

    public void PlaceOnGrid(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        transform.position = GridManager.Instance.GridToWorld(gridPosition);

        GridCell cell = GridManager.Instance.GetCell(gridPosition);
        if (cell != null)
        {
            cell.isOccupied = true;
            cell.occupant = this;
        }
    }

    public void MoveTo(Vector2Int newGridPosition)
    {
        GridCell oldCell = GridManager.Instance.GetCell(GridPosition);
        if (oldCell != null)
        {
            oldCell.isOccupied = false;
            oldCell.occupant = null;
        }

        GridPosition = newGridPosition;
        transform.position = GridManager.Instance.GridToWorld(newGridPosition);

        GridCell newCell = GridManager.Instance.GetCell(newGridPosition);
        if (newCell != null)
        {
            newCell.isOccupied = true;
            newCell.occupant = this;
        }
    }
}

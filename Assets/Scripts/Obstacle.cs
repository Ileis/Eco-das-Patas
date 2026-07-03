using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public Vector2Int gridPosition;

    private void Start()
    {
        PlaceOnGrid(gridPosition);
    }

    public void PlaceOnGrid(Vector2Int position)
    {
        gridPosition = position;
        if (GridManager.Instance == null || !GridManager.Instance.IsValidGridPosition(position))
        {
            Debug.LogWarning($"Obstáculo {name} está fora da grade em {position}.");
            return;
        }

        transform.position = GridManager.Instance.GridToWorld(position);

        GridCell cell = GridManager.Instance.GetCell(position);
        if (cell != null)
        {
            cell.isWalkable = false;
            cell.isOccupied = true;
            cell.occupant = this;
        }
    }

    private void OnDestroy()
    {
        if (GridManager.Instance == null) return;

        GridCell cell = GridManager.Instance.GetCell(gridPosition);
        if (cell != null && ReferenceEquals(cell.occupant, this))
        {
            cell.isWalkable = true;
            cell.isOccupied = false;
            cell.occupant = null;
        }
    }
}

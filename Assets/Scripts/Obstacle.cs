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
        transform.position = GridManager.Instance.GridToWorld(position);

        GridCell cell = GridManager.Instance.GetCell(position);
        if (cell != null)
        {
            cell.isWalkable = false;
            cell.isOccupied = true;
            cell.occupant = this;
        }
    }
}

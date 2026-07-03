using UnityEngine;

[DefaultExecutionOrder(-200)]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;
    public Vector3 origin = new(-5f, 0f, -5f);
    private GridCell[,] grid;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateGrid();
    }

    private void CreateGrid()
    {
        grid = new GridCell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new GridCell(x, y);
            }
        }
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.z - origin.z) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        float worldX = origin.x + gridPosition.x * cellSize + cellSize * 0.5f;
        float worldZ = origin.z + gridPosition.y * cellSize + cellSize * 0.5f;
        return new Vector3(worldX, origin.y, worldZ);
    }

    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < width
            && gridPosition.y >= 0 && gridPosition.y < height;
    }

    public GridCell GetCell(Vector2Int gridPosition)
    {
        if (!IsValidGridPosition(gridPosition)) return null;
        return grid[gridPosition.x, gridPosition.y];
    }

    public void SetTerrain(Vector2Int gridPosition, TerrainType terrainType)
    {
        GridCell cell = GetCell(gridPosition);
        if (cell != null)
        {
            cell.terrainType = terrainType;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int x = 0; x <= width; x++)
        {
            Vector3 from = origin + new Vector3(x * cellSize, 0.01f, 0);
            Vector3  to = origin + new Vector3(x * cellSize, 0.01f, height * cellSize);
            Gizmos.DrawLine(from, to);
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 from = origin + new Vector3(0, 0.01f, y * cellSize);
            Vector3 to = origin + new Vector3(width * cellSize, 0.01f, y * cellSize);
            Gizmos.DrawLine(from, to);
        }
    }
}

public enum TerrainType
{
    Dirt,
    Grass,
    Water
}

public class GridCell
{
    public int x;
    public int y;
    public bool isWalkable = true;
    public bool isOccupied = false;
    public TerrainType terrainType = TerrainType.Dirt;

    public object occupant = null;

    public int MovementCost => terrainType == TerrainType.Water ? 2 : 1;
    public bool HidesFromZombies => terrainType == TerrainType.Grass;

    public GridCell(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

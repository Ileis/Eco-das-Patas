using UnityEngine;

public class TerrainTile : MonoBehaviour
{
    public Vector2Int gridPosition;
    public TerrainType terrainType = TerrainType.Dirt;

    private void Start()
    {
        if (GridManager.Instance == null) return;

        transform.position = GridManager.Instance.GridToWorld(gridPosition)
            + Vector3.down * 0.035f;
        GridManager.Instance.SetTerrain(gridPosition, terrainType);
    }
}

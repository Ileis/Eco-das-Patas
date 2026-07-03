using UnityEngine;
using UnityEngine.Events;

public class GridInputHandler : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask boardLayer;
    public GameObject highlightObject;
    public UnityEvent<Vector2Int> OnCellClicked;
    public UnityEvent<Vector2Int> OnCellHovered;
    
    private Vector2Int lastHoveredCell = new(-1, -1);

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (highlightObject != null) highlightObject.SetActive(false);
    }

    void Update()
    {
        HandleHover();

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private bool TryGetGridCellUnderMouse(out Vector2Int gridPos)
    {
        gridPos = default;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, boardLayer))
        {
            gridPos = GridManager.Instance.WorldToGrid(hit.point);
            return GridManager.Instance.IsValidGridPosition(gridPos);
        }
        return false;
    }

    private void HandleHover()
    {
        if (TryGetGridCellUnderMouse(out Vector2Int gridPos))
        {
            if (gridPos != lastHoveredCell)
            {
                lastHoveredCell = gridPos;

                if (highlightObject != null)
                {
                    highlightObject.SetActive(true);
                    highlightObject.transform.position = GridManager.Instance.GridToWorld(gridPos) + UnityEngine.Vector3.up * 0.03f;
                }

                OnCellHovered?.Invoke(gridPos);
            }
        }
        else
        {
            lastHoveredCell = new Vector2Int(-1, -1);
            if (highlightObject != null) highlightObject.SetActive(false);
        }
    }

    private void HandleClick()
    {
        if (TryGetGridCellUnderMouse(out Vector2Int gridPos))
        {
            OnCellClicked?.Invoke(gridPos);
            Debug.Log($"Célula clicada: {gridPos}");
        }
    }
}

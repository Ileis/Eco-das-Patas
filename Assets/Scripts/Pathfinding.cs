using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    public static Dictionary<Vector2Int, int> GetReachableCells(
        Vector2Int origin,
        int range,
        out Dictionary<Vector2Int, Vector2Int> cameFrom
    )
    {
        var costSoFar = new Dictionary<Vector2Int, int>();
        cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var frontier = new Queue<Vector2Int>();

        costSoFar[origin] = 0;
        frontier.Enqueue(origin);

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            int currentCost = costSoFar[current];

            if (currentCost >= range) continue;

            foreach(Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (!GridManager.Instance.IsValidGridPosition(next)) continue;

                GridCell cell = GridManager.Instance.GetCell(next);
                if (!cell.isWalkable) continue;
                if (cell.isOccupied) continue;

                int newCost = currentCost + 1;
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }
        }

        costSoFar.Remove(origin);
        return costSoFar;
    }

    public static List<Vector2Int> ReconstructPath(
        Vector2Int origin,
        Vector2Int destination,
        Dictionary<Vector2Int, Vector2Int> cameFrom
    )
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = destination;
        
        while (current != origin)
        {
            path.Add(current);
            if (!cameFrom.TryGetValue(current, out current))
            {
                return new List<Vector2Int>();
            }
        }

        path.Reverse();
        return path;
    }
}
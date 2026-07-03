using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyCaiting : Enemy
{
    protected override void Move(Vector2Int targetGridPosition)
    {
        Dictionary<Vector2Int, int> reachable = Pathfinding.GetReachableCells(GridPosition, moveRange, out _);

        if (ManhattanDistance(GridPosition, targetGridPosition) < Ability.range || ManhattanDistance(GridPosition, targetGridPosition) < (moveRange + Ability.range))
        {
            List<Vector2Int> bestCells = new();
            int bestDistance = Ability.range;

            foreach (Vector2Int cell in reachable.Keys)
            {
                int dist = ManhattanDistance(cell, targetGridPosition);
                if (dist == bestDistance)
                {
                    bestCells.Add(cell);
                }
            }
            if (bestCells.Count <= 0) return;
            Vector2Int selectedCell = bestCells[Random.Range(0, bestCells.Count - 1)];
            Debug.Log($"Distancia: {ManhattanDistance(selectedCell, targetGridPosition)}");

            MoveTo(selectedCell);
        }
        else
        {
            base.Move(targetGridPosition);
        }
    }
}
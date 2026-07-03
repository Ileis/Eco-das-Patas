using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyBlind : Enemy
{
    protected override void Move(Vector2Int targetGridPosition)
    {
        Dictionary<Vector2Int, int> reachable = Pathfinding.GetReachableCells(GridPosition, moveRange, out _);
        MoveTo(reachable.Keys.ToArray()[Random.Range(0, reachable.Keys.Count - 1)]);
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : Unit
{
    public Ability meleeAbility;

    public void TakeAITurn()
    {
        StartCoroutine(AIRoutine());
    }

    private IEnumerator AIRoutine()
    {
        yield return new WaitForSeconds(0.4f);

        Unit player = FindPlayerUnit();

        if (player != null && meleeAbility != null)
        {
            int distance = ManhattanDistance(GridPosition, player.GridPosition);

            if (distance <= meleeAbility.range && CanUseAbility(meleeAbility))
            {
                TryUseAbility(meleeAbility, player);
                yield return new WaitForSeconds(0.4f);
            }
            else if (!HasMovedThisTurn)
            {
                MoveTowards(player.GridPosition);
                yield return new WaitForSeconds(0.4f);

                distance = ManhattanDistance(GridPosition, player.GridPosition);
                if (distance <= meleeAbility.range && CanUseAbility(meleeAbility))
                {
                    TryUseAbility(meleeAbility, player);
                    yield return new WaitForSeconds(0.4f);
                }
            }
        }

        if (TurnManager.Instance != null) TurnManager.Instance.NextTurn();
    }

    private void MoveTowards(Vector2Int targetGridPosition)
    {
        Dictionary<Vector2Int, int> reachable = Pathfinding.GetReachableCells(GridPosition, moveRange, out _);

        Vector2Int bestCell = GridPosition;
        int bestDistance = ManhattanDistance(GridPosition, targetGridPosition);

        foreach (Vector2Int cell in reachable.Keys)
        {
            int dist = ManhattanDistance(cell, targetGridPosition);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestCell = cell;
            }
        }

        if (bestCell != GridPosition)
        {
            MoveTo(bestCell);
        }
    }

    private Unit FindPlayerUnit()
    {
        Unit[] allUnits = FindObjectsByType<Unit>();
        foreach (Unit unit in allUnits)
        {
            if (!(unit is Enemy) && !unit.IsDead) return unit;
        }
        return null;
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x -b.x) + Mathf.Abs(a.y - b.y);
    }
}
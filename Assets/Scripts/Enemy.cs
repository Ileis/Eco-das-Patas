using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Unit
{
    public Ability Ability;

    public void TakeAITurn()
    {
        StartCoroutine(AIRoutine());
    }

    private IEnumerator AIRoutine()
    {
        yield return new WaitForSeconds(0.4f);

        Unit player = FindPlayerUnit();

        if (player != null && player.IsHiddenFromZombies)
        {
            Debug.Log($"{name} perdeu o rastro de {player.name} no mato.");
            if (TurnManager.Instance != null) TurnManager.Instance.NextTurn();
            yield break;
        }

        if (player != null && Ability != null)
        {
            int distance = ManhattanDistance(GridPosition, player.GridPosition);

            // if (distance <= Ability.range && CanUseAbility(Ability))
            // {
            //     TryUseAbility(Ability, player);
            //     yield return new WaitForSeconds(0.4f);
            // }
            // else
            if (!HasMovedThisTurn)
            {
                Move(player.GridPosition);
                yield return new WaitUntil(() => !IsMoving);
                yield return new WaitForSeconds(0.2f); // Pequeno atraso após chegar antes de tentar atacar

                distance = ManhattanDistance(GridPosition, player.GridPosition);
                if (distance <= Ability.range && CanUseAbility(Ability))
                {
                    TryUseAbility(Ability, player);
                    yield return new WaitForSeconds(0.4f);
                }
            }
        }

        if (TurnManager.Instance != null) TurnManager.Instance.NextTurn();
    }

    protected virtual void Move(Vector2Int targetGridPosition)
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
            if (unit is not Enemy && !unit.IsDead) return unit;
        }
        return null;
    }

    protected int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x -b.x) + Mathf.Abs(a.y - b.y);
    }
}

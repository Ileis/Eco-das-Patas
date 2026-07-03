using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Vector2Int startPosition;
    public int moveRange = 3;
    public Vector2Int GridPosition { get; private set; }

    public int maxHealth = 10;
    public int maxActionPoints = 3;
    public int initiative = 10;

    public int CurrentHealth; // { get; private set; }
    public int CurrentActionPoints; // { get; private set; }
    public bool IsDead { get; private set; }
    public bool HasMovedThisTurn { get; private set; }

    private readonly Dictionary<Ability, int> usesThisTurn = new();

    protected virtual void Start()
    {
        PlaceOnGrid(startPosition);
        CurrentHealth = maxHealth;
        CurrentActionPoints = maxActionPoints;
    }

    public void PlaceOnGrid(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
        transform.position = GridManager.Instance.GridToWorld(gridPosition);

        GridCell cell = GridManager.Instance.GetCell(gridPosition);
        if (cell != null)
        {
            cell.isOccupied = true;
            cell.occupant = this;
        }
    }

    public bool MoveTo(Vector2Int newGridPosition)
    {
        if (HasMovedThisTurn) return false;

        GridCell oldCell = GridManager.Instance.GetCell(GridPosition);
        if (oldCell != null)
        {
            oldCell.isOccupied = false;
            oldCell.occupant = null;
        }

        GridPosition = newGridPosition;
        transform.position = GridManager.Instance.GridToWorld(newGridPosition);

        GridCell newCell = GridManager.Instance.GetCell(newGridPosition);
        if (newCell != null)
        {
            newCell.isOccupied = true;
            newCell.occupant = this;
        }

        HasMovedThisTurn = true;
        return true;
    }

    public virtual void StartTurn()
    {
        CurrentActionPoints = maxActionPoints;
        HasMovedThisTurn = false;
        usesThisTurn.Clear();
    }

    public bool CanUseAbility(Ability ability)
    {
        if (ability == null || IsDead) return false;
        if (CurrentActionPoints < ability.actionPoinCost) return false;

        usesThisTurn.TryGetValue(ability, out int uses);
        if (uses >= ability.maxUsesPerTurn) return false;

        return true;
    }

    public bool TryUseAbility(Ability ability, Unit target)
    {
        if (!CanUseAbility(ability) || target == null) return false;

        int distance = Mathf.Abs(target.GridPosition.x - GridPosition.x)
                     + Mathf.Abs(target.GridPosition.y - GridPosition.y);
        
        if (distance > ability.range) return false;

        ApplyAbilityEffect(ability, target);

        CurrentActionPoints -= ability.actionPoinCost;
        usesThisTurn.TryGetValue(ability, out int uses);
        usesThisTurn[ability] = uses + 1;

        return true;
    }

    private void ApplyAbilityEffect(Ability ability, Unit target)
    {
        switch (ability.type)
        {
            case AbilityType.Melee:
            case AbilityType.Ranged:
                target.TakeDamage(ability.power);
                Debug.Log($"{name} usou {ability.abilityName} em {target.name}, causando ${ability.power} de dano.");
                break;

            case AbilityType.Taunt:
                Debug.Log($"{name} provocou {target.name} com {ability.abilityName}.");
                break;

            case AbilityType.Buff:
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + ability.power);
                Debug.Log($"{name} usou ${ability.abilityName} e recuperou {ability.power} de vida.");
                break;
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        Debug.Log($"{name} tomou {amount} de dano ({CurrentHealth}/{maxHealth} de vida restante).");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        IsDead = true;

        GridCell cell = GridManager.Instance.GetCell(GridPosition);
        if (cell != null)
        {
            cell.isOccupied = false;
            cell.occupant = null;
        }

        if (TurnManager.Instance != null) TurnManager.Instance.RemoveUnit(this);

        Debug.Log($"{name} morreu.");
        Destroy(gameObject);
    }
}

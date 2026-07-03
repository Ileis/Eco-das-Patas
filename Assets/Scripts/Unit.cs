using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Vector2Int startPosition;
    public int moveRange = 3;
    [Header("Orientação visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualYawOffset;
    public Vector2Int GridPosition { get; private set; }

    public int maxHealth = 10;
    public int maxActionPoints = 3;
    public int initiative = 10;

    public int CurrentHealth; // { get; private set; }
    public int CurrentActionPoints; // { get; private set; }
    public bool IsDead { get; private set; }
    public bool HasMovedThisTurn { get; private set; }
    public TerrainType CurrentTerrain
    {
        get
        {
            GridCell cell = GridManager.Instance != null
                ? GridManager.Instance.GetCell(GridPosition)
                : null;
            return cell != null ? cell.terrainType : TerrainType.Dirt;
        }
    }
    public bool IsHiddenFromZombies => CurrentTerrain == TerrainType.Grass;

    private readonly Dictionary<Ability, int> usesThisTurn = new();

    protected virtual void Start()
    {
        PlaceOnGrid(startPosition);
        CurrentHealth = maxHealth;
        CurrentActionPoints = maxActionPoints;
        CacheVisualRoot();
        FaceNearestEnemy();
    }

    protected virtual void LateUpdate()
    {
        if (!(this is Enemy))
        {
            FaceNearestEnemy();
        }
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

        Vector3 previousPosition = transform.position;
        GridCell oldCell = GridManager.Instance.GetCell(GridPosition);
        if (oldCell != null)
        {
            oldCell.isOccupied = false;
            oldCell.occupant = null;
        }

        GridPosition = newGridPosition;
        transform.position = GridManager.Instance.GridToWorld(newGridPosition);

        if (this is Enemy)
        {
            FaceDirection(transform.position - previousPosition);
        }

        GridCell newCell = GridManager.Instance.GetCell(newGridPosition);
        if (newCell != null)
        {
            newCell.isOccupied = true;
            newCell.occupant = this;
        }

        HasMovedThisTurn = true;
        if (newCell != null && newCell.terrainType == TerrainType.Water)
        {
            Debug.Log($"{name} entrou na água: cada célula de água custa 2 pontos de movimento.");
        }
        return true;
    }

    private void CacheVisualRoot()
    {
        if (visualRoot != null) return;

        Transform namedVisual = transform.Find(this is Enemy ? "VisualKenney" : "VisualGato");
        if (namedVisual != null)
        {
            visualRoot = namedVisual;
            return;
        }

        Animator animator = GetComponentInChildren<Animator>(true);
        visualRoot = animator != null ? animator.transform : transform;
    }

    private void FaceNearestEnemy()
    {
        if (IsDead) return;

        Enemy[] enemies = FindObjectsByType<Enemy>();
        Enemy nearest = null;
        float nearestSqrDistance = float.PositiveInfinity;

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = enemy;
            }
        }

        if (nearest != null)
        {
            FaceDirection(nearest.transform.position - transform.position);
        }
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        CacheVisualRoot();
        visualRoot.rotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up)
            * Quaternion.Euler(0f, visualYawOffset, 0f);
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

    public bool TryUseAbility(Ability ability, object target)
    {
        Debug.Log(target);
        if (!CanUseAbility(ability)) return false;

        if (target is Unit unit)
        {

            int distance = Mathf.Abs(unit.GridPosition.x - GridPosition.x)
                        + Mathf.Abs(unit.GridPosition.y - GridPosition.y);
            
            if (distance > ability.range) return false;

            ApplyAbilityEffect(ability, unit);
        }

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
                break;

            case AbilityType.Taunt:
                break;

            case AbilityType.Buff:
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + ability.power);
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

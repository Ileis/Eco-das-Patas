using System.Collections;
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
    public bool IsMoving { get; private set; }
    public event System.Action OnMovementStarted;
    public event System.Action OnMovementFinished;
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
        if (!(this is Enemy) && !IsMoving)
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
        if (HasMovedThisTurn || IsMoving) return false;

        GridCell oldCell = GridManager.Instance.GetCell(GridPosition);
        GridCell newCell = GridManager.Instance.GetCell(newGridPosition);
        if (newCell == null || newCell.isOccupied || !newCell.isWalkable) return false;

        // Reconstrói a rota física com busca em largura a partir da posição atual
        var reachable = Pathfinding.GetReachableCells(GridPosition, moveRange, out var cameFrom);
        if (!reachable.ContainsKey(newGridPosition)) return false;

        List<Vector2Int> path = Pathfinding.ReconstructPath(GridPosition, newGridPosition, cameFrom);
        if (path == null || path.Count == 0) return false;

        // Atualização instantânea do estado lógico da grade para outros sistemas
        if (oldCell != null)
        {
            oldCell.isOccupied = false;
            oldCell.occupant = null;
        }

        GridPosition = newGridPosition;
        newCell.isOccupied = true;
        newCell.occupant = this;

        HasMovedThisTurn = true;

        if (newCell.terrainType == TerrainType.Water)
        {
            Debug.Log($"{name} entrou na água: cada célula de água custa 2 pontos de movimento.");
        }

        // Inicia o deslocamento visual suave
        StartCoroutine(AnimateMovement(path));
        return true;
    }

    private IEnumerator AnimateMovement(List<Vector2Int> path)
    {
        IsMoving = true;
        OnMovementStarted?.Invoke();

        Animator anim = GetComponentInChildren<Animator>(true);
        bool hasWalk = anim != null && anim.HasState(0, Animator.StringToHash("Walk"));
        bool hasIdle = anim != null && anim.HasState(0, Animator.StringToHash("Idle"));

        if (hasWalk)
        {
            anim.Play("Walk");
        }

        float speed = 13f; // Velocidade de movimento (células por segundo)

        foreach (Vector2Int cellPos in path)
        {
            Vector3 targetWorldPos = GridManager.Instance.GridToWorld(cellPos);
            
            // Rotaciona visualmente para a direção do movimento
            FaceDirection(targetWorldPos - transform.position);

            while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetWorldPos;
        }

        if (hasIdle)
        {
            anim.Play("Idle");
        }

        IsMoving = false;
        OnMovementFinished?.Invoke();
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

            // Rotaciona visualmente para encarar o alvo
            FaceDirection(unit.transform.position - transform.position);

            // Dispara a animação correspondente se disponível
            Animator anim = GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                if (this is Enemy)
                {
                    if (anim.HasState(0, Animator.StringToHash("attack-melee-right")))
                    {
                        anim.Play("attack-melee-right");
                    }
                }
                else
                {
                    // Habilidades do gato mapeadas para as animações do fbx
                    string abilityId = $"{ability.name} {ability.abilityName}";
                    bool usesJump = abilityId.Contains("Patada")
                        || abilityId.Contains("Cuspir")
                        || abilityId.Contains("Cuspe");

                    if (usesJump)
                    {
                        if (anim.HasState(0, Animator.StringToHash("Jump")))
                        {
                            StartCoroutine(PlayAnimationWithSpeed(anim, "Jump", 1.3f));
                        }
                    }
                    else if (abilityId.Contains("Ronronar"))
                    {
                        if (anim.HasState(0, Animator.StringToHash("Eat"))) anim.Play("Eat");
                    }
                    else if (abilityId.Contains("Miar"))
                    {
                        if (anim.HasState(0, Animator.StringToHash("sound"))) anim.Play("sound");
                    }
                }
            }

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

    private IEnumerator PlayAnimationWithSpeed(Animator anim, string stateName, float speedMultiplier)
    {
        if (anim == null) yield break;

        float originalSpeed = anim.speed;
        anim.speed = speedMultiplier;
        anim.Play(stateName);

        yield return null;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(stateName))
        {
            yield return new WaitForSeconds(stateInfo.length / speedMultiplier);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (anim != null)
        {
            anim.speed = originalSpeed;
        }
    }
}

using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    public Unit unit;

    public Ability patada;
    public Ability miar;
    public Ability cuspir;
    public Ability ronronar;

    public void UsePatada() => UseOnNearestEnemy(patada);
    public void UseMiar() => UseOnNearestEnemy(miar);
    public void UseCuspir() => UseOnNearestEnemy(cuspir);

    public void UseRonronar()
    {
        if (unit == null || ronronar == null) return;

        bool sucess = unit.TryUseAbility(ronronar, unit);
        if (!sucess) Debug.Log($"Não foi possível usar {ronronar.abilityName}.");
    }

    private void UseOnNearestEnemy(Ability ability)
    {
        if (unit == null || ability == null) return;

        Enemy target = FindNearestEnemyInRange(ability.range);
        if (target == null)
        {
            Debug.Log($"Nenhum inimigo no alcance de {ability.abilityName}.");
            return;
        }

        bool success = unit.TryUseAbility(ability, target);
        if (!success)
        {
            Debug.Log($"Não foi possível usar ${ability.abilityName} (pontos de ação insuficientes ou usos esgotados neste turno)");
        }
    }

    private Enemy FindNearestEnemyInRange(int range)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>();
        Enemy closest = null;
        int closestDistance = int.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.IsDead) continue;

            int distance = Mathf.Abs(enemy.GridPosition.x - unit.GridPosition.x)
                         + Mathf.Abs(enemy.GridPosition.y - unit.GridPosition.y);

            if (distance <= range && distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }
}
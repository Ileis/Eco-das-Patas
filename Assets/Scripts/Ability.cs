using UnityEngine;

public enum AbilityType
{
    Melee,
    Taunt,
    Ranged,
    Buff
}

[CreateAssetMenu(fileName = "NewAbility", menuName = "Tactics/Ability")]
public class Ability : ScriptableObject
{
    public string abilityName = "Nova Habilidade";
    public AbilityType type;

    public int actionPoinCost = 1;
    public int maxUsesPerTurn = 2;

    public int range = 1;

    public int power = 2;

    public int buffDurationTurns = 2;
}
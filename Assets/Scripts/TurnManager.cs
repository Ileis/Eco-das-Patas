using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private List<Unit> turnOrder = new List<Unit>();
    private int currentIndex = -1;

    public Unit CurrentUnit =>
        (currentIndex >= 0 && currentIndex < turnOrder.Count) ? turnOrder[currentIndex] : null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Unit[] unitsInScene = FindObjectsByType<Unit>();
        turnOrder = unitsInScene.OrderByDescending(u => u.initiative).ToList();

        NextTurn();
    }

    public void RemoveUnit(Unit unit)
    {
        int removedIndex = turnOrder.IndexOf(unit);
        turnOrder.Remove(unit);

        if (removedIndex != -1 && removedIndex < currentIndex)
        {
            currentIndex--;
        }
    }

    public void NextTurn()
    {
        if (turnOrder.Count == 0) return;

        currentIndex++;
        if (currentIndex >= turnOrder.Count)
        {
            currentIndex = 0;
        }

        CurrentUnit.StartTurn();
        Debug.Log($"Turno de ${CurrentUnit.name} (iniciativa {CurrentUnit.initiative})");

        
        if (CurrentUnit is Enemy enemy)
        {
            enemy.TakeAITurn();
        }
                
    }

    public void EndTurnButtonPressed()
    {
        
        if (CurrentUnit != null && !(CurrentUnit is Enemy))
        {
            NextTurn();
        }
        
    }
}
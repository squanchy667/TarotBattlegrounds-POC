using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Ensure this is present

public static class CombatManager
{
    public static int SimulateBattle(List<Card> pBoard, List<Card> aBoard, int tavernTier)
    {
        Debug.Log($"CombatManager: Simulating battle with P={pBoard.Count}, A={aBoard.Count}, Tavern Tier={tavernTier}"); // Confirm entry

        // Connect fixed names to boards and randomly choose first attacker
        string pName = "ofek"; // Fixed name for pBoard
        string aName = "jaya"; // Fixed name for aBoard
        bool FirstToAttack = Random.value > 0.5f;
        string firstPlayer = FirstToAttack ? pName : aName;
        string secondPlayer = FirstToAttack ? aName : pName;
        Debug.Log($"Entering attack phase: {firstPlayer} attacks first, {pName}={pBoard.Count}, {aName}={aBoard.Count}"); // Debug entry with fixed names

        // Alternate attacks between ofek and jaya
        List<(List<Card> attackers, List<Card> targetBoard, bool isOfek)> sides = new List<(List<Card>, List<Card>, bool)>();
        sides.Add((pBoard.ToList(), aBoard.ToList(), true));  // ofek (copy of pBoard)
        sides.Add((aBoard.ToList(), pBoard.ToList(), false)); // jaya (copy of aBoard)

        int currentSide = FirstToAttack ? 0 : 1;
        int turnCount = 0;
        bool hasValidAttack = true;
        while (hasValidAttack)
        {
            hasValidAttack = false;
            var (attackers, targetBoard, isOfek) = sides[currentSide];
            var attacker = attackers.FirstOrDefault(c => c.health > 0);
            Debug.Log($"Turn {turnCount}: {(isOfek ? pName : aName)}'s turn"); // Use pName or aName based on isOfek
            if (attacker != null)
            {
                var aliveTargets = targetBoard.Where(c => c.health > 0).ToList();
                if (aliveTargets.Count > 0)
                {
                    hasValidAttack = true;
                    int targetIdx = Random.Range(0, aliveTargets.Count);
                    Card target = aliveTargets[targetIdx]; // Random target
                    string attackerPlayer = isOfek ? pName : aName;
                    string targetPlayer = isOfek ? aName : pName;
                    Debug.Log($"Run Attack - {attacker.cardName} ({attackerPlayer}) targets {target.cardName} ({targetPlayer}), damage {attacker.attack}");
                    target.health -= attacker.attack;
                    Debug.Log($"Counterattack - {target.cardName} ({targetPlayer}) Counterattacks {attacker.cardName} ({attackerPlayer}), damage {target.attack}");
                    attacker.health -= target.attack;
                    Debug.Log($"{target.name} ({targetPlayer}) {target.cardName} health now {target.health}");
                    Debug.Log($"{attacker.name} ({attackerPlayer}) {attacker.cardName} health now {attacker.health}");
                    if (target.health <= 0)
                    {
                        targetBoard.Remove(target);
                        Debug.Log($"{target.cardName} removed from {targetPlayer} board");
                    }
                    if (attacker.health <= 0)
                    {
                        attackers.Remove(attacker);
                        Debug.Log($"{attacker.cardName} removed from {attackerPlayer} board");
                    }
                }
                else
                {
                    string attackerPlayer = isOfek ? pName : aName;
                    Debug.Log($"No valid targets for {attacker.cardName} {attackerPlayer}");
                }
            }
            currentSide = 1 - currentSide; // Switch sides
            turnCount++;
            // Check if both sides have no valid attackers or targets
            if (!sides[0].attackers.Any(c => c.health > 0) || !sides[1].attackers.Any(c => c.health > 0))
            {
                hasValidAttack = false;
            }
        }

        // Remove any remaining dead minions
        Debug.Log($"Post-removal state: P={pBoard.Count}, A={aBoard.Count}"); // Debug post-removal
        pBoard.RemoveAll(c => c.health <= 0);
        aBoard.RemoveAll(c => c.health <= 0);
        Debug.Log($"Determining outcome: P={pBoard.Count}, A={aBoard.Count}"); // Debug before outcome

        // Determine winner/tie after resolution
        string outcome = "";
        if (pBoard.Count == 0 && aBoard.Count == 0)
        {
            outcome = "Both boards empty, Tie";
        }
        else if (aBoard.Count == 0)
        {
            outcome = $"{pName} wins";
        }
        else if (pBoard.Count == 0)
        {
            outcome = $"{aName} wins";
        }
        Debug.Log($"Outcome determined: {outcome}"); // Debug after outcome

        // Damage calculation based on final state
        int survivingTier = (pBoard.Count > 0 ? pBoard.Sum(c => c.tier) : aBoard.Count > 0 ? aBoard.Sum(c => c.tier) : 0) + tavernTier;
        int damage = Mathf.Min(5, survivingTier);
        if (pBoard.Count == 0 && aBoard.Count == 0)
        {
            survivingTier = 0;
            damage = 0;
            Debug.Log("Tie confirmed, surviving tier and damage set to 0");
        }
        Debug.Log("Battle outcome: Surviving tier sum = " + survivingTier + ", Damage = " + damage);
        return damage;
    }
}
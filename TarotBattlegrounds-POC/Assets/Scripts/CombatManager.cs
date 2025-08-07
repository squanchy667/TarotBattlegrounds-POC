using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class CombatManager
{
    public static int SimulateBattle(List<Card> pBoard, List<Card> aBoard, int tavernTier, string pName, string aName)
    {
        Debug.Log($"CombatManager: Simulating battle with {pName}={pBoard.Count}, {aName}={aBoard.Count}, Tavern Tier={tavernTier}");
        List<Card> pBoardCopy = pBoard.Select(c => c.Clone()).ToList();
        List<Card> aBoardCopy = aBoard.Select(c => c.Clone()).ToList();
        bool pFirst = Random.value > 0.5f;
        string firstPlayer = pFirst ? pName : aName;
        string secondPlayer = pFirst ? aName : pName;
        Debug.Log($"Entering attack phase: {firstPlayer} attacks first, {pName}={pBoardCopy.Count}, {aName}={aBoardCopy.Count}");

        List<(List<Card> attackers, List<Card> targetBoard, bool isP, string attackerName, string targetName)> sides = new List<(List<Card>, List<Card>, bool, string, string)>
        {
            (pBoardCopy, aBoardCopy, true, pName, aName),
            (aBoardCopy, pBoardCopy, false, aName, pName)
        };
        int currentSide = pFirst ? 0 : 1;
        int turnCount = 1;
        bool hasValidAttack = true;

        while (hasValidAttack)
        {
            hasValidAttack = false;
            var (attackers, targetBoard, isP, attackerName, targetName) = sides[currentSide];
            var attacker = attackers.FirstOrDefault(c => c.health > 0);
            Debug.Log($"Turn {turnCount}: {attackerName}'s turn");
            if (attacker != null)
            {
                var aliveTargets = targetBoard.Where(c => c.health > 0).ToList();
                var guardianTarget = aliveTargets.FirstOrDefault(c => c.effectType == Card.EffectType.Guardian);
                Card target = guardianTarget ?? aliveTargets.OrderBy(x => Random.value).FirstOrDefault();
                if (target != null)
                {
                    hasValidAttack = true;
                    Debug.Log($"Run Attack - {attacker.cardName} ({attackerName}) targets {target.cardName} ({targetName}), damage {attacker.attack}");
                    if (target.hasAegis)
                    {
                        Debug.Log($"Aegis: {target.cardName} ({targetName}) blocks attack");
                        target.hasAegis = false;
                    }
                    else
                    {
                        target.health -= attacker.attack;
                        Debug.Log($"{target.cardName} ({targetName}) health now {target.health}");
                        if (target.health <= 0)
                        {
                            if (target.effectType == Card.EffectType.Echo)
                            {
                                Card ally = targetBoard.Where(c => c != target && c.health > 0).OrderBy(x => Random.value).FirstOrDefault();
                                if (ally != null)
                                {
                                    string[] param = target.effectParameter.Split(':');
                                    int value = param.Length > 1 && int.TryParse(param[1], out int v) ? v : 0;
                                    ally.attack += value;
                                    Debug.Log($"Echo: {target.cardName} ({targetName}) buffs {ally.cardName} attack by {value} (New Attack: {ally.attack})");
                                }
                            }
                            targetBoard.Remove(target);
                            Debug.Log($"{target.cardName} removed from {targetName} board");
                        }
                    }
                    Debug.Log($"Counterattack - {target.cardName} ({targetName}) counterattacks {attacker.cardName} ({attackerName}), damage {target.attack}");
                    if (attacker.hasAegis)
                    {
                        Debug.Log($"Aegis: {attacker.cardName} ({attackerName}) blocks counterattack");
                        attacker.hasAegis = false;
                    }
                    else
                    {
                        attacker.health -= target.attack;
                        Debug.Log($"{attacker.cardName} ({attackerName}) health now {attacker.health}");
                        if (attacker.health <= 0)
                        {
                            if (attacker.effectType == Card.EffectType.Echo)
                            {
                                Card ally = attackers.Where(c => c != attacker && c.health > 0).OrderBy(x => Random.value).FirstOrDefault();
                                if (ally != null)
                                {
                                    string[] param = attacker.effectParameter.Split(':');
                                    int value = param.Length > 1 && int.TryParse(param[1], out int v) ? v : 0;
                                    ally.attack += value;
                                    Debug.Log($"Echo: {attacker.cardName} ({attackerName}) buffs {ally.cardName} attack by {value} (New Attack: {ally.attack})");
                                }
                            }
                            attackers.Remove(attacker);
                            Debug.Log($"{attacker.cardName} removed from {attackerName} board");
                        }
                    }
                }
                else
                {
                    Debug.Log($"No valid targets for {attacker.cardName} ({attackerName})");
                }
            }
            currentSide = 1 - currentSide;
            turnCount++;
            if (!sides[0].attackers.Any(c => c.health > 0) || !sides[1].attackers.Any(c => c.health > 0))
            {
                hasValidAttack = false;
            }
        }

        Debug.Log($"Post-removal state: {pName}={pBoardCopy.Count}, {aName}={aBoardCopy.Count}");
        int survivingTier = (pBoardCopy.Count > 0 ? pBoardCopy.Sum(c => c.tier) : aBoardCopy.Count > 0 ? aBoardCopy.Sum(c => c.tier) : 0) + tavernTier;
        int damage = pBoardCopy.Count == 0 && aBoardCopy.Count == 0 ? 0 : Mathf.Min(5, survivingTier);
        string outcome = pBoardCopy.Count == 0 && aBoardCopy.Count == 0 ? "Both boards empty, Tie" :
                        pBoardCopy.Count > 0 ? $"{pName} wins" : $"{aName} wins";
        Debug.Log($"Outcome: {outcome}, Surviving tier sum = {survivingTier}, Damage = {damage}");
        return damage;
    }
}
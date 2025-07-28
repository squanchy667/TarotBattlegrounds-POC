using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CombatManager : MonoBehaviour
{
    public TavernManager tavern;  // Add reference (assign in Inspector or via GameManager)

    public int SimulateBattle(List<Card> playerBoard, List<Card> aiBoard, int tavernTier)
    {
        // Copy boards
        List<Card> pBoard = playerBoard.Select(c => c.Clone()).ToList();
        List<Card> aBoard = aiBoard.Select(c => c.Clone()).ToList();

        // Battle loop
        while (pBoard.Count > 0 && aBoard.Count > 0)
        {
            Card pAttacker = pBoard[0];
            Card aAttacker = aBoard[0];

            // Player attack with random targeting
            List<Card> aliveEnemies = aBoard.Where(c => c.health > 0).ToList();
            if (aliveEnemies.Count > 0)
            {
                int targetIdx = Random.Range(0, aliveEnemies.Count);
                aliveEnemies[targetIdx].health -= pAttacker.attack;
                // Update original board health
                int originalIdx = aBoard.IndexOf(aliveEnemies[targetIdx]);
                aBoard[originalIdx].health = aliveEnemies[targetIdx].health;
            }

            // AI attack with random targeting
            List<Card> aliveAllies = pBoard.Where(c => c.health > 0).ToList();
            if (aliveAllies.Count > 0)
            {
                int targetIdx = Random.Range(0, aliveAllies.Count);
                aliveAllies[targetIdx].health -= aAttacker.attack;
                // Update original board health
                int originalIdx = pBoard.IndexOf(aliveAllies[targetIdx]);
                pBoard[originalIdx].health = aliveAllies[targetIdx].health;
            }

            // Remove dead
            if (aAttacker.health <= 0) aBoard.RemoveAt(0);
            if (pAttacker.health <= 0) pBoard.RemoveAt(0);

            // Tribe synergy example (Pentacles +coins on kill, player only)
            if (pAttacker.tribe == "Pentacles" && aAttacker.health <= 0)
            {
                tavern.coins += 1;
                Debug.Log("Pentacles synergy: +1 coin on kill.");
            }
        }

        // Damage calculation
        int survivingTier = (pBoard.Count > 0 ? pBoard.Sum(c => c.tier) : aBoard.Sum(c => c.tier)) + tavernTier;
        int damage = Mathf.Min(5, survivingTier);
        Debug.Log("Battle outcome: Surviving tier sum = " + survivingTier + ", Damage = " + damage);
        return damage;
    }
}
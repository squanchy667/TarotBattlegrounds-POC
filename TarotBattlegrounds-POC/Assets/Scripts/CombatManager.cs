using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CombatManager : MonoBehaviour
{
    public TavernManager tavern; // Add reference (assign in Inspector or via GameManager)

    public int SimulateBattle(List<Card> playerBoard, List<Card> aiBoard, int tavernTier)
    {
        // Copy boards to avoid modifying originals
        List<Card> pBoard = playerBoard.Select(c => c.Clone()).ToList();
        List<Card> aBoard = aiBoard.Select(c => c.Clone()).ToList();

        // Single combat cycle for now
        if (pBoard.Count > 0 && aBoard.Count > 0)
        {
            Card pAttacker = pBoard[0];
            Card aAttacker = aBoard[0];
            Dictionary<Card, int> damages = new Dictionary<Card, int>();

            // Queue simultaneous attacks
            List<Card> aliveEnemies = aBoard.Where(c => c.health > 0).ToList();
            if (aliveEnemies.Count > 0)
            {
                int targetIdx = Random.Range(0, aliveEnemies.Count);
                Card target = aliveEnemies[targetIdx];
                Debug.Log($"Player Attack: {pAttacker.cardName} targets {target.cardName}, damage {pAttacker.attack}");
                damages[target] = pAttacker.attack;
            }

            List<Card> aliveAllies = pBoard.Where(c => c.health > 0).ToList();
            if (aliveAllies.Count > 0)
            {
                int targetIdx = Random.Range(0, aliveAllies.Count);
                Card target = aliveAllies[targetIdx];
                Debug.Log($"AI Attack: {aAttacker.cardName} targets {target.cardName}, damage {aAttacker.attack}");
                damages[target] = damages.ContainsKey(target) ? damages[target] + aAttacker.attack : aAttacker.attack;
            }

            // Apply damages simultaneously
            foreach (var pair in damages)
            {
                pair.Key.health -= pair.Value;
            }

            // Remove dead minions
            pBoard.RemoveAll(c => c.health <= 0);
            aBoard.RemoveAll(c => c.health <= 0);

            // Determine winner/tie after resolution
            if (pBoard.Count == 0 && aBoard.Count == 0)
            {
                Debug.Log("Both boards empty, Tie");
            }
            else if (aBoard.Count == 0)
            {
                Debug.Log("AI board empty, Player wins");
            }
            else if (pBoard.Count == 0)
            {
                Debug.Log("Player board empty, AI wins");
            }
        }

        // Damage calculation based on final state
        int survivingTier = (pBoard.Count > 0 ? pBoard.Sum(c => c.tier) : aBoard.Count > 0 ? aBoard.Sum(c => c.tier) : 0) + tavernTier;
        int damage = Mathf.Min(5, survivingTier);
        Debug.Log("Battle outcome: Surviving tier sum = " + survivingTier + ", Damage = " + damage);
        return damage;
    }
}
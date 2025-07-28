using UnityEngine;

public class CardTest : MonoBehaviour
{
    public Card testCard;  // Assign TestCard asset in Inspector

    void Start()
    {
        if (testCard != null)
        {
            Card copy = testCard.Clone();
            Debug.Log($"Original: {testCard.cardName} (Tier {testCard.tier}, Tribe {testCard.tribe})");
            Debug.Log($"Copy: {copy.cardName} (Tier {copy.tier}, Tribe {copy.tribe})");
            // Test synergy placeholder
            Card defender = copy.Clone();
            defender.health = 0;
            copy.OnAttack(defender);
            Debug.Log($"After attack: Copy health = {copy.health}, attack = {copy.attack}");
        }
        else
        {
            Debug.LogWarning("Assign a Card asset to TestCard in Inspector!");
        }
    }
}
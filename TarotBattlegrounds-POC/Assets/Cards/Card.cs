using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Tarot/Card")]  // Allows creating in Unity menu
public class Card : ScriptableObject
{
    public string cardName;  // e.g., "3 of Pentacles"
    public int tier;  // 1-5
    public string tribe;  // "Pentacles", "Cups", etc.
    public int attack;
    public int health;
    public string ability;  // Placeholder text, e.g., "Arcane Barrier: Divine shield equivalent"

    // Later: Add effects as enums or methods
}
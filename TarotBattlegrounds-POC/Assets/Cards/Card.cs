using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Tarot/Card")]
public class Card : ScriptableObject
{
    public string cardName;
    public int tier;
    public string tribe;
    public int attack;
    public int health;
    public string ability;
    public int buyCostModifier = 0;
    public int sellValueModifier = 0;

    public Card Clone()
    {
        Card clone = ScriptableObject.CreateInstance<Card>();
        clone.cardName = this.cardName;
        clone.tier = this.tier;
        clone.tribe = this.tribe;
        clone.attack = this.attack;
        clone.health = this.health;
        clone.ability = this.ability;
        clone.buyCostModifier = this.buyCostModifier;
        clone.sellValueModifier = this.sellValueModifier;
        return clone;
    }

    public virtual void OnAttack(Card defender)
    {
        // Placeholder
    }

    public virtual void OnDeath()
    {
        // Placeholder
    }

    public virtual void OnSurvive()
    {
        // Placeholder
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Tarot/Card")]
public class Card : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;
    public int tier;
    public string tribe;
    
    [Header("Stats")]
    public int attack;
    public int health;
    
    [Header("Visuals")]
    public Sprite cardImage;  // NEW: Card artwork
    
    [Header("Ability")]
    public string ability;
    public enum EffectType { NoEffect, Summoning, LastReading, Guardian, Aegis, Echo }
    public EffectType effectType;
    public string effectParameter;
    
    [Header("Economy")]
    public int buyCostModifier = 0;
    public int sellValueModifier = 0;
    
    [System.NonSerialized] public bool hasAegis;

    public Card Clone()
    {
        Card clone = ScriptableObject.CreateInstance<Card>();
        clone.cardName = this.cardName;
        clone.tier = this.tier;
        clone.tribe = this.tribe;
        clone.attack = this.attack;
        clone.health = this.health;
        clone.cardImage = this.cardImage;  // Copy image reference
        clone.ability = this.ability;
        clone.buyCostModifier = this.buyCostModifier;
        clone.sellValueModifier = this.sellValueModifier;
        clone.effectType = this.effectType;
        clone.effectParameter = this.effectParameter;
        clone.hasAegis = this.hasAegis;
        return clone;
    }

    public virtual void OnAttack(Card defender) { }
    public virtual void OnDeath() { }
    public virtual void OnSurvive() { }
}
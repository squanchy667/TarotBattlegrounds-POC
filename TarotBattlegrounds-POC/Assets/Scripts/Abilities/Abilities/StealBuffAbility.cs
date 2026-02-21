using UnityEngine;

/// <summary>
/// StealBuff ability (T109) - on attack, steal +X/+X from the target.
/// Reduces target's attack and health by X, increases self's attack and health by X.
/// Target stats cannot go below 1 (won't steal more than available).
/// </summary>
public class StealBuffAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.OnAttack;

    private int _value;
    private string _description;

    public override string Description => _description;

    public StealBuffAbility(int value)
    {
        _value = value;
        _description = $"On Attack: Steal +{_value}/+{_value} from target";
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        if (context.TargetCard == null || context.SourceCard == null) return;

        Card target = context.TargetCard;
        Card source = context.SourceCard;

        // Steal attack (don't reduce below 0)
        int stolenAttack = Mathf.Min(_value, target.attack);
        target.attack -= stolenAttack;
        source.attack += stolenAttack;

        // Steal health (don't reduce below 1 to avoid instant death from steal alone)
        int stolenHealth = Mathf.Min(_value, target.health - 1);
        if (stolenHealth > 0)
        {
            target.health -= stolenHealth;
            source.health += stolenHealth;
        }

        Debug.Log($"[StealBuff] {source.cardName} steals +{stolenAttack}/+{stolenHealth} from {target.cardName}");
    }
}

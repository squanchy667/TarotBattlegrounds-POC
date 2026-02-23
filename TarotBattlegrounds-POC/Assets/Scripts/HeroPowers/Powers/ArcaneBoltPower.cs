using UnityEngine;

/// <summary>
/// Wands hero power: Deal 3 damage to a random enemy minion at combat start.
/// Marks the power as used; damage is applied via OnCombatStart.
/// </summary>
public class ArcaneBoltPower : HeroPowerBase
{
    public override string PowerName => "Arcane Bolt";
    public override string Description => "Deal 3 damage to a random enemy minion at combat start";
    public override int CoinCost => 2;

    /// <summary>Whether the bolt is armed for the next combat.</summary>
    private bool armed;

    public override void Activate(Player owner)
    {
        armed = true;
        Debug.Log($"[Arcane Bolt] Armed — will deal 3 damage to a random enemy at combat start");
    }

    public override void OnCombatStart(Player owner, Player opponent)
    {
        if (!armed) return;
        armed = false;

        if (opponent == null || opponent.board.Count == 0)
        {
            Debug.Log("[Arcane Bolt] No enemy minions to target");
            return;
        }

        Card target = opponent.board[Random.Range(0, opponent.board.Count)];
        int oldHealth = target.health;
        target.health -= 3;
        Debug.Log($"[Arcane Bolt] Dealt 3 damage to {target.cardName} ({oldHealth} -> {target.health})");

        if (target.health <= 0)
        {
            opponent.board.Remove(target);
            Debug.Log($"[Arcane Bolt] {target.cardName} destroyed!");
        }
    }
}

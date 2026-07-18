using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Wands hero power: Deal 3 damage to a random enemy minion at combat start.
/// WO-12: Lethal damage routes through the same deathrattle/reborn rules as combat deaths
/// (no bare board.Remove that skips the death pipeline).
/// </summary>
public class ArcaneBoltPower : HeroPowerBase
{
    public override string PowerName => "The Tower";
    public override string Description => "Deal 3 damage to a random enemy minion at combat start";
    public override int CoinCost => 2;

    /// <summary>Whether the bolt is armed for the next combat.</summary>
    private bool armed;

    public override void Activate(Player owner)
    {
        armed = true;
        Debug.Log($"[Arcane Bolt] Armed — will deal 3 damage to a random enemy at combat start");
    }

    /// <summary>Reset armed state along with UsedThisTurn to prevent free bolts across turns.</summary>
    public override void ResetForNewTurn()
    {
        base.ResetForNewTurn();
        armed = false;
    }

    public override void OnCombatStart(Player owner, Player opponent)
    {
        if (!armed) return;
        armed = false;

        if (opponent == null || opponent.board == null || opponent.board.Count == 0)
        {
            Debug.Log("[Arcane Bolt] No enemy minions to target");
            return;
        }

        Card target = opponent.board[Random.Range(0, opponent.board.Count)];
        if (target == null) return;

        // Aegis blocks the bolt (same as combat attacks)
        if (target.hasAegis)
        {
            target.hasAegis = false;
            Debug.Log($"[Arcane Bolt] {target.cardName}'s Aegis blocks the bolt");
            return;
        }

        int oldHealth = target.health;
        target.health -= 3;
        Debug.Log($"[Arcane Bolt] Dealt 3 damage to {target.cardName} ({oldHealth} -> {target.health})");

        if (target.health <= 0)
        {
            ResolveLethalLikeCombat(target, opponent.board, owner != null ? owner.board : null);
        }
    }

    /// <summary>
    /// WO-12: Mirror CombatManager.ProcessDeaths single-card lethal path:
    /// Deathrattle → Reborn (1 HP, strip Reborn) or remove. No bare Remove that skips abilities.
    /// </summary>
    public static void ResolveLethalLikeCombat(Card deadCard, List<Card> ownerBoard, List<Card> enemyBoard)
    {
        if (deadCard == null || ownerBoard == null) return;
        if (!ownerBoard.Contains(deadCard)) return;

        bool willReborn = RebornAbility.HasReborn(deadCard);

        var context = new AbilityContext
        {
            SourceCard = deadCard,
            TargetCard = null,
            OwnerBoard = ownerBoard,
            EnemyBoard = enemyBoard,
            Owner = null
        };
        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, context);

        if (willReborn)
        {
            deadCard.health = 1;
            deadCard.hasReborn = false;
            deadCard.hasAegis = false;
            Debug.Log($"[Arcane Bolt] {deadCard.cardName} is Reborn with 1 HP");
        }
        else
        {
            ownerBoard.Remove(deadCard);
            Debug.Log($"[Arcane Bolt] {deadCard.cardName} destroyed (death pipeline)");
            AbilityManager.UnregisterCard(deadCard);
        }
    }
}

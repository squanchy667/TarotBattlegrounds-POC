using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TarotBattlegrounds.Combat.Animator;

/// <summary>
/// Runs a single pairwise battle: board snapshot/restore around hero-power combat
/// passives, CombatManager simulation, animated replay wait, 8-player damage
/// scaling, health application, match tracking and network broadcasts.
/// Extracted from GameManager.GameLoop's per-battle block.
/// </summary>
public class BattleExecutor
{
    private readonly GameManager gm;
    private readonly GameSessionState session;
    private readonly BattlePairingService pairing;

    public BattleExecutor(GameManager gm, GameSessionState session, BattlePairingService pairing)
    {
        this.gm = gm;
        this.session = session;
        this.pairing = pairing;
    }

    /// <summary>
    /// Moved verbatim from the per-battle block inside GameManager.GameLoop.
    /// </summary>
    public IEnumerator RunBattle(int p1, int p2, int alivePlayerCount)
    {
        // Snapshot live board state before hero power combat buffs
        // Hero powers modify the live board directly (WarChief +1 Atk, Tactician +2/+2,
        // ArcaneBolt removes cards). We must restore after SimulateBattle clones them.
        var p1Snapshot = gm.players[p1].board.Select(c => (card: c, atk: c.attack, hp: c.health, aegis: c.hasAegis)).ToList();
        var p2Snapshot = gm.players[p2].board.Select(c => (card: c, atk: c.attack, hp: c.health, aegis: c.hasAegis)).ToList();
        var p1BoardBackup = new List<Card>(gm.players[p1].board);
        var p2BoardBackup = new List<Card>(gm.players[p2].board);

        // T115: Trigger combat-start hero powers before battle
        if (HeroPowerManager.Instance != null)
        {
            HeroPowerManager.Instance.TriggerCombatPassives(gm.players[p1], gm.players[p2]);
            HeroPowerManager.Instance.TriggerCombatPassives(gm.players[p2], gm.players[p1]);
        }

        var board1 = gm.players[p1].board;
        var board2 = gm.players[p2].board;
        string p1Name = $"Player {p1 + 1}" + (GameConfig.IsHumanPlayer(p1) ? "" : " (AI)");
        string p2Name = $"Player {p2 + 1}" + (GameConfig.IsHumanPlayer(p2) ? "" : " (AI)");
        // T725: thread the live owners through so owner-aware synergies (Golden Hoard) can read
        // each player's banked coins at StartOfCombat. recordReplay stays at its default (true).
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, gm.players[p1].currentTavernTier, gm.players[p2].currentTavernTier, p1Name, p2Name, true, gm.players[p1], gm.players[p2]);

        // Restore live boards after SimulateBattle has cloned the buffed state
        gm.players[p1].board.Clear(); gm.players[p1].board.AddRange(p1BoardBackup);
        gm.players[p2].board.Clear(); gm.players[p2].board.AddRange(p2BoardBackup);
        foreach (var (card, atk, hp, aegis) in p1Snapshot) { card.attack = atk; card.health = hp; card.hasAegis = aegis; }
        foreach (var (card, atk, hp, aegis) in p2Snapshot) { card.attack = atk; card.health = hp; card.hasAegis = aegis; }
        Debug.Log($"[Combat] {p1Name} vs {p2Name}");
        Debug.Log($"  {p1Name} Board: " + string.Join(", ", board1.Select(c => c.cardName)));
        Debug.Log($"  {p2Name} Board: " + string.Join(", ", board2.Select(c => c.cardName)));

        // T316: Play animated combat replay if this is the local player's battle
        if (CombatAnimator.Instance != null && CombatManager.lastReplay != null
            && IsLocalPlayerBattle(p1, p2))
        {
            CombatAnimator.Instance.PlayReplay(CombatManager.lastReplay);
            // Wait for animation to complete before proceeding
            while (CombatAnimator.Instance.IsPlaying)
                yield return null;
        }

        // Use 8-player scaled damage when applicable
        if (EightPlayerManager.Instance != null && alivePlayerCount > 4 && winner != "Tie")
        {
            int boardStrength = damage; // surviving count + tavern tier from CombatManager
            damage = EightPlayerManager.Instance.CalculateCombatDamage(session.TurnNumber, boardStrength, alivePlayerCount);
        }

        int winnerIndex;
        if (winner == "Tie")
        {
            int p1Health = session.ApplyDamage(p1, damage);
            int p2Health = session.ApplyDamage(p2, damage);
            gm.players[p1].Health = p1Health;
            gm.players[p2].Health = p2Health;
            winnerIndex = -1;
            Debug.Log($"[Combat] TIE! Both take {damage} damage. {p1Name}: {p1Health} HP, {p2Name}: {p2Health} HP");
        }
        else if (winner == p1Name)
        {
            int p2Health = session.ApplyDamage(p2, damage);
            gm.players[p2].Health = p2Health;
            winnerIndex = p1;
            Debug.Log($"[Combat] {p1Name} WINS! {p2Name} takes {damage} damage. Health: {p2Health}");
        }
        else
        {
            int p1Health = session.ApplyDamage(p1, damage);
            gm.players[p1].Health = p1Health;
            winnerIndex = p2;
            Debug.Log($"[Combat] {p2Name} WINS! {p1Name} takes {damage} damage. Health: {p1Health}");
        }

        // Record battle for match info scoreboard
        if (MatchTracker.Instance != null)
            MatchTracker.Instance.RecordBattle(p1, p2, winnerIndex, damage);

#if PHOTON_UNITY_NETWORKING
        // Broadcast combat result and updated states to clients
        if (gm.IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.BroadcastCombatResult(p1, p2, winner, damage);
            NetworkGameBridge.Instance.BroadcastPlayerState(p1);
            NetworkGameBridge.Instance.BroadcastPlayerState(p2);
        }
#endif

        // Track recent opponents for matchmaking
        pairing.RecordOpponents(p1, p2);
    }

    /// <summary>
    /// T316: Check if a battle involves the local/human player for animated replay.
    /// Moved verbatim from GameManager.IsLocalPlayerBattle.
    /// </summary>
    private bool IsLocalPlayerBattle(int p1, int p2)
    {
        // In offline mode, check if either player is the human player
        if (!gm.IsOnlineMode)
            return GameConfig.IsHumanPlayer(p1) || GameConfig.IsHumanPlayer(p2);

#if PHOTON_UNITY_NETWORKING
        if (NetworkGameBridge.Instance != null)
        {
            int localSlot = NetworkGameBridge.Instance.LocalPlayerSlot;
            return p1 == localSlot || p2 == localSlot;
        }
#endif
        return false;
    }
}

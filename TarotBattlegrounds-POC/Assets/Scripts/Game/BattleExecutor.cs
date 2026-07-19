using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TarotBattlegrounds.Combat.Animator;
using TarotBattlegrounds.Combat.Replay;

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
        // Snapshot STATS before hero-power combat buffs (WarChief +1 Atk etc. must not persist).
        // WO-12: do NOT restore board MEMBERSHIP from pre-passive lists — ArcaneBolt kills that
        // go through the death pipeline must stick (restoring membership was the "resurrect" bug).
        var p1Snapshot = gm.players[p1].board.Select(c => (card: c, atk: c.attack, hp: c.health, aegis: c.hasAegis, reborn: c.hasReborn)).ToList();
        var p2Snapshot = gm.players[p2].board.Select(c => (card: c, atk: c.attack, hp: c.health, aegis: c.hasAegis, reborn: c.hasReborn)).ToList();

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

        // T846: write a human-readable play-by-play of every battle for post-session analysis
        if (CombatManager.lastReplay != null)
        {
            bool yourBattle = IsLocalPlayerBattle(p1, p2);
            CombatTranscript.WriteToDisk(CombatManager.lastReplay,
                $"T{session.TurnNumber}_P{p1 + 1}v{p2 + 1}" + (yourBattle ? "_YOURS" : ""),
                $"**Turn {session.TurnNumber}** — {p1Name} (tavern tier {gm.players[p1].currentTavernTier}) vs {p2Name} (tavern tier {gm.players[p2].currentTavernTier})" +
                (yourBattle ? " — **YOUR BATTLE**" : " — AI-vs-AI battle"));
        }

        // Restore pre-passive stats for cards still on the board (membership stays post-passive).
        foreach (var (card, atk, hp, aegis, reborn) in p1Snapshot)
        {
            if (card != null && gm.players[p1].board.Contains(card))
            {
                card.attack = atk;
                card.health = hp;
                card.hasAegis = aegis;
                card.hasReborn = reborn;
            }
        }
        foreach (var (card, atk, hp, aegis, reborn) in p2Snapshot)
        {
            if (card != null && gm.players[p2].board.Contains(card))
            {
                card.attack = atk;
                card.health = hp;
                card.hasAegis = aegis;
                card.hasReborn = reborn;
            }
        }
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

        // Snapshot combat board sizes before result (for result banner — live boards may still show minions)
        int p1BoardCount = board1 != null ? board1.Count : 0;
        int p2BoardCount = board2 != null ? board2.Count : 0;
        // Prefer lastReplay initial state if present (true combat clone counts)
        if (CombatManager.lastReplay?.initialState != null)
        {
            var st = CombatManager.lastReplay.initialState;
            // initialState is attacker/defender order, not p1/p2 — keep live counts as fallback
        }

        int winnerIndex;
        int p1HealthAfter;
        int p2HealthAfter;
        if (winner == "Tie")
        {
            p1HealthAfter = session.ApplyDamage(p1, damage);
            p2HealthAfter = session.ApplyDamage(p2, damage);
            gm.players[p1].Health = p1HealthAfter;
            gm.players[p2].Health = p2HealthAfter;
            winnerIndex = -1;
            Debug.Log($"[Combat] TIE! Both take {damage} damage. {p1Name}: {p1HealthAfter} HP, {p2Name}: {p2HealthAfter} HP");
        }
        else if (winner == p1Name)
        {
            p2HealthAfter = session.ApplyDamage(p2, damage);
            p1HealthAfter = session.GetHealth(p1);
            gm.players[p2].Health = p2HealthAfter;
            winnerIndex = p1;
            Debug.Log($"[Combat] {p1Name} WINS! {p2Name} takes {damage} damage. Health: {p2HealthAfter}");
        }
        else
        {
            p1HealthAfter = session.ApplyDamage(p1, damage);
            p2HealthAfter = session.GetHealth(p2);
            gm.players[p1].Health = p1HealthAfter;
            winnerIndex = p2;
            Debug.Log($"[Combat] {p2Name} WINS! {p1Name} takes {damage} damage. Health: {p1HealthAfter}");
        }

        // Record battle for match info scoreboard
        if (MatchTracker.Instance != null)
            MatchTracker.Instance.RecordBattle(p1, p2, winnerIndex, damage);

        // Testability: always show a clear result for local fights (and a note for spectate)
        ShowBattleResultBanner(p1, p2, winnerIndex, damage, p1HealthAfter, p2HealthAfter, p1BoardCount, p2BoardCount);

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

    private int GetLocalPlayerIndex()
    {
        if (!gm.IsOnlineMode)
            return GameConfig.HumanPlayerIndex;
#if PHOTON_UNITY_NETWORKING
        if (NetworkGameBridge.Instance != null)
            return NetworkGameBridge.Instance.LocalPlayerSlot;
#endif
        return GameConfig.HumanPlayerIndex;
    }

    private void ShowBattleResultBanner(
        int p1, int p2, int winnerIndex, int damage,
        int p1HpAfter, int p2HpAfter, int p1BoardCount, int p2BoardCount)
    {
        var banner = CombatResultBanner.EnsureInstance();
        if (banner == null) return;

        int local = GetLocalPlayerIndex();
        bool localInFight = p1 == local || p2 == local;

        if (localInFight)
        {
            int opp = p1 == local ? p2 : p1;
            int localHp = p1 == local ? p1HpAfter : p2HpAfter;
            int oppHp = p1 == local ? p2HpAfter : p1HpAfter;
            int localBoard = p1 == local ? p1BoardCount : p2BoardCount;
            int oppBoard = p1 == local ? p2BoardCount : p1BoardCount;
            banner.ShowLocalBattleResult(local, opp, winnerIndex, damage, localHp, oppHp, localBoard, oppBoard);
        }
        else
        {
            string w = winnerIndex < 0 ? "Tie" : $"P{winnerIndex + 1} won";
            banner.ShowSpectating($"P{p1 + 1} vs P{p2 + 1}: {w} ({damage} dmg). You were not in this pairing.");
        }
    }
}

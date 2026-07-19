using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using TarotBattlegrounds.Combat.Replay;

/// <summary>
/// Combat log entry for UI visualization
/// </summary>
public struct CombatLogEntry
{
    public enum LogType
    {
        TurnStart,
        Attack,
        Counterattack,
        CardDeath,
        AegisBlock,
        EchoTrigger,
        GuardianTaunt,
        DamageDealt,
        BattleResult
    }
    
    public LogType Type;
    public string AttackerName;
    public string DefenderName;
    public string AttackerOwner;
    public string DefenderOwner;
    public int Damage;
    public int RemainingHealth;
    public string Message;
    public int TurnNumber;
}

public static class CombatManager
{
    // ====== EVENTS FOR UI VISUALIZATION ======
    public static event Action<string, string> OnCombatStart; // (player1Name, player2Name)
    public static event Action<CombatLogEntry> OnCombatLogEntry;
    public static event Action<string, int> OnCombatEnd; // (winnerName, damage)

    /// <summary>
    /// The replay recorded during the most recent SimulateBattle() call.
    /// Null if recordReplay was false or no combat has run yet.
    /// </summary>
    public static CombatReplay lastReplay { get; private set; }

    /// <summary>
    /// Simulate a battle between two boards
    /// </summary>
    public static (int damage, string winner) SimulateBattle(List<Card> pBoard, List<Card> aBoard, int pTavernTier, int aTavernTier, string pName, string aName, bool recordReplay = true, Player pOwner = null, Player aOwner = null)
    {
        Debug.Log($"CombatManager: Simulating battle with {pName}={pBoard.Count} (Tier {pTavernTier}), {aName}={aBoard.Count} (Tier {aTavernTier})");

        // Replay recording setup
        CombatReplay replay = recordReplay ? CombatReplay.CreateEmpty() : null;
        lastReplay = null;

        // Notify combat start
        OnCombatStart?.Invoke(pName, aName);
        
        // Clone boards to avoid mutating originals
        List<Card> pBoardCopy = pBoard.Select(c => c.Clone()).ToList();
        List<Card> aBoardCopy = aBoard.Select(c => c.Clone()).ToList();

        // Track all clones for ability cleanup after combat
        List<Card> allClones = new List<Card>(pBoardCopy.Count + aBoardCopy.Count);
        allClones.AddRange(pBoardCopy);
        allClones.AddRange(aBoardCopy);
        
        // Handle empty boards — WO-04c (B1): still record a minimal replay so CombatAnimator
        // can enter combat presentation briefly (CombatStart → result → CombatEnd) then return
        // to recruit, instead of silent instant skip.
        if (pBoardCopy.Count == 0 && aBoardCopy.Count == 0)
        {
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.BattleResult,
                Message = "Both boards empty - Tie!",
                TurnNumber = 0
            });
            FinalizeMinimalEmptyBoardReplay(replay, pBoardCopy, aBoardCopy, pName, aName, "Tie", 0);
            CleanupCombatClones(allClones);
            OnCombatEnd?.Invoke("Tie", 0);
            return (0, "Tie");
        }

        if (pBoardCopy.Count == 0)
        {
            int damage = CalculateDamage(aBoardCopy, aTavernTier);
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.BattleResult,
                Message = $"{pName} has no cards - {aName} wins! Dealing {damage} damage.",
                TurnNumber = 0,
                Damage = damage
            });
            FinalizeMinimalEmptyBoardReplay(replay, pBoardCopy, aBoardCopy, pName, aName, aName, damage);
            CleanupCombatClones(allClones);
            OnCombatEnd?.Invoke(aName, damage);
            return (damage, aName);
        }

        if (aBoardCopy.Count == 0)
        {
            int damage = CalculateDamage(pBoardCopy, pTavernTier);
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.BattleResult,
                Message = $"{aName} has no cards - {pName} wins! Dealing {damage} damage.",
                TurnNumber = 0,
                Damage = damage
            });
            FinalizeMinimalEmptyBoardReplay(replay, pBoardCopy, aBoardCopy, pName, aName, pName, damage);
            CleanupCombatClones(allClones);
            OnCombatEnd?.Invoke(pName, damage);
            return (damage, pName);
        }
        
        // Trigger StartOfCombat synergies on cloned boards — M1: separate snapshots per player
        if (SynergyManager.Instance != null)
        {
            var pSnapshot = SynergyManager.Instance.CalculateSynergies(pBoardCopy);
            SynergyManager.Instance.TriggerSynergies(SynergyTrigger.StartOfCombat, pBoardCopy, pOwner, pSnapshot);

            var aSnapshot = SynergyManager.Instance.CalculateSynergies(aBoardCopy);
            SynergyManager.Instance.TriggerSynergies(SynergyTrigger.StartOfCombat, aBoardCopy, aOwner, aSnapshot);
        }

        // Fire StartOfCombat ability triggers (e.g., Warlord Supreme's buff)
        foreach (var card in pBoardCopy)
        {
            if (card.health <= 0) continue;
            var ctx = new AbilityContext { SourceCard = card, OwnerBoard = pBoardCopy, EnemyBoard = aBoardCopy };
            AbilityManager.TriggerAbilities(AbilityTrigger.StartOfCombat, ctx);
        }
        foreach (var card in aBoardCopy)
        {
            if (card.health <= 0) continue;
            var ctx = new AbilityContext { SourceCard = card, OwnerBoard = aBoardCopy, EnemyBoard = pBoardCopy };
            AbilityManager.TriggerAbilities(AbilityTrigger.StartOfCombat, ctx);
        }

        // T104: Apply Aura effects at combat start (after synergies and StartOfCombat abilities)
        AuraManager.RefreshAuras(pBoardCopy, null);
        AuraManager.RefreshAuras(aBoardCopy, null);

        // Determine who attacks first
        bool pFirst = UnityEngine.Random.value > 0.5f;
        string firstPlayer = pFirst ? pName : aName;
        string secondPlayer = pFirst ? aName : pName;

        // T301: Build board snapshot now that attacker side is determined
        if (replay != null)
        {
            var attackerClones = pFirst ? pBoardCopy : aBoardCopy;
            var defenderClones = pFirst ? aBoardCopy : pBoardCopy;
            replay.initialState = CombatReplayState.FromBoards(
                attackerClones, defenderClones,
                firstPlayer, secondPlayer, 0, 0);
            replay.RecordCombatStart();
        }

        LogEntry(new CombatLogEntry
        {
            Type = CombatLogEntry.LogType.TurnStart,
            Message = $"{firstPlayer} attacks first!",
            TurnNumber = 0
        });
        
        Debug.Log($"=== COMBAT START ===");
        Debug.Log($"pName={pName} (board: {pBoardCopy.Count}), aName={aName} (board: {aBoardCopy.Count})");
        Debug.Log($"pFirst={pFirst}, so firstPlayer={firstPlayer}");
        Debug.Log($"Entering attack phase: {firstPlayer} attacks first");

        // Setup attack sides
        List<(List<Card> attackers, List<Card> targetBoard, bool isP, string attackerName, string targetName)> sides = 
            new List<(List<Card>, List<Card>, bool, string, string)>
        {
            (pBoardCopy, aBoardCopy, true, pName, aName),
            (aBoardCopy, pBoardCopy, false, aName, pName)
        };
        
        int currentSide = pFirst ? 0 : 1;
        int turnCount = 1;
        bool hasValidAttack = true;
        // C5 fix: Track attack pointer per side for cycling (each minion attacks once before wrapping)
        int[] attackPointer = new int[] { 0, 0 };

        Debug.Log($"Starting combat loop: currentSide={currentSide}, turnCount={turnCount}");

        while (hasValidAttack)
        {
            hasValidAttack = false;
            var (attackers, targetBoard, isP, attackerName, targetName) = sides[currentSide];

            Debug.Log($"--- TURN {turnCount} START ---");
            Debug.Log($"currentSide={currentSide}, attackerName={attackerName}, targetName={targetName}");
            Debug.Log($"attackers count: {attackers.Count(c => c.health > 0)} alive, targets count: {targetBoard.Count(c => c.health > 0)} alive");

            // C5 fix: Cycle through attackers using pointer instead of always picking leftmost
            Card attacker = null;
            int aliveCount = attackers.Count(c => c.health > 0);
            if (aliveCount > 0)
            {
                // Wrap pointer if it went past the end
                if (attackPointer[currentSide] >= attackers.Count)
                    attackPointer[currentSide] = 0;
                // Find next alive attacker starting from pointer position
                int startIdx = attackPointer[currentSide];
                for (int i = 0; i < attackers.Count; i++)
                {
                    int idx = (startIdx + i) % attackers.Count;
                    if (attackers[idx].health > 0)
                    {
                        attacker = attackers[idx];
                        attackPointer[currentSide] = idx + 1; // advance for next turn
                        break;
                    }
                }
            }

            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.TurnStart,
                Message = $"Turn {turnCount}: {attackerName}'s turn",
                TurnNumber = turnCount
            });

            if (attacker != null)
            {
                var aliveTargets = targetBoard.Where(c => c.health > 0).ToList();
                
                // Check for Guardian/Taunt (legacy effectType or new ability system)
                // H3 fix: Pick randomly among all guardians, not just leftmost
                var guardianTargets = aliveTargets.Where(c =>
                    c.effectType == Card.EffectType.Guardian ||
                    c.abilityEffect == Card.AbilityEffectType.Taunt).ToList();
                Card target = guardianTargets.Count > 0
                    ? guardianTargets[UnityEngine.Random.Range(0, guardianTargets.Count)]
                    : aliveTargets.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
                
                if (target != null)
                {
                    hasValidAttack = true;
                    
                    // Log guardian taunt if applicable
                    if (guardianTargets.Count > 0)
                    {
                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.GuardianTaunt,
                            DefenderName = target.cardName,
                            DefenderOwner = targetName,
                            Message = $"{target.cardName} (Guardian) forces attack!",
                            TurnNumber = turnCount
                        });
                    }
                    
                    Debug.Log($"Run Attack - {attacker.cardName} ({attackerName}) targets {target.cardName} ({targetName}), damage {attacker.attack}");

                    // Reset temporary bonus tracker before OnAttack abilities fire
                    attacker.tempBonusDamage = 0;

                    // T302: Record attack start
                    if (replay != null)
                    {
                        int atkIdx = attackers.IndexOf(attacker);
                        int tgtIdx = targetBoard.IndexOf(target);
                        int atkSide = isP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0);
                        replay.RecordAttack(atkIdx, atkSide, tgtIdx, 1 - atkSide);
                    }

                    // Apply attack damage
                    if (target.hasAegis)
                    {
                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.AegisBlock,
                            AttackerName = attacker.cardName,
                            DefenderName = target.cardName,
                            AttackerOwner = attackerName,
                            DefenderOwner = targetName,
                            Message = $"{target.cardName}'s Aegis blocks the attack!",
                            TurnNumber = turnCount
                        });
                        Debug.Log($"Aegis: {target.cardName} ({targetName}) blocks attack");
                        target.hasAegis = false;

                        // T302: Record aegis pop
                        if (replay != null)
                        {
                            int tgtIdx = targetBoard.IndexOf(target);
                            int tgtSide = isP ? (pFirst ? 1 : 0) : (pFirst ? 0 : 1);
                            replay.RecordAegisPopped(tgtIdx, tgtSide);
                        }
                    }
                    else
                    {
                        // Trigger OnAttack abilities only when attack connects (not blocked by Aegis)
                        {
                            int atkSideForAbility = isP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0);
                            TriggerCombatAbility(AbilityTrigger.OnAttack, attacker, target, attackers, targetBoard, replay, atkSideForAbility);
                        }

                        // T110: Apply armor damage reduction
                        int actualDamage = GainArmorAbility.ApplyArmor(target, attacker.attack);
                        target.health -= actualDamage;

                        // T107: Venomous instant kill
                        bool venomKill = false;
                        if (VenomousAbility.HasVenomous(attacker) && target.health > 0)
                        {
                            Debug.Log($"[Venomous] {attacker.cardName} poisons {target.cardName} — instant kill!");
                            target.health = 0;
                            venomKill = true;
                        }

                        // T302: Record damage taken
                        if (replay != null)
                        {
                            int tgtIdx = targetBoard.IndexOf(target);
                            int tgtSide = isP ? (pFirst ? 1 : 0) : (pFirst ? 0 : 1);
                            replay.RecordTakeDamage(tgtIdx, tgtSide, actualDamage, target.health);
                            if (venomKill)
                            {
                                int atkIdxV = attackers.IndexOf(attacker);
                                int atkSideV = isP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0);
                                replay.RecordVenomousKill(atkIdxV, atkSideV, tgtIdx, tgtSide);
                            }
                        }

                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.Attack,
                            AttackerName = attacker.cardName,
                            DefenderName = target.cardName,
                            AttackerOwner = attackerName,
                            DefenderOwner = targetName,
                            Damage = actualDamage,
                            RemainingHealth = target.health,
                            Message = $"{attacker.cardName} attacks {target.cardName} for {actualDamage} damage!",
                            TurnNumber = turnCount
                        });

                        // Synergy-granted cleave: damage adjacent enemies
                        // C7 fix: Skip if card already has OnAttackCleave ability (fired via TriggerCombatAbility above)
                        // TA-2: route through GainArmorAbility.ApplyArmor like the main hit path
                        if (attacker.hasCleave && attacker.abilityEffect != Card.AbilityEffectType.OnAttackCleave)
                        {
                            int targetIndex = targetBoard.IndexOf(target);
                            if (targetIndex >= 0)
                            {
                                void CleaveHit(Card adj)
                                {
                                    if (adj == null || adj.health <= 0) return;
                                    if (adj.hasAegis)
                                    {
                                        adj.hasAegis = false;
                                        Debug.Log($"[Cleave/Synergy] {adj.cardName}'s Aegis blocks cleave");
                                        if (replay != null)
                                        {
                                            int adjIdx = targetBoard.IndexOf(adj);
                                            int tgtSide = isP ? (pFirst ? 1 : 0) : (pFirst ? 0 : 1);
                                            replay.RecordAegisPopped(adjIdx, tgtSide);
                                        }
                                        return;
                                    }
                                    int dmg = GainArmorAbility.ApplyArmor(adj, attacker.attack);
                                    adj.health -= dmg;
                                    Debug.Log($"[Cleave/Synergy] {attacker.cardName} cleaves {adj.cardName} for {dmg}");
                                    if (replay != null)
                                    {
                                        int adjIdx = targetBoard.IndexOf(adj);
                                        int tgtSide = isP ? (pFirst ? 1 : 0) : (pFirst ? 0 : 1);
                                        replay.RecordTakeDamage(adjIdx, tgtSide, dmg, adj.health);
                                    }
                                }
                                if (targetIndex > 0)
                                    CleaveHit(targetBoard[targetIndex - 1]);
                                if (targetIndex < targetBoard.Count - 1)
                                    CleaveHit(targetBoard[targetIndex + 1]);
                            }
                        }

                        Debug.Log($"Post-attack: {target.cardName} ({targetName}) health now {target.health}");
                    }

                    // Apply counterattack damage
                    if (!attacker.hasAegis)
                    {
                        // T110: Apply armor damage reduction to counterattack
                        int counterDamage = GainArmorAbility.ApplyArmor(attacker, target.attack);
                        attacker.health -= counterDamage;

                        // T107: Venomous counterattack instant kill
                        bool venomCounterKill = false;
                        if (VenomousAbility.HasVenomous(target) && attacker.health > 0)
                        {
                            Debug.Log($"[Venomous] {target.cardName} poisons {attacker.cardName} on counterattack — instant kill!");
                            attacker.health = 0;
                            venomCounterKill = true;
                        }

                        // T302: Record counterattack
                        if (replay != null)
                        {
                            int atkIdx = attackers.IndexOf(attacker);
                            int atkSide = isP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0);
                            int tgtIdx = targetBoard.IndexOf(target);
                            replay.RecordCounterattack(tgtIdx, 1 - atkSide, atkIdx, atkSide, counterDamage, attacker.health);
                            if (venomCounterKill)
                                replay.RecordVenomousKill(tgtIdx, 1 - atkSide, atkIdx, atkSide);
                        }

                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.Counterattack,
                            AttackerName = target.cardName,
                            DefenderName = attacker.cardName,
                            AttackerOwner = targetName,
                            DefenderOwner = attackerName,
                            Damage = counterDamage,
                            RemainingHealth = attacker.health,
                            Message = $"{target.cardName} counterattacks for {counterDamage} damage!",
                            TurnNumber = turnCount
                        });

                        Debug.Log($"Post-counterattack: {attacker.cardName} ({attackerName}) health now {attacker.health}");
                    }
                    else
                    {
                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.AegisBlock,
                            AttackerName = target.cardName,
                            DefenderName = attacker.cardName,
                            AttackerOwner = targetName,
                            DefenderOwner = attackerName,
                            Message = $"{attacker.cardName}'s Aegis blocks the counterattack!",
                            TurnNumber = turnCount
                        });
                        Debug.Log($"Aegis: {attacker.cardName} ({attackerName}) blocks counterattack");
                        attacker.hasAegis = false;
                    }

                    // Undo only temporary bonus damage (StealBuff's permanent steal is preserved)
                    if (attacker.tempBonusDamage > 0)
                    {
                        attacker.attack -= attacker.tempBonusDamage;
                        attacker.tempBonusDamage = 0;
                    }

                    // Process all deaths from this attack using death queue
                    // Handles cleave victims, deterministic deathrattle order, and cascade deaths
                    ProcessDeaths(attackers, targetBoard, attackerName, targetName, turnCount, replay, pFirst, isP, allClones);

                    // T106: Windfury — second attack if attacker survived
                    if (WindfuryAbility.HasWindfury(attacker) && attacker.health > 0)
                    {
                        var aliveTargets2 = targetBoard.Where(c => c.health > 0).ToList();
                        if (aliveTargets2.Count > 0)
                        {
                            var guardianTargets2 = aliveTargets2.Where(c =>
                                c.effectType == Card.EffectType.Guardian ||
                                c.abilityEffect == Card.AbilityEffectType.Taunt).ToList();
                            Card target2 = guardianTargets2.Count > 0
                                ? guardianTargets2[UnityEngine.Random.Range(0, guardianTargets2.Count)]
                                : aliveTargets2.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();

                            if (target2 != null)
                            {
                                Debug.Log($"[Windfury] {attacker.cardName} attacks again!");
                                attacker.tempBonusDamage = 0;

                                if (replay != null)
                                {
                                    int atkIdx2 = attackers.IndexOf(attacker);
                                    int tgtIdx2 = targetBoard.IndexOf(target2);
                                    int atkSide2 = isP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0);
                                    // WO-03: second strike is WindfuryAttack (first strike remains RecordAttack)
                                    replay.RecordWindfuryAttack(atkIdx2, atkSide2, tgtIdx2, 1 - atkSide2);
                                }

                                if (target2.hasAegis)
                                {
                                    target2.hasAegis = false;
                                    Debug.Log($"[Windfury] {target2.cardName}'s Aegis blocks second attack");
                                    if (replay != null)
                                    {
                                        int tgtIdx2 = targetBoard.IndexOf(target2);
                                        int tgtSide2 = isP ? (pFirst ? 1 : 0) : (pFirst ? 0 : 1);
                                        replay.RecordAegisPopped(tgtIdx2, tgtSide2);
                                    }
                                }
                                else
                                {
                                    int atkSideWf = isP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0);
                                    TriggerCombatAbility(AbilityTrigger.OnAttack, attacker, target2, attackers, targetBoard, replay, atkSideWf);
                                    int wfDamage = GainArmorAbility.ApplyArmor(target2, attacker.attack);
                                    target2.health -= wfDamage;
                                    bool wfVenom = false;
                                    if (VenomousAbility.HasVenomous(attacker) && target2.health > 0)
                                    {
                                        target2.health = 0;
                                        wfVenom = true;
                                    }

                                    if (replay != null)
                                    {
                                        int tgtIdx2 = targetBoard.IndexOf(target2);
                                        int tgtSide2 = isP ? (pFirst ? 1 : 0) : (pFirst ? 0 : 1);
                                        replay.RecordTakeDamage(tgtIdx2, tgtSide2, wfDamage, target2.health);
                                        if (wfVenom)
                                        {
                                            int atkIdx2 = attackers.IndexOf(attacker);
                                            replay.RecordVenomousKill(atkIdx2, atkSideWf, tgtIdx2, tgtSide2);
                                        }
                                    }

                                    Debug.Log($"[Windfury] {attacker.cardName} hits {target2.cardName} for {wfDamage} (health: {target2.health})");
                                }

                                // Counterattack for Windfury second strike
                                // T841/TA-6: dead target2 must not counter (and must not record dead-on-dead actions)
                                if (target2.health > 0)
                                {
                                    if (!attacker.hasAegis)
                                    {
                                        int wfCounter = GainArmorAbility.ApplyArmor(attacker, target2.attack);
                                        attacker.health -= wfCounter;
                                        bool wfCounterVenom = false;
                                        if (VenomousAbility.HasVenomous(target2) && attacker.health > 0)
                                        {
                                            attacker.health = 0;
                                            wfCounterVenom = true;
                                        }

                                        if (replay != null)
                                        {
                                            int atkIdx2 = attackers.IndexOf(attacker);
                                            int atkSide2 = isP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0);
                                            int tgtIdx2 = targetBoard.IndexOf(target2);
                                            replay.RecordCounterattack(tgtIdx2, 1 - atkSide2, atkIdx2, atkSide2, wfCounter, attacker.health);
                                            if (wfCounterVenom)
                                                replay.RecordVenomousKill(tgtIdx2, 1 - atkSide2, atkIdx2, atkSide2);
                                        }
                                    }
                                    else
                                    {
                                        attacker.hasAegis = false;
                                    }
                                }

                                // Undo only temporary bonus damage for windfury strike
                                if (attacker.tempBonusDamage > 0)
                                {
                                    attacker.attack -= attacker.tempBonusDamage;
                                    attacker.tempBonusDamage = 0;
                                }
                                ProcessDeaths(attackers, targetBoard, attackerName, targetName, turnCount, replay, pFirst, isP, allClones);
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"No valid targets for {attacker.cardName} ({attackerName})");
                }
            }
            
            Debug.Log($"--- TURN {turnCount} END ---");
            currentSide = 1 - currentSide;
            turnCount++;
            Debug.Log($"After turn: currentSide now {currentSide}, turnCount now {turnCount}");

            // Check if battle should end
            int side0Alive = sides[0].attackers.Count(c => c.health > 0);
            int side1Alive = sides[1].attackers.Count(c => c.health > 0);
            Debug.Log($"Board state: sides[0]={side0Alive} alive, sides[1]={side1Alive} alive");

            if (side0Alive == 0 || side1Alive == 0)
            {
                hasValidAttack = false;
                Debug.Log($"Battle ending: one side has no cards");
            }

            // Safety limit to prevent infinite loops
            if (turnCount > 100)
            {
                Debug.LogWarning("Combat exceeded 100 turns, forcing end");
                hasValidAttack = false;
            }
        }
        
        Debug.Log($"Post-removal state: {pName}={pBoardCopy.Count}, {aName}={aBoardCopy.Count}");
        
        // Damage formula (T835 verified 2026-07-18):
        //   SHIPPED: damage = count(surviving minions) + winner tavern tier
        //   HSBG / EXECUTION_PHASES brief: damage = sum(surviving minion tiers) + winner tavern tier
        // Divergence: this uses survivor COUNT, not the sum of their card.tier values.
        // Documented deliberately — do not "fix" without a design decision + golden tests.
        // Filter for alive cards only to be safe (ProcessDeaths should have removed dead cards, but be defensive)
        int pAlive = pBoardCopy.Count(c => c.health > 0);
        int aAlive = aBoardCopy.Count(c => c.health > 0);

        int survivingTier = pAlive > 0 ? pAlive + pTavernTier :
                           aAlive > 0 ? aAlive + aTavernTier : 0;

        int finalDamage = pAlive == 0 && aAlive == 0 ? 0 : survivingTier;

        string winner = pAlive == 0 && aAlive == 0 ? "Tie" :
                       pAlive > 0 ? pName : aName;
        
        // Log battle result
        LogEntry(new CombatLogEntry
        {
            Type = CombatLogEntry.LogType.BattleResult,
            Message = winner == "Tie" ? "Both sides eliminated - Tie!" : 
                     $"{winner} wins! Dealing {finalDamage} damage.",
            Damage = finalDamage,
            TurnNumber = turnCount
        });
        
        Debug.Log($"Outcome: {(winner == "Tie" ? "Both boards empty, Tie" : $"{winner} wins")}, Surviving tier sum = {survivingTier}, Damage = {finalDamage}");
        
        // T302: Populate replay result
        if (replay != null)
        {
            var survivingBoard = pAlive > 0 ? pBoardCopy : (aAlive > 0 ? aBoardCopy : new List<Card>());
            var survivors = new List<CombatCardSnapshot>();
            for (int i = 0; i < survivingBoard.Count; i++)
            {
                if (survivingBoard[i].health > 0)
                    survivors.Add(CombatCardSnapshot.FromCard(survivingBoard[i], i));
            }

            replay.result = new CombatReplayResult
            {
                winnerSide = winner == "Tie" ? "Tie" : (winner == pName ? (pFirst ? "attacker" : "defender") : (pFirst ? "defender" : "attacker")),
                winnerName = winner,
                damageDealt = finalDamage,
                survivingCards = survivors,
                turnCount = turnCount
            };
            replay.RecordCombatEnd();
            lastReplay = replay;
        }

        CleanupCombatClones(allClones);
        OnCombatEnd?.Invoke(winner, finalDamage);

        return (finalDamage, winner);
    }

    /// <summary>
    /// WO-04c (Ofek B1): minimal replay for empty-board early outs.
    /// initialState + CombatStart + result (damage/winner) + CombatEnd → lastReplay.
    /// Numeric outcomes unchanged — only presentation data is added.
    /// Side mapping: P = attacker, A = defender (no first-player roll on empty paths).
    /// </summary>
    private static void FinalizeMinimalEmptyBoardReplay(
        CombatReplay replay,
        List<Card> pBoardCopy, List<Card> aBoardCopy,
        string pName, string aName,
        string winner, int damage)
    {
        if (replay == null) return;

        replay.initialState = CombatReplayState.FromBoards(
            pBoardCopy, aBoardCopy, pName, aName, 0, 0);
        replay.RecordCombatStart();

        var survivors = new List<CombatCardSnapshot>();
        List<Card> survivingBoard = null;
        if (winner == pName) survivingBoard = pBoardCopy;
        else if (winner == aName) survivingBoard = aBoardCopy;

        if (survivingBoard != null)
        {
            for (int i = 0; i < survivingBoard.Count; i++)
            {
                if (survivingBoard[i] != null && survivingBoard[i].health > 0)
                    survivors.Add(CombatCardSnapshot.FromCard(survivingBoard[i], i));
            }
        }

        replay.result = new CombatReplayResult
        {
            winnerSide = winner == "Tie" ? "Tie" : (winner == pName ? "attacker" : "defender"),
            winnerName = winner,
            damageDealt = damage,
            survivingCards = survivors,
            turnCount = 0
        };
        replay.RecordCombatEnd();
        lastReplay = replay;
    }

    /// <summary>
    /// Unregister abilities from all combat clones to prevent memory leak.
    /// </summary>
    private static void CleanupCombatClones(List<Card> clones)
    {
        foreach (var card in clones)
            AbilityManager.UnregisterCard(card);
    }
    
    /// <summary>
    /// Process all deaths from combat damage using a death queue pattern.
    /// Ensures deterministic deathrattle order and handles cascade deaths.
    /// Order: attacker board first (left to right), then defender board (left to right).
    /// </summary>
    // H1 fix: allClones passed in so tokens summoned by deathrattles are tracked for cleanup
    private static void ProcessDeaths(List<Card> attackerBoard, List<Card> defenderBoard,
        string attackerName, string defenderName, int turnCount,
        CombatReplay replay = null, bool pFirst = true, bool isAttackerP = true,
        List<Card> allClones = null)
    {
        const int MAX_CASCADE_ITERATIONS = 10;
        int cascadeCount = 0;

        while (cascadeCount < MAX_CASCADE_ITERATIONS)
        {
            var deathQueue = new List<(Card card, List<Card> ownerBoard, string ownerName, List<Card> enemyBoard)>();

            // Attacker board deaths first (left to right)
            foreach (var card in attackerBoard.ToList())
            {
                if (card.health <= 0)
                    deathQueue.Add((card, attackerBoard, attackerName, defenderBoard));
            }

            // Defender board deaths second (left to right)
            foreach (var card in defenderBoard.ToList())
            {
                if (card.health <= 0)
                    deathQueue.Add((card, defenderBoard, defenderName, attackerBoard));
            }

            if (deathQueue.Count == 0)
                break;

            Debug.Log($"[Death Queue] Processing {deathQueue.Count} deaths (cascade {cascadeCount})");

            foreach (var (deadCard, ownerBoard, ownerName, enemyBoard) in deathQueue)
            {
                // T105: Check for Reborn before processing death
                bool willReborn = RebornAbility.HasReborn(deadCard);

                LogEntry(new CombatLogEntry
                {
                    Type = CombatLogEntry.LogType.CardDeath,
                    DefenderName = deadCard.cardName,
                    DefenderOwner = ownerName,
                    Message = willReborn
                        ? $"{deadCard.cardName} is destroyed but will be Reborn!"
                        : $"{deadCard.cardName} is destroyed!",
                    TurnNumber = turnCount
                });

                bool isOnAttackerBoard = ownerBoard == attackerBoard;
                int deadSide = isOnAttackerBoard ? (isAttackerP ? (pFirst ? 0 : 1) : (pFirst ? 1 : 0))
                                                 : (isAttackerP ? (pFirst ? 1 : 0) : (pFirst ? 0 : 1));

                if (deadCard.effectType == Card.EffectType.Echo)
                    TriggerEcho(deadCard, ownerBoard, ownerName, turnCount, replay, deadSide);

                // Snapshot owner board before deathrattle so WO-03 can emit SummonToken for new cards
                var boardBeforeDeathrattle = new List<Card>(ownerBoard);

                TriggerCombatAbility(AbilityTrigger.Deathrattle, deadCard, null, ownerBoard, enemyBoard, replay, deadSide);

                // WO-03: Record summons for any new cards inserted by deathrattle
                if (replay != null)
                {
                    for (int i = 0; i < ownerBoard.Count; i++)
                    {
                        Card c = ownerBoard[i];
                        if (!boardBeforeDeathrattle.Contains(c))
                        {
                            int tokenValue = Mathf.Max(c.attack, c.health);
                            replay.RecordSummonToken(i, deadSide, tokenValue > 0 ? tokenValue : 1, c.cardName);
                        }
                    }
                }

                // H1 fix: Register any tokens summoned by this deathrattle into allClones so
                // CleanupCombatClones() will unregister their abilities after combat ends.
                if (allClones != null)
                {
                    foreach (var card in attackerBoard)
                    {
                        if (!allClones.Contains(card))
                        {
                            allClones.Add(card);
                            Debug.Log($"[H1] New token '{card.cardName}' added to allClones for cleanup.");
                        }
                    }
                    foreach (var card in defenderBoard)
                    {
                        if (!allClones.Contains(card))
                        {
                            allClones.Add(card);
                            Debug.Log($"[H1] New token '{card.cardName}' added to allClones for cleanup.");
                        }
                    }
                }

                // T302: Record death before removing from board
                if (replay != null)
                {
                    int deadIdx = ownerBoard.IndexOf(deadCard);
                    if (deadIdx >= 0)
                        replay.RecordDie(deadIdx, deadSide);
                }

                // T105: Reborn — revive with 1 HP at same position, lose Reborn keyword (once)
                if (willReborn)
                {
                    int rebornIdx = ownerBoard.IndexOf(deadCard);
                    deadCard.health = 1;
                    deadCard.hasAegis = false; // Reborn strips Aegis
                    // Consume flag + ability registration so this cannot reborn again
                    // (old HasReborn still returned true from leftover RebornAbility → infinite loop
                    //  with durable tanks like Vault Guardian / Celestial Guardian).
                    RebornAbility.ConsumeReborn(deadCard);
                    Debug.Log($"[Reborn] {deadCard.cardName} revives with 1 HP at position {rebornIdx} (reborn spent)");
                    // WO-03: record reborn for animator
                    if (replay != null && rebornIdx >= 0)
                        replay.RecordReborn(rebornIdx, deadSide);
                    // Card stays in the board at its current position — don't remove it
                }
                else
                {
                    ownerBoard.Remove(deadCard);
                    Debug.Log($"{deadCard.cardName} removed from {ownerName} board");
                }

                // T101: Notify all surviving allies that a friendly card has died
                // Skip if card was Reborn — it didn't truly die, so OnAllyDeath should not fire
                if (!willReborn)
                    TriggerOnAllyDeathForBoard(deadCard, ownerBoard, enemyBoard);

                // T104: Refresh auras after board changes
                AuraManager.RefreshAuras(ownerBoard, null);
            }

            cascadeCount++;
        }

        if (cascadeCount >= MAX_CASCADE_ITERATIONS)
            Debug.LogWarning($"[Death Queue] Reached max cascade iterations ({MAX_CASCADE_ITERATIONS})");
    }

    /// <summary>
    /// Fire OnAllyDeath trigger on all surviving friendly cards after a death.
    /// </summary>
    private static void TriggerOnAllyDeathForBoard(Card deadCard, List<Card> survivingBoard, List<Card> enemyBoard)
    {
        foreach (var ally in survivingBoard)
        {
            if (ally.health <= 0) continue;
            var context = new AbilityContext
            {
                SourceCard = ally,
                TargetCard = deadCard,
                OwnerBoard = survivingBoard,
                EnemyBoard = enemyBoard,
                Owner = null
            };
            AbilityManager.TriggerAbilities(AbilityTrigger.OnAllyDeath, context);
        }
    }

    private static void TriggerEcho(Card dyingCard, List<Card> board, string ownerName, int turn,
        CombatReplay replay = null, int ownerSide = -1)
    {
        Card ally = board.Where(c => c != dyingCard && c.health > 0).OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
        if (ally != null)
        {
            string[] param = dyingCard.effectParameter?.Split(':') ?? new string[0];
            int value = param.Length > 1 && int.TryParse(param[1], out int v) ? v : 0;
            ally.attack += value;
            
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.EchoTrigger,
                AttackerName = dyingCard.cardName,
                DefenderName = ally.cardName,
                AttackerOwner = ownerName,
                Damage = value,
                Message = $"{dyingCard.cardName}'s Echo buffs {ally.cardName}'s attack by {value}!",
                TurnNumber = turn
            });

            // WO-03: replay EchoTrigger (not BuffApplied — avoid double-count)
            if (replay != null && ownerSide >= 0)
            {
                int srcIdx = board.IndexOf(dyingCard);
                int allyIdx = board.IndexOf(ally);
                if (srcIdx >= 0 && allyIdx >= 0)
                    replay.RecordEchoTrigger(srcIdx, ownerSide, allyIdx, ownerSide, value);
            }
            
            Debug.Log($"Echo: {dyingCard.cardName} ({ownerName}) buffs {ally.cardName} attack by {value} (New Attack: {ally.attack})");
        }
    }
    
    /// <summary>
    /// Trigger abilities during combat (OnAttack, Deathrattle, etc.)
    /// WO-03: optional replay + sourceSide records AbilityTrigger without changing ability order.
    /// </summary>
    private static void TriggerCombatAbility(AbilityTrigger trigger, Card source, Card target,
        List<Card> ownerBoard, List<Card> enemyBoard, CombatReplay replay = null, int sourceSide = -1)
    {
        // Only emit AbilityTrigger when the card has something that could fire (avoid empty noise).
        if (replay != null && source != null && sourceSide >= 0 && ownerBoard != null)
        {
            var registered = AbilityManager.GetAbilities(source);
            bool hasAbility = source.abilityEffect != Card.AbilityEffectType.None
                || source.abilityTrigger != AbilityTrigger.None
                || !string.IsNullOrEmpty(source.ability)
                || (registered != null && registered.Count > 0);
            if (hasAbility)
            {
                int si = ownerBoard.IndexOf(source);
                if (si >= 0)
                {
                    string name = !string.IsNullOrEmpty(source.ability)
                        ? source.ability
                        : (source.abilityEffect != Card.AbilityEffectType.None
                            ? source.abilityEffect.ToString()
                            : trigger.ToString());
                    replay.RecordAbilityTrigger(si, sourceSide, name);
                }
            }
        }

        var context = new AbilityContext
        {
            SourceCard = source,
            TargetCard = target,
            OwnerBoard = ownerBoard,
            EnemyBoard = enemyBoard,
            Owner = null // Combat uses cloned cards, no player reference
        };
        AbilityManager.TriggerAbilities(trigger, context);
    }

    private static int CalculateDamage(List<Card> survivingBoard, int tavernTier)
    {
        // T835: same formula as the main battle path — survivor COUNT + tavern tier
        // (NOT sum of minion tiers). See comment at final-damage calculation above.
        int damage = survivingBoard.Count + tavernTier;
        return damage;
    }
    
    private static void LogEntry(CombatLogEntry entry)
    {
        Debug.Log($"[LogEntry] Invoking event for: {entry.Message} (Turn {entry.TurnNumber})");
        OnCombatLogEntry?.Invoke(entry);
    }
}

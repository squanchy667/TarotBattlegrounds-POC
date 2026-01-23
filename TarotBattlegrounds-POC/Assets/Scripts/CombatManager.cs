using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

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
    /// Simulate a battle between two boards
    /// </summary>
    public static (int damage, string winner) SimulateBattle(List<Card> pBoard, List<Card> aBoard, int tavernTier, string pName, string aName)
    {
        Debug.Log($"CombatManager: Simulating battle with {pName}={pBoard.Count}, {aName}={aBoard.Count}, Tavern Tier={tavernTier}");
        
        // Notify combat start
        OnCombatStart?.Invoke(pName, aName);
        
        // Clone boards to avoid mutating originals
        List<Card> pBoardCopy = pBoard.Select(c => c.Clone()).ToList();
        List<Card> aBoardCopy = aBoard.Select(c => c.Clone()).ToList();
        
        // Handle empty boards
        if (pBoardCopy.Count == 0 && aBoardCopy.Count == 0)
        {
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.BattleResult,
                Message = "Both boards empty - Tie!",
                TurnNumber = 0
            });
            OnCombatEnd?.Invoke("Tie", 0);
            return (0, "Tie");
        }
        
        if (pBoardCopy.Count == 0)
        {
            int damage = CalculateDamage(aBoardCopy, tavernTier);
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.BattleResult,
                Message = $"{pName} has no cards - {aName} wins! Dealing {damage} damage.",
                TurnNumber = 0,
                Damage = damage
            });
            OnCombatEnd?.Invoke(aName, damage);
            return (damage, aName);
        }
        
        if (aBoardCopy.Count == 0)
        {
            int damage = CalculateDamage(pBoardCopy, tavernTier);
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.BattleResult,
                Message = $"{aName} has no cards - {pName} wins! Dealing {damage} damage.",
                TurnNumber = 0,
                Damage = damage
            });
            OnCombatEnd?.Invoke(pName, damage);
            return (damage, pName);
        }
        
        // Trigger StartOfCombat synergies on cloned boards
        if (SynergyManager.Instance != null)
        {
            // Update synergy counts for combat (using cloned boards)
            SynergyManager.Instance.UpdateTribeCounts(pBoardCopy);
            SynergyManager.Instance.TriggerSynergies(SynergyTrigger.StartOfCombat, pBoardCopy, null);

            SynergyManager.Instance.UpdateTribeCounts(aBoardCopy);
            SynergyManager.Instance.TriggerSynergies(SynergyTrigger.StartOfCombat, aBoardCopy, null);
        }

        // Determine who attacks first
        bool pFirst = UnityEngine.Random.value > 0.5f;
        string firstPlayer = pFirst ? pName : aName;
        string secondPlayer = pFirst ? aName : pName;
        
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

        Debug.Log($"Starting combat loop: currentSide={currentSide}, turnCount={turnCount}");

        while (hasValidAttack)
        {
            hasValidAttack = false;
            var (attackers, targetBoard, isP, attackerName, targetName) = sides[currentSide];

            Debug.Log($"--- TURN {turnCount} START ---");
            Debug.Log($"currentSide={currentSide}, attackerName={attackerName}, targetName={targetName}");
            Debug.Log($"attackers count: {attackers.Count(c => c.health > 0)} alive, targets count: {targetBoard.Count(c => c.health > 0)} alive");

            var attacker = attackers.FirstOrDefault(c => c.health > 0);

            Debug.Log($"[BEFORE_LOGENTRY] About to log Turn {turnCount}: {attackerName}'s turn");
            LogEntry(new CombatLogEntry
            {
                Type = CombatLogEntry.LogType.TurnStart,
                Message = $"Turn {turnCount}: {attackerName}'s turn",
                TurnNumber = turnCount
            });
            Debug.Log($"[AFTER_LOGENTRY] Done logging Turn {turnCount}: {attackerName}'s turn");
            
            if (attacker != null)
            {
                var aliveTargets = targetBoard.Where(c => c.health > 0).ToList();
                
                // Check for Guardian (taunt)
                var guardianTarget = aliveTargets.FirstOrDefault(c => c.effectType == Card.EffectType.Guardian);
                Card target = guardianTarget ?? aliveTargets.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
                
                if (target != null)
                {
                    hasValidAttack = true;
                    
                    // Log guardian taunt if applicable
                    if (guardianTarget != null)
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

                    // Trigger OnAttack abilities
                    TriggerCombatAbility(AbilityTrigger.OnAttack, attacker, target, attackers, targetBoard);

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
                    }
                    else
                    {
                        target.health -= attacker.attack;
                        
                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.Attack,
                            AttackerName = attacker.cardName,
                            DefenderName = target.cardName,
                            AttackerOwner = attackerName,
                            DefenderOwner = targetName,
                            Damage = attacker.attack,
                            RemainingHealth = target.health,
                            Message = $"{attacker.cardName} attacks {target.cardName} for {attacker.attack} damage!",
                            TurnNumber = turnCount
                        });
                        
                        Debug.Log($"Post-attack: {target.cardName} ({targetName}) health now {target.health}");
                    }
                    
                    // Apply counterattack damage
                    if (!attacker.hasAegis)
                    {
                        attacker.health -= target.attack;
                        
                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.Counterattack,
                            AttackerName = target.cardName,
                            DefenderName = attacker.cardName,
                            AttackerOwner = targetName,
                            DefenderOwner = attackerName,
                            Damage = target.attack,
                            RemainingHealth = attacker.health,
                            Message = $"{target.cardName} counterattacks for {target.attack} damage!",
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
                    
                    // Handle target death
                    if (target.health <= 0)
                    {
                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.CardDeath,
                            DefenderName = target.cardName,
                            DefenderOwner = targetName,
                            Message = $"{target.cardName} is destroyed!",
                            TurnNumber = turnCount
                        });
                        
                        // Echo effect (legacy)
                        if (target.effectType == Card.EffectType.Echo)
                        {
                            TriggerEcho(target, targetBoard, targetName, turnCount);
                        }

                        // Trigger Deathrattle abilities
                        TriggerCombatAbility(AbilityTrigger.Deathrattle, target, null, targetBoard, attackers);

                        targetBoard.Remove(target);
                        Debug.Log($"{target.cardName} removed from {targetName} board");
                    }

                    // Handle attacker death
                    if (attacker.health <= 0)
                    {
                        LogEntry(new CombatLogEntry
                        {
                            Type = CombatLogEntry.LogType.CardDeath,
                            DefenderName = attacker.cardName,
                            DefenderOwner = attackerName,
                            Message = $"{attacker.cardName} is destroyed!",
                            TurnNumber = turnCount
                        });

                        // Echo effect (legacy)
                        if (attacker.effectType == Card.EffectType.Echo)
                        {
                            TriggerEcho(attacker, attackers, attackerName, turnCount);
                        }

                        // Trigger Deathrattle abilities
                        TriggerCombatAbility(AbilityTrigger.Deathrattle, attacker, null, attackers, targetBoard);

                        attackers.Remove(attacker);
                        Debug.Log($"{attacker.cardName} removed from {attackerName} board");
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
        
        // Calculate results
        int survivingTier = pBoardCopy.Count > 0 ? pBoardCopy.Sum(c => c.tier) + tavernTier : 
                           aBoardCopy.Count > 0 ? aBoardCopy.Sum(c => c.tier) + tavernTier : 0;
        
        int finalDamage = pBoardCopy.Count == 0 && aBoardCopy.Count == 0 ? 0 : Mathf.Min(5, survivingTier);
        
        string winner = pBoardCopy.Count == 0 && aBoardCopy.Count == 0 ? "Tie" :
                       pBoardCopy.Count > 0 ? pName : aName;
        
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
        
        OnCombatEnd?.Invoke(winner, finalDamage);
        
        return (finalDamage, winner);
    }
    
    private static void TriggerEcho(Card dyingCard, List<Card> board, string ownerName, int turn)
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
            
            Debug.Log($"Echo: {dyingCard.cardName} ({ownerName}) buffs {ally.cardName} attack by {value} (New Attack: {ally.attack})");
        }
    }
    
    /// <summary>
    /// Trigger abilities during combat (OnAttack, Deathrattle, etc.)
    /// </summary>
    private static void TriggerCombatAbility(AbilityTrigger trigger, Card source, Card target, List<Card> ownerBoard, List<Card> enemyBoard)
    {
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
        int tierSum = survivingBoard.Sum(c => c.tier) + tavernTier;
        return Mathf.Min(5, tierSum);
    }
    
    private static void LogEntry(CombatLogEntry entry)
    {
        Debug.Log($"[LogEntry] Invoking event for: {entry.Message} (Turn {entry.TurnNumber})");
        OnCombatLogEntry?.Invoke(entry);
    }
}

using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TarotBattlegrounds.Combat.Replay;

/// <summary>
/// WO-03: Assert CombatManager emits the full set of combat replay action types
/// and that action indices stay aligned with initialState + summons.
/// </summary>
[TestFixture]
public class CombatReplayCompletenessTests
{
    private Card CreateCard(string name, int attack, int health, int tier = 1)
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tier = tier;
        card.attack = attack;
        card.health = health;
        card.tribes = new TribeType[0];
        return card;
    }

    [TearDown]
    public void TearDown()
    {
        AbilityManager.ClearAll();
    }

    private static bool ReplayHas(CombatActionType type)
    {
        Assert.IsNotNull(CombatManager.lastReplay, "lastReplay should be populated");
        Assert.IsNotNull(CombatManager.lastReplay.actions, "lastReplay.actions should exist");
        return CombatManager.lastReplay.actions.Any(a => a.type == type);
    }

    [Test]
    public void Replay_Contains_WindfuryAttack_OnSecondStrike()
    {
        var attacker = CreateCard("WindfuryBruiser", attack: 5, health: 10);
        attacker.hasWindfury = true;
        // Tanky target so first strike doesn't wipe the board before second strike
        var defender = CreateCard("Tank", attack: 1, health: 20);

        CombatManager.SimulateBattle(
            new List<Card> { attacker },
            new List<Card> { defender },
            1, 1, "P1", "P2", recordReplay: true);

        Assert.IsTrue(ReplayHas(CombatActionType.WindfuryAttack),
            "Windfury second strike should record WindfuryAttack, not only Attack");
        Assert.IsTrue(ReplayHas(CombatActionType.Attack),
            "First strike should still record Attack");
    }

    [Test]
    public void Replay_Contains_VenomousKill()
    {
        var attacker = CreateCard("Viper", attack: 1, health: 5);
        attacker.hasVenomous = true;
        var defender = CreateCard("Beefy", attack: 0, health: 50);

        CombatManager.SimulateBattle(
            new List<Card> { attacker },
            new List<Card> { defender },
            1, 1, "P1", "P2", recordReplay: true);

        Assert.IsTrue(ReplayHas(CombatActionType.VenomousKill),
            "Venomous instant kill should emit VenomousKill");
    }

    [Test]
    public void Replay_Contains_Reborn()
    {
        var killer = CreateCard("Killer", attack: 10, health: 10);
        var reborn = CreateCard("Phoenix", attack: 1, health: 1);
        reborn.hasReborn = true;

        CombatManager.SimulateBattle(
            new List<Card> { killer },
            new List<Card> { reborn },
            1, 1, "P1", "P2", recordReplay: true);

        Assert.IsTrue(ReplayHas(CombatActionType.Reborn),
            "Reborn revive should emit Reborn action");
        Assert.IsTrue(ReplayHas(CombatActionType.Die),
            "Death before reborn should still emit Die");
    }

    [Test]
    public void Replay_Contains_SummonToken_FromDeathrattle()
    {
        var killer = CreateCard("Killer", attack: 10, health: 10);
        var host = CreateCard("TokenHost", attack: 1, health: 1);
        host.abilityTrigger = AbilityTrigger.Deathrattle;
        host.abilityEffect = Card.AbilityEffectType.SummonTokenOnDeath;
        host.abilityValue = 2;
        // ability string helps AbilityTrigger naming; registration comes from Clone.RegisterAbility
        host.ability = "Summon Token";

        CombatManager.SimulateBattle(
            new List<Card> { killer },
            new List<Card> { host },
            1, 1, "P1", "P2", recordReplay: true);

        Assert.IsTrue(ReplayHas(CombatActionType.SummonToken),
            "Deathrattle SummonTokenOnDeath should emit SummonToken");
        Assert.IsTrue(ReplayHas(CombatActionType.AbilityTrigger),
            "Deathrattle with ability should also emit AbilityTrigger");
    }

    [Test]
    public void Replay_Contains_EchoTrigger()
    {
        // Deterministic: Echo has Taunt so killer always hits it while Buddy is still alive
        // (random target selection made this flaky when Buddy died first).
        var killer = CreateCard("Killer", attack: 10, health: 20);
        var echo = CreateCard("Echoer", attack: 0, health: 1);
        echo.effectType = Card.EffectType.Echo;
        echo.effectParameter = "attack:2";
        echo.abilityEffect = Card.AbilityEffectType.Taunt;
        var ally = CreateCard("Buddy", attack: 0, health: 20);

        CombatManager.SimulateBattle(
            new List<Card> { killer },
            new List<Card> { echo, ally },
            1, 1, "P1", "P2", recordReplay: true);

        Assert.IsTrue(ReplayHas(CombatActionType.EchoTrigger),
            "Echo death should emit EchoTrigger when an ally is present");
    }

    // ─── WO-04c (B1): empty-board minimal replay ───

    [Test]
    public void Replay_EmptyBothBoards_RecordsMinimalReplay_Tie()
    {
        CombatManager.SimulateBattle(
            new List<Card>(),
            new List<Card>(),
            1, 1, "P1", "P2", recordReplay: true);

        var replay = CombatManager.lastReplay;
        Assert.IsNotNull(replay, "empty-board path must set lastReplay when recordReplay=true");
        Assert.IsNotNull(replay.initialState);
        Assert.IsTrue(ReplayHas(CombatActionType.CombatStart));
        Assert.IsTrue(ReplayHas(CombatActionType.CombatEnd));
        Assert.IsNotNull(replay.result);
        Assert.AreEqual("Tie", replay.result.winnerName);
        Assert.AreEqual(0, replay.result.damageDealt);
    }

    [Test]
    public void Replay_EmptyPlayerBoard_RecordsMinimalReplay_WithDamage()
    {
        var defender = CreateCard("Only", attack: 2, health: 3);
        var (damage, winner) = CombatManager.SimulateBattle(
            new List<Card>(),
            new List<Card> { defender },
            1, 2, "P1", "P2", recordReplay: true);

        Assert.AreEqual("P2", winner);
        Assert.IsTrue(damage > 0);

        var replay = CombatManager.lastReplay;
        Assert.IsNotNull(replay);
        Assert.IsTrue(ReplayHas(CombatActionType.CombatStart));
        Assert.IsTrue(ReplayHas(CombatActionType.CombatEnd));
        Assert.IsNotNull(replay.result);
        Assert.AreEqual("P2", replay.result.winnerName);
        Assert.AreEqual(damage, replay.result.damageDealt);
        Assert.IsNotNull(replay.initialState.defenderBoard);
        Assert.AreEqual(1, replay.initialState.defenderBoard.Count);
    }

    [Test]
    public void Replay_EmptyOpponentBoard_RecordsMinimalReplay_PlayerWins()
    {
        var attacker = CreateCard("Only", attack: 2, health: 3);
        var (damage, winner) = CombatManager.SimulateBattle(
            new List<Card> { attacker },
            new List<Card>(),
            2, 1, "P1", "P2", recordReplay: true);

        Assert.AreEqual("P1", winner);
        Assert.IsTrue(damage > 0);

        var replay = CombatManager.lastReplay;
        Assert.IsNotNull(replay);
        Assert.AreEqual("P1", replay.result.winnerName);
        Assert.AreEqual(damage, replay.result.damageDealt);
        Assert.IsTrue(ReplayHas(CombatActionType.CombatStart));
        Assert.IsTrue(ReplayHas(CombatActionType.CombatEnd));
    }

    [Test]
    public void Replay_IndexAlignment_Invariant()
    {
        var a = CreateCard("A", attack: 3, health: 6);
        a.hasWindfury = true;
        var b = CreateCard("B", attack: 2, health: 4);
        b.hasVenomous = true;
        var c = CreateCard("C", attack: 1, health: 1);
        c.hasReborn = true;
        c.abilityTrigger = AbilityTrigger.Deathrattle;
        c.abilityEffect = Card.AbilityEffectType.SummonTokenOnDeath;
        c.abilityValue = 1;

        CombatManager.SimulateBattle(
            new List<Card> { a, b },
            new List<Card> { c },
            1, 1, "P1", "P2", recordReplay: true);

        var replay = CombatManager.lastReplay;
        Assert.IsNotNull(replay);
        Assert.IsNotNull(replay.initialState);

        // Battlegrounds board hard cap is 7. Indices are valid for the board *at action time*
        // (removals re-index; tracking live max by growth-only is wrong after deaths).
        const int BoardCap = 7;

        foreach (var action in replay.actions)
        {
            if (action.type == CombatActionType.CombatStart || action.type == CombatActionType.CombatEnd)
                continue;

            if (action.sourceCardIndex >= 0)
            {
                Assert.Less(action.sourceCardIndex, BoardCap,
                    $"source index {action.sourceCardIndex} side {action.sourceOwnerSide} OOR for {action.type}");
            }

            if (action.targetCardIndex >= 0)
            {
                Assert.Less(action.targetCardIndex, BoardCap,
                    $"target index {action.targetCardIndex} side {action.targetOwnerSide} OOR for {action.type}");
            }
        }
    }
}

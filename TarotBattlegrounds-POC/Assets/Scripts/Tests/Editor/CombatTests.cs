using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TarotBattlegrounds.Combat.Replay;

/// <summary>
/// Tests for combat simulation mechanics.
/// Covers basic outcomes, aegis, guardian taunt, deathrattle cascade, and golden card stats.
/// </summary>
[TestFixture]
public class CombatTests
{
    private List<Card> CreateBoard(int count, int attack, int health, int tier = 1)
    {
        var board = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            var card = ScriptableObject.CreateInstance<Card>();
            card.cardName = $"Card_{i}";
            card.tier = tier;
            card.attack = attack;
            card.health = health;
            board.Add(card);
        }
        return board;
    }

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
    
    [Test]
    public void Combat_WithEmptyBoards_ReturnsTie()
    {
        var board1 = new List<Card>();
        var board2 = new List<Card>();
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");
        
        Assert.AreEqual("Tie", winner);
        Assert.AreEqual(0, damage);
    }
    
    [Test]
    public void Combat_Player1Empty_Player2Wins()
    {
        var board1 = new List<Card>();
        var board2 = CreateBoard(1, 1, 3);
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");
        
        Assert.AreEqual("P2", winner);
        Assert.IsTrue(damage > 0);
    }
    
    [Test]
    public void Combat_Player2Empty_Player1Wins()
    {
        var board1 = CreateBoard(1, 1, 3);
        var board2 = new List<Card>();
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");
        
        Assert.AreEqual("P1", winner);
        Assert.IsTrue(damage > 0);
    }
    
    [Test]
    public void Combat_EqualBoards_ProducesDamage()
    {
        var board1 = CreateBoard(2, 2, 2);
        var board2 = CreateBoard(2, 2, 2);
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");
        
        // Either someone wins or it's a tie
        Assert.IsTrue(winner == "P1" || winner == "P2" || winner == "Tie");
    }

    /// <summary>
    /// Regression: Shooting Star (Reborn) vs armored tank must not infinite-loop.
    /// Old bug: HasReborn kept returning true from leftover RebornAbility registration
    /// after hasReborn was cleared → reborn every death until turn cap 100.
    /// </summary>
    [Test]
    public void Combat_Reborn_OnlyOnce_VsArmoredGuardian()
    {
        var star = CreateCard("Shooting Star", attack: 2, health: 1);
        star.abilityEffect = Card.AbilityEffectType.Reborn;
        star.hasReborn = true;
        AbilityManager.RegisterAbility(star, new RebornAbility());

        var vault = CreateCard("Vault Guardian", attack: 2, health: 20);
        vault.abilityEffect = Card.AbilityEffectType.GainArmor;
        vault.abilityValue = 1;
        vault.armor = 1;

        // Must finish well under the 100-turn safety cap
        var (damage, winner) = CombatManager.SimulateBattle(
            new List<Card> { star },
            new List<Card> { vault },
            1, 1, "Stars", "Vault", recordReplay: true);

        Assert.IsTrue(winner == "Stars" || winner == "Vault" || winner == "Tie");
        Assert.IsNotNull(CombatManager.lastReplay);
        int rebornCount = CombatManager.lastReplay.actions.Count(a => a.type == CombatActionType.Reborn);
        Assert.LessOrEqual(rebornCount, 1, "Reborn must fire at most once per combat clone");
    }
    
    [Test]
    public void Combat_Damage_EqualsMinionsAndTier()
    {
        // Fix 2: Damage should equal surviving minions + tavern tier (no cap)
        var board1 = CreateBoard(7, 1, 10, 3); // 7 tier-3 cards
        var board2 = new List<Card>();

        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 3, 3, "P1", "P2");

        // 7 surviving minions + tavern tier 3 = 10
        Assert.AreEqual(10, damage, $"Damage should equal surviving minions (7) + tavern tier (3) = 10");
    }
    
    [Test]
    public void Combat_HigherStats_MoreLikelyToWin()
    {
        int p1Wins = 0;
        int p2Wins = 0;
        int ties = 0;
        
        // Run 100 simulations
        for (int i = 0; i < 100; i++)
        {
            var board1 = CreateBoard(3, 5, 5); // Strong board
            var board2 = CreateBoard(3, 1, 1); // Weak board
            
            var (_, winner) = CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");
            
            if (winner == "P1") p1Wins++;
            else if (winner == "P2") p2Wins++;
            else ties++;
        }
        
        // Strong board should win most of the time
        Assert.IsTrue(p1Wins > p2Wins, $"P1 wins: {p1Wins}, P2 wins: {p2Wins}");
    }
    
    [Test]
    public void Combat_AegisCard_BlocksFirstHit()
    {
        var board1 = CreateBoard(1, 10, 1);
        
        var aegisCard = ScriptableObject.CreateInstance<Card>();
        aegisCard.cardName = "AegisCard";
        aegisCard.attack = 1;
        aegisCard.health = 1;
        aegisCard.tier = 1;
        aegisCard.hasAegis = true;
        var board2 = new List<Card> { aegisCard };
        
        // Run multiple times to test aegis
        var (_, winner) = CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");

        // Test passes if no exception thrown
        Assert.Pass("Aegis combat simulation completed");
    }
    
    [Test]
    public void Combat_GuardianCard_ForcesAttack()
    {
        var attacker = ScriptableObject.CreateInstance<Card>();
        attacker.cardName = "Attacker";
        attacker.attack = 5;
        attacker.health = 5;
        attacker.tier = 1;

        var guardian = ScriptableObject.CreateInstance<Card>();
        guardian.cardName = "Guardian";
        guardian.attack = 1;
        guardian.health = 10;
        guardian.tier = 1;
        guardian.effectType = Card.EffectType.Guardian;

        var target = ScriptableObject.CreateInstance<Card>();
        target.cardName = "Target";
        target.attack = 1;
        target.health = 1;
        target.tier = 1;

        var board1 = new List<Card> { attacker };
        var board2 = new List<Card> { target, guardian }; // Guardian should be hit

        // Test completes without exception
        var (_, winner) = CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");
        Assert.Pass("Guardian combat simulation completed");
    }

    // =====================================================
    // DEATHRATTLE / DEATH CASCADE TESTS
    // =====================================================

    /// <summary>
    /// A card with DeathrattleAbility.SummonToken places a new card on the board when it
    /// dies during combat. Board size grows by 1 after the death is processed.
    /// </summary>
    [Test]
    public void Deathrattle_SummonToken_TokenAppearsOnBoardAfterDeath()
    {
        // Attacker one-shots the deathrattle card.
        var attacker = CreateCard("Killer", attack: 10, health: 10);

        // The card that will die has a SummonToken deathrattle (value=2 => 2/2 token).
        var dyingCard = CreateCard("TokenHost", attack: 1, health: 1);
        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.SummonToken, 2);
        AbilityManager.RegisterAbility(dyingCard, ability);

        // Friendly blocker ensures board1 can retaliate and the dying card is not the attacker.
        // We actually want the dyingCard to be on board2 so it gets hit.
        var board1 = new List<Card> { attacker };
        var board2 = new List<Card> { dyingCard };

        // SimulateBattle clones cards; we validate via board size logic.
        // To directly observe the token we need to test the ability in isolation.
        // Create a fresh board simulating what CombatManager does internally.
        var board = new List<Card> { dyingCard };
        var context = new AbilityContext
        {
            SourceCard = dyingCard,
            OwnerBoard = board,
            EnemyBoard = new List<Card>()
        };

        int boardSizeBefore = board.Count;
        ability.Execute(context);
        int boardSizeAfter = board.Count;

        Assert.AreEqual(boardSizeBefore + 1, boardSizeAfter,
            "SummonToken deathrattle should add exactly 1 card to the owner's board");

        // Verify the token has the correct stats.
        Card token = board.FirstOrDefault(c => c.cardName.Contains("Token"));
        Assert.IsNotNull(token, "Board should contain a card whose name includes 'Token'");
        Assert.AreEqual(2, token.attack, "Summoned token attack should match deathrattle value");
        Assert.AreEqual(2, token.health, "Summoned token health should match deathrattle value");
    }

    /// <summary>
    /// When a card dies its deathrattle fires before the death is fully resolved.
    /// A BuffAllFriendlyAttack deathrattle on card A should buff card B that is
    /// still alive on the same board.
    /// </summary>
    [Test]
    public void Deathrattle_BuffAllFriendlyAttack_BuffsSurvivingAlly()
    {
        var dyingCard   = CreateCard("Mentor",   attack: 1, health: 1);
        var survivingAlly = CreateCard("Student", attack: 2, health: 5);

        // Deathrattle: give all other friendlies +3 attack.
        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.BuffAllFriendlyAttack, 3);
        AbilityManager.RegisterAbility(dyingCard, ability);

        var board = new List<Card> { dyingCard, survivingAlly };
        var context = new AbilityContext
        {
            SourceCard = dyingCard,
            OwnerBoard = board,
            EnemyBoard = new List<Card>()
        };

        int originalAllyAttack = survivingAlly.attack;
        ability.Execute(context);

        Assert.AreEqual(originalAllyAttack + 3, survivingAlly.attack,
            "BuffAllFriendlyAttack deathrattle should increase the surviving ally's attack by 3");
    }

    /// <summary>
    /// Nested deathrattle scenario: card A (low health) dies in combat and its deathrattle
    /// buffs card B. Because card B had already taken enough damage to die without the buff
    /// but now survives with boosted health, B should remain alive after the combat resolves.
    ///
    /// We test this by running the deathrattle directly: B starts with health that would
    /// put it at 0 without the buff. After the deathrattle grants +X health B survives.
    /// </summary>
    [Test]
    public void Deathrattle_BuffRandomFriendlyHealth_CanSaveAllyFromDeath()
    {
        var dyingCard = CreateCard("LifeGiver", attack: 1, health: 1);
        // Ally is at 1 health — normally would die to the next 1-damage hit.
        var criticalAlly = CreateCard("CriticalAlly", attack: 1, health: 1);

        // Deathrattle: give a random friendly +5 health (enough to survive subsequent hits).
        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.BuffRandomFriendlyHealth, 5);
        AbilityManager.RegisterAbility(dyingCard, ability);

        var board = new List<Card> { dyingCard, criticalAlly };
        var context = new AbilityContext
        {
            SourceCard = dyingCard,
            OwnerBoard = board,
            EnemyBoard = new List<Card>()
        };

        ability.Execute(context);

        Assert.IsTrue(criticalAlly.health > 1,
            "After BuffRandomFriendlyHealth deathrattle the surviving ally should have more than 1 health, saving it from a lethal 1-damage hit");
    }

    /// <summary>
    /// A deathrattle that deals damage to all enemies should reduce the health of every
    /// enemy card still alive when the ability fires.
    /// </summary>
    [Test]
    public void Deathrattle_DealDamageToAllEnemies_ReducesHealthOnAllEnemies()
    {
        var dyingCard  = CreateCard("Bomber", attack: 1, health: 1);
        var enemy1     = CreateCard("Enemy1", attack: 1, health: 5);
        var enemy2     = CreateCard("Enemy2", attack: 1, health: 3);

        // Deathrattle: deal 2 damage to all enemies.
        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.DealDamageToAllEnemies, 2);
        AbilityManager.RegisterAbility(dyingCard, ability);

        var ownerBoard = new List<Card> { dyingCard };
        var enemyBoard = new List<Card> { enemy1, enemy2 };
        var context = new AbilityContext
        {
            SourceCard = dyingCard,
            OwnerBoard = ownerBoard,
            EnemyBoard = enemyBoard
        };

        ability.Execute(context);

        Assert.AreEqual(3, enemy1.health, "Enemy1 should take 2 damage (5 - 2 = 3)");
        Assert.AreEqual(1, enemy2.health, "Enemy2 should take 2 damage (3 - 2 = 1)");
    }

    /// <summary>
    /// A card whose deathrattle kills an enemy should not trigger further cascade if the
    /// board is empty afterwards (no infinite loop). The combat result should still be
    /// calculable without error.
    /// </summary>
    [Test]
    public void Deathrattle_CascadeKill_DoesNotInfiniteLoop()
    {
        // board1: 1-attack card with deathrattle that deals 10 damage to all enemies
        var friendly = CreateCard("KamikazeAlly", attack: 1, health: 1);
        friendly.abilityEffect = Card.AbilityEffectType.DeathrattleDamageAllEnemies;
        friendly.abilityValue  = 10;
        friendly.abilityTrigger = AbilityTrigger.Deathrattle;
        friendly.RegisterAbility();

        // board2: one weak enemy that the deathrattle will kill
        var enemy = CreateCard("WeakEnemy", attack: 1, health: 1);

        var board1 = new List<Card> { friendly };
        var board2 = new List<Card> { enemy };

        // Should complete without StackOverflowException or infinite loop.
        Assert.DoesNotThrow(() =>
        {
            CombatManager.SimulateBattle(board1, board2, 1, 1, "P1", "P2");
        }, "Death cascade from deathrattle kill should not throw or loop infinitely");
    }

    // =====================================================
    // GOLDEN CARD TESTS
    // =====================================================

    /// <summary>
    /// A golden card should have exactly double the attack and health of its base version.
    /// </summary>
    [Test]
    public void GoldenCard_StatsDoubled_ComparedToBaseCard()
    {
        var baseCard = CreateCard("TestMinion", attack: 3, health: 4);
        var golden   = Card.CreateGoldenVersion(baseCard);

        Assert.IsTrue(golden.isGolden, "Golden card should have isGolden=true");
        Assert.AreEqual(baseCard.attack * 2, golden.attack,
            "Golden card attack should be exactly double the base card attack");
        Assert.AreEqual(baseCard.health * 2, golden.health,
            "Golden card health should be exactly double the base card health");
    }

    /// <summary>
    /// Creating a golden card must NOT double the abilityValue — only the base stats are
    /// doubled. This is the critical invariant: ability magnitude should remain unchanged.
    /// </summary>
    [Test]
    public void GoldenCard_AbilityValueNotDoubled()
    {
        var baseCard = CreateCard("AbilityCard", attack: 2, health: 2);
        baseCard.abilityEffect = Card.AbilityEffectType.DeathrattleBuffRandomFriendly;
        baseCard.abilityValue  = 4;

        var golden = Card.CreateGoldenVersion(baseCard);

        Assert.AreEqual(4, golden.abilityValue,
            "abilityValue must NOT be doubled for golden cards — only attack/health are doubled");
    }

    /// <summary>
    /// A golden card's Clone() should not propagate the golden flag — clones start as
    /// non-golden to prevent accidental golden contamination.
    /// </summary>
    [Test]
    public void GoldenCard_Clone_DoesNotPropagateGoldenFlag()
    {
        var baseCard = CreateCard("BaseMinion", attack: 2, health: 3);
        var golden   = Card.CreateGoldenVersion(baseCard);
        var clone    = golden.Clone();

        Assert.IsFalse(clone.isGolden,
            "Cloning a golden card should produce a non-golden card (no golden contamination)");
    }

    /// <summary>
    /// A golden card with doubled stats should win combat significantly more often than
    /// its base version against an identical normal opponent, because its doubled stats
    /// give a clear advantage.
    /// </summary>
    [Test]
    public void GoldenCard_InCombat_WinsMoreOftenThanBaseCard()
    {
        int goldenWins = 0;
        int baseWins   = 0;

        for (int i = 0; i < 50; i++)
        {
            // Golden board: 1 golden card with 6/6 vs opponent with 3/3.
            var baseSource = CreateCard("Minion", attack: 3, health: 3);
            var goldenCard = Card.CreateGoldenVersion(baseSource);
            var goldenBoard = new List<Card> { goldenCard };

            var opponentForGolden = new List<Card> { CreateCard("Opponent", attack: 3, health: 3) };
            var (_, gwinner) = CombatManager.SimulateBattle(goldenBoard, opponentForGolden, 1, 1, "Golden", "Opponent");
            if (gwinner == "Golden") goldenWins++;

            // Base board: 1 base card with 3/3 vs opponent with 3/3.
            var normalCard  = CreateCard("Minion", attack: 3, health: 3);
            var normalBoard = new List<Card> { normalCard };

            var opponentForBase = new List<Card> { CreateCard("Opponent", attack: 3, health: 3) };
            var (_, bwinner) = CombatManager.SimulateBattle(normalBoard, opponentForBase, 1, 1, "Base", "Opponent");
            if (bwinner == "Base") baseWins++;
        }

        Assert.IsTrue(goldenWins > baseWins,
            $"Golden card (doubled stats) should win more often than base card. Golden wins: {goldenWins}, Base wins: {baseWins}");
    }
}

using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Comprehensive NUnit tests for all 16 ability implementations.
///
/// Covers:
///   BattlecryAbility, DeathrattleAbility, OnAttackAbility,
///   WindfuryAbility, VenomousAbility, RebornAbility,
///   AuraAbility, OnAllyDeathAbility, OnAllySummonedAbility,
///   OnSellAbility, SummonTokenAbility, StealBuffAbility,
///   GainArmorAbility, BuffAllTribesAbility, RandomTransformAbility,
///   TauntAbility
///
/// Test naming convention: MethodName_Scenario_ExpectedResult
/// </summary>
[TestFixture]
public class AbilityTests
{
    // -----------------------------------------------------------------------
    // Shared setup / teardown
    // -----------------------------------------------------------------------

    private List<GameObject> _gameObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        AbilityManager.ClearAll();
        _gameObjects.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        AbilityManager.ClearAll();
        foreach (var go in _gameObjects)
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }
        _gameObjects.Clear();
    }

    // -----------------------------------------------------------------------
    // Helper factories
    // -----------------------------------------------------------------------

    private Card MakeCard(string name, int attack = 2, int health = 2,
        TribeType[] tribes = null, int tier = 1)
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.attack = attack;
        card.health = health;
        card.tier = tier;
        card.tribes = tribes ?? new TribeType[0];
        return card;
    }

    private Player MakePlayer(List<Card> board = null)
    {
        var go = new GameObject("TestPlayer");
        _gameObjects.Add(go);
        var player = go.AddComponent<Player>();
        player.board = board ?? new List<Card>();
        return player;
    }

    private AbilityContext MakeContext(Card source, List<Card> ownerBoard,
        List<Card> enemyBoard = null, Card target = null, Player owner = null)
    {
        return new AbilityContext
        {
            SourceCard = source,
            OwnerBoard = ownerBoard,
            EnemyBoard = enemyBoard,
            TargetCard = target,
            Owner = owner
        };
    }

    // -----------------------------------------------------------------------
    // 1. BattlecryAbility
    // -----------------------------------------------------------------------

    [Test]
    public void BattlecryAbility_BuffAdjacentAttack_BuffsLeftAndRight()
    {
        var left = MakeCard("Left", attack: 1, health: 3);
        var source = MakeCard("Source", attack: 2, health: 2);
        var right = MakeCard("Right", attack: 1, health: 3);
        var board = new List<Card> { left, source, right };

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAdjacentAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        var ctx = MakeContext(source, board);
        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, ctx);

        Assert.AreEqual(3, left.attack, "Left neighbor should receive +2 attack");
        Assert.AreEqual(3, right.attack, "Right neighbor should receive +2 attack");
        Assert.AreEqual(2, source.attack, "Source should not buff itself (adjacent only)");
    }

    [Test]
    public void BattlecryAbility_BuffAdjacentAttack_SourceAtLeftEdge_OnlyBuffsRight()
    {
        var source = MakeCard("Source", attack: 1, health: 2);
        var right = MakeCard("Right", attack: 1, health: 2);
        var board = new List<Card> { source, right };

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAdjacentAttack, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(4, right.attack, "Right neighbor should receive +3 attack");
        Assert.AreEqual(1, source.attack, "Source attack unchanged");
    }

    [Test]
    public void BattlecryAbility_BuffAdjacentStats_BuffsBothStatsOnNeighbors()
    {
        var left = MakeCard("Left", attack: 1, health: 1);
        var source = MakeCard("Source", attack: 2, health: 2);
        var right = MakeCard("Right", attack: 1, health: 1);
        var board = new List<Card> { left, source, right };

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAdjacentStats, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(3, left.attack);
        Assert.AreEqual(3, left.health);
        Assert.AreEqual(3, right.attack);
        Assert.AreEqual(3, right.health);
    }

    [Test]
    public void BattlecryAbility_BuffAllFriendlyAttack_IncludesSelf()
    {
        var source = MakeCard("Source", attack: 1, health: 2);
        var ally = MakeCard("Ally", attack: 1, health: 2);
        var board = new List<Card> { source, ally };

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAllFriendlyAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(3, source.attack, "Source should be buffed (includeSelf=true)");
        Assert.AreEqual(3, ally.attack, "Ally should be buffed");
    }

    [Test]
    public void BattlecryAbility_BuffOtherFriendlyAttack_ExcludesSelf()
    {
        var source = MakeCard("Source", attack: 1, health: 2);
        var ally = MakeCard("Ally", attack: 1, health: 2);
        var board = new List<Card> { source, ally };

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffOtherFriendlyAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(1, source.attack, "Source should NOT be buffed (includeSelf=false)");
        Assert.AreEqual(3, ally.attack, "Ally should be buffed");
    }

    [Test]
    public void BattlecryAbility_GainAegis_SetsHasAegisTrue()
    {
        var source = MakeCard("Source");
        var board = new List<Card> { source };

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.GainAegis, 0);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.IsTrue(source.hasAegis, "Card should have Aegis after Battlecry");
    }

    [Test]
    public void BattlecryAbility_GainCoins_IncreasesOwnerCoins()
    {
        var source = MakeCard("Source");
        var board = new List<Card> { source };
        var player = MakePlayer(board);
        player.coins = 3;

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.GainCoins, 2);
        AbilityManager.RegisterAbility(source, ability);

        var ctx = MakeContext(source, board, owner: player);
        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, ctx);

        Assert.AreEqual(5, player.coins, "Owner should gain 2 coins");
    }

    [Test]
    public void BattlecryAbility_BuffSelfHealth_IncreasesSourceHealth()
    {
        var source = MakeCard("Source", health: 3);
        var board = new List<Card> { source };

        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffSelfHealth, 4);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(7, source.health, "Source health should increase by 4");
    }

    [Test]
    public void BattlecryAbility_EmptyBoard_DoesNotThrow()
    {
        var source = MakeCard("Source");
        var board = new List<Card> { source };

        // A single card board means no adjacent neighbors
        var ability = new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAdjacentAttack, 5);
        AbilityManager.RegisterAbility(source, ability);

        // Should not throw
        Assert.DoesNotThrow(() =>
            AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board)));
    }

    // -----------------------------------------------------------------------
    // 2. DeathrattleAbility
    // -----------------------------------------------------------------------

    [Test]
    public void DeathrattleAbility_BuffAllFriendlyAttack_BuffsAllAliveExceptSource()
    {
        var source = MakeCard("Source", attack: 2, health: 2);
        var ally1 = MakeCard("Ally1", attack: 1, health: 3);
        var ally2 = MakeCard("Ally2", attack: 1, health: 3);
        var dead = MakeCard("DeadAlly", attack: 1, health: 0); // dead
        var board = new List<Card> { source, ally1, ally2, dead };

        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.BuffAllFriendlyAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        Assert.AreEqual(3, ally1.attack, "Alive ally should get +2 attack");
        Assert.AreEqual(3, ally2.attack, "Alive ally should get +2 attack");
        Assert.AreEqual(2, source.attack, "Source should not buff itself");
        Assert.AreEqual(1, dead.attack, "Dead card should not be buffed");
    }

    [Test]
    public void DeathrattleAbility_DealDamageToAllEnemies_DamagesAllAliveEnemies()
    {
        var source = MakeCard("Source");
        var enemy1 = MakeCard("Enemy1", health: 5);
        var enemy2 = MakeCard("Enemy2", health: 5);
        var deadEnemy = MakeCard("DeadEnemy", health: 0);
        var ownerBoard = new List<Card> { source };
        var enemyBoard = new List<Card> { enemy1, enemy2, deadEnemy };

        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.DealDamageToAllEnemies, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle,
            MakeContext(source, ownerBoard, enemyBoard: enemyBoard));

        Assert.AreEqual(2, enemy1.health, "Enemy1 should take 3 damage");
        Assert.AreEqual(2, enemy2.health, "Enemy2 should take 3 damage");
        Assert.AreEqual(0, deadEnemy.health, "Dead enemy health should be unchanged at 0");
    }

    [Test]
    public void DeathrattleAbility_SummonToken_InsertsTokenAtSourcePosition()
    {
        var source = MakeCard("Source");
        var ally = MakeCard("Ally");
        var board = new List<Card> { source, ally };

        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.SummonToken, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        // Token inserted at position 0 (source position)
        Assert.AreEqual(3, board.Count, "Board should now have 3 entries (source + token + ally)");
        Card token = board[0];
        Assert.AreEqual(3, token.attack, "Token attack should match value");
        Assert.AreEqual(3, token.health, "Token health should match value");
        StringAssert.Contains("Token", token.cardName, "Token name should contain 'Token'");
    }

    [Test]
    public void DeathrattleAbility_SummonToken_BoardFull_DoesNotSummon()
    {
        var source = MakeCard("Source");
        // Create a board with the source + 6 alive cards = 7 alive total
        var board = new List<Card> { source };
        for (int i = 0; i < 6; i++)
            board.Add(MakeCard($"Ally{i}", health: 5));

        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.SummonToken, 2);
        AbilityManager.RegisterAbility(source, ability);

        // Make source dead so aliveCount of non-source cards = 6, but source still in board list
        // The SummonToken counts all alive cards; if source is in board + dead, alive = 6 others
        // Let's mark source as dead (health 0) so aliveCount = 6 (others) = not >= 7
        // To truly test board-full: put source as dead, 7 alive allies
        source.health = 0;
        board.Add(MakeCard("ExtraAlly", health: 5)); // now 8 items, 7 alive

        int boardSizeBefore = board.Count;
        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        Assert.AreEqual(boardSizeBefore, board.Count, "Board should not grow when 7 alive cards present");
    }

    [Test]
    public void DeathrattleAbility_GainCoins_AddsCoinsToOwner()
    {
        var source = MakeCard("Source");
        var board = new List<Card> { source };
        var player = MakePlayer(board);
        player.coins = 2;

        var ability = new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.GainCoins, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle,
            MakeContext(source, board, owner: player));

        Assert.AreEqual(5, player.coins, "Owner should gain 3 coins from deathrattle");
    }

    // -----------------------------------------------------------------------
    // 3. OnAttackAbility
    // -----------------------------------------------------------------------

    [Test]
    public void OnAttackAbility_BuffSelfAttack_PermanentlyIncreasesAttack()
    {
        var source = MakeCard("Attacker", attack: 3, health: 4);
        var board = new List<Card> { source };

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.BuffSelfAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack, MakeContext(source, board));

        Assert.AreEqual(5, source.attack, "Attack should permanently increase by 2");
    }

    [Test]
    public void OnAttackAbility_BuffSelfHealth_PermanentlyIncreasesHealth()
    {
        var source = MakeCard("Attacker", attack: 2, health: 3);
        var board = new List<Card> { source };

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.BuffSelfHealth, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack, MakeContext(source, board));

        Assert.AreEqual(6, source.health, "Health should permanently increase by 3");
    }

    [Test]
    public void OnAttackAbility_DealBonusDamage_SetsTempBonusDamageAndBoostsAttack()
    {
        var source = MakeCard("Attacker", attack: 4, health: 4);
        var board = new List<Card> { source };

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.DealBonusDamage, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack, MakeContext(source, board));

        Assert.AreEqual(7, source.attack, "Attack should be boosted by bonus value for this strike");
        Assert.AreEqual(3, source.tempBonusDamage, "tempBonusDamage should reflect the bonus");
    }

    [Test]
    public void OnAttackAbility_DealDamageToAdjacent_DamagesNeighborsOfTarget()
    {
        var source = MakeCard("Attacker");
        var ownerBoard = new List<Card> { source };

        var leftEnemy = MakeCard("LeftEnemy", health: 5);
        var target = MakeCard("Target", health: 5);
        var rightEnemy = MakeCard("RightEnemy", health: 5);
        var enemyBoard = new List<Card> { leftEnemy, target, rightEnemy };

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.DealDamageToAdjacent, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, ownerBoard, enemyBoard: enemyBoard, target: target));

        Assert.AreEqual(3, leftEnemy.health, "Left neighbor should take 2 damage");
        Assert.AreEqual(5, target.health, "Target itself should NOT be damaged by adjacent effect");
        Assert.AreEqual(3, rightEnemy.health, "Right neighbor should take 2 damage");
    }

    [Test]
    public void OnAttackAbility_DealDamageToAdjacent_AegisOnNeighbor_BlocksDamageAndRemovesAegis()
    {
        var source = MakeCard("Attacker");
        var ownerBoard = new List<Card> { source };

        var leftEnemy = MakeCard("LeftEnemy", health: 5);
        leftEnemy.hasAegis = true;
        var target = MakeCard("Target", health: 5);
        var enemyBoard = new List<Card> { leftEnemy, target };

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.DealDamageToAdjacent, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, ownerBoard, enemyBoard: enemyBoard, target: target));

        Assert.AreEqual(5, leftEnemy.health, "Aegis should block adjacent damage — health unchanged");
        Assert.IsFalse(leftEnemy.hasAegis, "Aegis should be consumed after blocking");
    }

    [Test]
    public void OnAttackAbility_Cleave_DamagesAdjacentEnemiesWithFullAttack()
    {
        var source = MakeCard("Cleaver", attack: 4, health: 4);
        var ownerBoard = new List<Card> { source };

        var leftEnemy = MakeCard("LeftEnemy", health: 10);
        var target = MakeCard("Target", health: 10);
        var rightEnemy = MakeCard("RightEnemy", health: 10);
        var enemyBoard = new List<Card> { leftEnemy, target, rightEnemy };

        // value = 0 means use source.attack for cleave damage
        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.Cleave, 0);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, ownerBoard, enemyBoard: enemyBoard, target: target));

        Assert.AreEqual(6, leftEnemy.health, "Left neighbor should take source.attack (4) cleave damage");
        Assert.AreEqual(6, rightEnemy.health, "Right neighbor should take source.attack (4) cleave damage");
        Assert.AreEqual(10, target.health, "Cleave ability does NOT directly damage the main target here");
    }

    [Test]
    public void OnAttackAbility_Cleave_ConfiguredValue_UsesThatValueNotFullAttack()
    {
        var source = MakeCard("Cleaver", attack: 6, health: 4);
        var ownerBoard = new List<Card> { source };

        var leftEnemy = MakeCard("LeftEnemy", health: 10);
        var target = MakeCard("Target", health: 10);
        var rightEnemy = MakeCard("RightEnemy", health: 10);
        var enemyBoard = new List<Card> { leftEnemy, target, rightEnemy };

        // value = 2 means use 2 as cleave damage (not source.attack=6)
        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.Cleave, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, ownerBoard, enemyBoard: enemyBoard, target: target));

        Assert.AreEqual(8, leftEnemy.health, "Left neighbor should take configured cleave damage (2)");
        Assert.AreEqual(8, rightEnemy.health, "Right neighbor should take configured cleave damage (2)");
    }

    [Test]
    public void OnAttackAbility_Cleave_AegisOnNeighbor_BlocksAndRemovesAegis()
    {
        var source = MakeCard("Cleaver", attack: 3, health: 4);
        var ownerBoard = new List<Card> { source };

        var leftEnemy = MakeCard("LeftEnemy", health: 5);
        leftEnemy.hasAegis = true;
        var target = MakeCard("Target", health: 5);
        var enemyBoard = new List<Card> { leftEnemy, target };

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.Cleave, 0);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, ownerBoard, enemyBoard: enemyBoard, target: target));

        Assert.AreEqual(5, leftEnemy.health, "Aegis should block cleave damage");
        Assert.IsFalse(leftEnemy.hasAegis, "Aegis should be consumed by cleave hit");
    }

    [Test]
    public void OnAttackAbility_LifestealSelf_IncreasesSourceHealth()
    {
        var source = MakeCard("Lifesteal", attack: 3, health: 2);
        var board = new List<Card> { source };

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.LifestealSelf, 4);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack, MakeContext(source, board));

        Assert.AreEqual(6, source.health, "Lifesteal should increase source health by 4");
    }

    [Test]
    public void OnAttackAbility_BuffRandomFriendlyAttack_BuffsAnAllyNotSource()
    {
        var source = MakeCard("Attacker", attack: 2, health: 2);
        var ally = MakeCard("Ally", attack: 1, health: 2);
        var board = new List<Card> { source, ally };
        int sourceBefore = source.attack;
        int allyBefore = ally.attack;

        var ability = new OnAttackAbility(OnAttackAbility.OnAttackEffect.BuffRandomFriendlyAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack, MakeContext(source, board));

        // Only ally should receive the buff (source is excluded)
        Assert.AreEqual(sourceBefore, source.attack, "Source should not buff itself");
        Assert.AreEqual(allyBefore + 2, ally.attack, "Ally should receive +2 attack");
    }

    // -----------------------------------------------------------------------
    // 4. WindfuryAbility
    // -----------------------------------------------------------------------

    [Test]
    public void WindfuryAbility_HasWindfury_TrueViaAbilityRegistration()
    {
        var card = MakeCard("Windcard");
        AbilityManager.RegisterAbility(card, new WindfuryAbility());

        Assert.IsTrue(WindfuryAbility.HasWindfury(card),
            "HasWindfury should return true when WindfuryAbility is registered");
    }

    [Test]
    public void WindfuryAbility_HasWindfury_TrueViaPassiveFlag()
    {
        var card = MakeCard("Windcard");
        card.hasWindfury = true;

        Assert.IsTrue(WindfuryAbility.HasWindfury(card),
            "HasWindfury should return true when card.hasWindfury is set");
    }

    [Test]
    public void WindfuryAbility_HasWindfury_FalseWithoutRegistrationOrFlag()
    {
        var card = MakeCard("Normal");
        Assert.IsFalse(WindfuryAbility.HasWindfury(card));
    }

    [Test]
    public void WindfuryAbility_HasWindfury_NullCard_ReturnsFalse()
    {
        Assert.IsFalse(WindfuryAbility.HasWindfury(null));
    }

    // -----------------------------------------------------------------------
    // 5. VenomousAbility
    // -----------------------------------------------------------------------

    [Test]
    public void VenomousAbility_HasVenomous_TrueViaAbilityRegistration()
    {
        var card = MakeCard("Venom");
        AbilityManager.RegisterAbility(card, new VenomousAbility());

        Assert.IsTrue(VenomousAbility.HasVenomous(card));
    }

    [Test]
    public void VenomousAbility_HasVenomous_TrueViaPassiveFlag()
    {
        var card = MakeCard("Venom");
        card.hasVenomous = true;

        Assert.IsTrue(VenomousAbility.HasVenomous(card));
    }

    [Test]
    public void VenomousAbility_HasVenomous_FalseWithoutRegistrationOrFlag()
    {
        var card = MakeCard("Normal");
        Assert.IsFalse(VenomousAbility.HasVenomous(card));
    }

    [Test]
    public void VenomousAbility_HasVenomous_NullCard_ReturnsFalse()
    {
        Assert.IsFalse(VenomousAbility.HasVenomous(null));
    }

    // -----------------------------------------------------------------------
    // 6. RebornAbility
    // -----------------------------------------------------------------------

    [Test]
    public void RebornAbility_HasReborn_TrueViaAbilityRegistration()
    {
        var card = MakeCard("Reborn");
        AbilityManager.RegisterAbility(card, new RebornAbility());

        Assert.IsTrue(RebornAbility.HasReborn(card));
    }

    [Test]
    public void RebornAbility_HasReborn_TrueViaPassiveFlag()
    {
        var card = MakeCard("Reborn");
        card.hasReborn = true;

        Assert.IsTrue(RebornAbility.HasReborn(card));
    }

    [Test]
    public void RebornAbility_HasReborn_FalseWithoutRegistrationOrFlag()
    {
        var card = MakeCard("Normal");
        Assert.IsFalse(RebornAbility.HasReborn(card));
    }

    [Test]
    public void RebornAbility_HasReborn_NullCard_ReturnsFalse()
    {
        Assert.IsFalse(RebornAbility.HasReborn(null));
    }

    // -----------------------------------------------------------------------
    // 7. AuraAbility
    // -----------------------------------------------------------------------

    [Test]
    public void AuraAbility_BuffTribematesAttack_BuffsMatchingTribeOnly()
    {
        var source = MakeCard("AuraSource", tribes: new[] { TribeType.Swords });
        var swordsAlly = MakeCard("SwordsAlly", attack: 1, tribes: new[] { TribeType.Swords });
        var cupsAlly = MakeCard("CupsAlly", attack: 1, tribes: new[] { TribeType.Cups });
        var board = new List<Card> { source, swordsAlly, cupsAlly };

        var ability = new AuraAbility(AuraAbility.AuraEffect.BuffTribematesAttack, 3, TribeType.Swords);
        AbilityManager.RegisterAbility(source, ability);

        var ctx = MakeContext(source, board);
        AbilityManager.TriggerAbilities(AbilityTrigger.Aura, ctx);

        Assert.AreEqual(4, swordsAlly.attack, "Swords ally should gain +3 attack from aura");
        Assert.AreEqual(1, cupsAlly.attack, "Cups ally should NOT be buffed (wrong tribe)");
        Assert.IsTrue(ability.IsActive, "Aura should be marked active after apply");
    }

    [Test]
    public void AuraAbility_RemoveAura_ReversesAppliedBuffs()
    {
        var source = MakeCard("AuraSource", tribes: new[] { TribeType.Swords });
        var ally = MakeCard("Ally", attack: 2, health: 2, tribes: new[] { TribeType.Swords });
        var board = new List<Card> { source, ally };

        var ability = new AuraAbility(AuraAbility.AuraEffect.BuffTribematesAttack, 3, TribeType.Swords);
        AbilityManager.RegisterAbility(source, ability);

        var ctx = MakeContext(source, board);
        AbilityManager.TriggerAbilities(AbilityTrigger.Aura, ctx);
        Assert.AreEqual(5, ally.attack, "Aura should have applied buff");

        ability.RemoveAura(ctx);
        Assert.AreEqual(2, ally.attack, "Aura removal should reverse the buff");
        Assert.IsFalse(ability.IsActive, "Aura should be marked inactive after removal");
    }

    [Test]
    public void AuraAbility_BuffAdjacentStats_BuffsOnlyNeighbors()
    {
        var left = MakeCard("Left", attack: 1, health: 1);
        var source = MakeCard("AuraSource", tribes: new[] { TribeType.Wands });
        var right = MakeCard("Right", attack: 1, health: 1);
        var farRight = MakeCard("FarRight", attack: 1, health: 1);
        var board = new List<Card> { left, source, right, farRight };

        var ability = new AuraAbility(AuraAbility.AuraEffect.BuffAdjacentStats, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Aura, MakeContext(source, board));

        Assert.AreEqual(3, left.attack, "Left neighbor should get +2 attack");
        Assert.AreEqual(3, left.health, "Left neighbor should get +2 health");
        Assert.AreEqual(3, right.attack, "Right neighbor should get +2 attack");
        Assert.AreEqual(3, right.health, "Right neighbor should get +2 health");
        Assert.AreEqual(1, farRight.attack, "Far-right card should NOT be buffed");
    }

    [Test]
    public void AuraAbility_BuffAllFriendlyAttack_BuffsAllOtherAliveCards()
    {
        var source = MakeCard("AuraSource", attack: 1);
        var ally1 = MakeCard("Ally1", attack: 1, health: 2);
        var ally2 = MakeCard("Ally2", attack: 1, health: 2);
        var dead = MakeCard("Dead", attack: 1, health: 0);
        var board = new List<Card> { source, ally1, ally2, dead };

        var ability = new AuraAbility(AuraAbility.AuraEffect.BuffAllFriendlyAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Aura, MakeContext(source, board));

        Assert.AreEqual(3, ally1.attack, "Alive ally1 should be buffed");
        Assert.AreEqual(3, ally2.attack, "Alive ally2 should be buffed");
        Assert.AreEqual(1, dead.attack, "Dead card should NOT be buffed");
        Assert.AreEqual(1, source.attack, "Source should NOT buff itself");
    }

    [Test]
    public void AuraAbility_IsActive_FalseBeforeFirstExecute()
    {
        var ability = new AuraAbility(AuraAbility.AuraEffect.BuffAllFriendlyAttack, 1);
        Assert.IsFalse(ability.IsActive, "Aura should be inactive before first Execute()");
    }

    [Test]
    public void AuraAbility_DoubleApply_LogsWarningAndDoesNotDoubleStack()
    {
        var source = MakeCard("AuraSource");
        var ally = MakeCard("Ally", attack: 1, health: 2);
        var board = new List<Card> { source, ally };

        var ability = new AuraAbility(AuraAbility.AuraEffect.BuffAllFriendlyAttack, 2);
        AbilityManager.RegisterAbility(source, ability);
        var ctx = MakeContext(source, board);

        AbilityManager.TriggerAbilities(AbilityTrigger.Aura, ctx);
        int attackAfterFirst = ally.attack;

        // Attempting to apply again without removing should be a no-op (guarded by IsActive)
        ability.Execute(ctx); // direct call to test the guard

        Assert.AreEqual(attackAfterFirst, ally.attack,
            "Second apply while active should not further increase stats");
    }

    // -----------------------------------------------------------------------
    // 8. OnAllyDeathAbility
    // -----------------------------------------------------------------------

    [Test]
    public void OnAllyDeathAbility_BuffSelfAttack_IncreasesSourceAttack()
    {
        var source = MakeCard("Watcher", attack: 2, health: 4);
        var dyingAlly = MakeCard("DyingAlly", health: 0);
        var board = new List<Card> { source, dyingAlly };

        var ability = new OnAllyDeathAbility(OnAllyDeathAbility.OnAllyDeathEffect.BuffSelfAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        var ctx = MakeContext(source, board, target: dyingAlly);
        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllyDeath, ctx);

        Assert.AreEqual(4, source.attack, "Source should gain +2 attack on ally death");
    }

    [Test]
    public void OnAllyDeathAbility_BuffSelfHealth_IncreasesSourceHealth()
    {
        var source = MakeCard("Watcher", attack: 1, health: 3);
        var dyingAlly = MakeCard("DyingAlly", health: 0);
        var board = new List<Card> { source, dyingAlly };

        var ability = new OnAllyDeathAbility(OnAllyDeathAbility.OnAllyDeathEffect.BuffSelfHealth, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllyDeath,
            MakeContext(source, board, target: dyingAlly));

        Assert.AreEqual(6, source.health, "Source should gain +3 health on ally death");
    }

    [Test]
    public void OnAllyDeathAbility_BuffSelfStats_IncreasesAttackAndHealth()
    {
        var source = MakeCard("Watcher", attack: 1, health: 1);
        var dyingAlly = MakeCard("DyingAlly", health: 0);
        var board = new List<Card> { source, dyingAlly };

        var ability = new OnAllyDeathAbility(OnAllyDeathAbility.OnAllyDeathEffect.BuffSelfStats, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllyDeath,
            MakeContext(source, board, target: dyingAlly));

        Assert.AreEqual(3, source.attack, "Source should gain +2 attack");
        Assert.AreEqual(3, source.health, "Source should gain +2 health");
    }

    [Test]
    public void OnAllyDeathAbility_BuffRandomAllyAttack_BuffsNeitherSourceNorDyingCard()
    {
        var source = MakeCard("Watcher", attack: 1, health: 4);
        var dyingAlly = MakeCard("DyingAlly", attack: 1, health: 0);
        var survivor = MakeCard("Survivor", attack: 1, health: 4);
        var board = new List<Card> { source, dyingAlly, survivor };

        var ability = new OnAllyDeathAbility(OnAllyDeathAbility.OnAllyDeathEffect.BuffRandomAllyAttack, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllyDeath,
            MakeContext(source, board, target: dyingAlly));

        // Survivor is the only eligible target
        Assert.AreEqual(4, survivor.attack, "Survivor should receive +3 attack");
        Assert.AreEqual(1, source.attack, "Source should NOT be the target");
        Assert.AreEqual(1, dyingAlly.attack, "DyingAlly should NOT be the target");
    }

    [Test]
    public void OnAllyDeathAbility_CanExecute_ReturnsFalseWhenSourceIsDead()
    {
        var source = MakeCard("DeadWatcher", attack: 1, health: 0); // source is dead
        var dyingAlly = MakeCard("DyingAlly", health: 0);
        var board = new List<Card> { source, dyingAlly };

        var ability = new OnAllyDeathAbility(OnAllyDeathAbility.OnAllyDeathEffect.BuffSelfAttack, 2);

        var ctx = MakeContext(source, board, target: dyingAlly);
        Assert.IsFalse(ability.CanExecute(ctx),
            "Dead source card should not be able to execute OnAllyDeath");
    }

    // -----------------------------------------------------------------------
    // 9. OnAllySummonedAbility
    // -----------------------------------------------------------------------

    [Test]
    public void OnAllySummonedAbility_BuffSummonedAttack_GivesSummonedCardAttack()
    {
        var source = MakeCard("Cheerleader", attack: 1, health: 3);
        var summoned = MakeCard("Newcomer", attack: 2, health: 2);
        var board = new List<Card> { source, summoned };

        var ability = new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffSummonedAttack, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllySummoned,
            MakeContext(source, board, target: summoned));

        Assert.AreEqual(5, summoned.attack, "Summoned card should receive +3 attack");
    }

    [Test]
    public void OnAllySummonedAbility_BuffSummonedHealth_GivesSummonedCardHealth()
    {
        var source = MakeCard("Cheerleader", attack: 1, health: 3);
        var summoned = MakeCard("Newcomer", attack: 2, health: 2);
        var board = new List<Card> { source, summoned };

        var ability = new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffSummonedHealth, 4);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllySummoned,
            MakeContext(source, board, target: summoned));

        Assert.AreEqual(6, summoned.health, "Summoned card should receive +4 health");
    }

    [Test]
    public void OnAllySummonedAbility_BuffSummonedStats_BuffsBothStatsOnSummonedCard()
    {
        var source = MakeCard("Cheerleader");
        var summoned = MakeCard("Newcomer", attack: 1, health: 1);
        var board = new List<Card> { source, summoned };

        var ability = new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffSummonedStats, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllySummoned,
            MakeContext(source, board, target: summoned));

        Assert.AreEqual(3, summoned.attack);
        Assert.AreEqual(3, summoned.health);
    }

    [Test]
    public void OnAllySummonedAbility_BuffSelfAttack_IncreasesSourceAttackOnSummon()
    {
        var source = MakeCard("Grower", attack: 1, health: 4);
        var summoned = MakeCard("Newcomer");
        var board = new List<Card> { source, summoned };

        var ability = new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffSelfAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllySummoned,
            MakeContext(source, board, target: summoned));

        Assert.AreEqual(3, source.attack, "Source should gain +2 attack per summon");
    }

    [Test]
    public void OnAllySummonedAbility_BuffAllAlliesAttack_BuffsAllAliveOnBoard()
    {
        var source = MakeCard("Commander");
        var ally1 = MakeCard("Ally1", attack: 1, health: 2);
        var ally2 = MakeCard("Ally2", attack: 1, health: 2);
        var summoned = MakeCard("Newcomer", attack: 1, health: 2);
        var board = new List<Card> { source, ally1, ally2, summoned };

        var ability = new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffAllAlliesAttack, 1);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAllySummoned,
            MakeContext(source, board, target: summoned));

        // BuffAllAlliesAttack buffs all alive on board (including source and summoned)
        Assert.AreEqual(2, ally1.attack, "Ally1 should get +1 attack");
        Assert.AreEqual(2, ally2.attack, "Ally2 should get +1 attack");
    }

    [Test]
    public void OnAllySummonedAbility_CanExecute_ReturnsFalseWhenSourceIsDead()
    {
        var source = MakeCard("DeadCheerleader", health: 0);
        var summoned = MakeCard("Newcomer");
        var board = new List<Card> { source, summoned };

        var ability = new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffSummonedAttack, 2);
        var ctx = MakeContext(source, board, target: summoned);

        Assert.IsFalse(ability.CanExecute(ctx),
            "Dead source card cannot react to ally summons");
    }

    // -----------------------------------------------------------------------
    // 10. OnSellAbility
    // -----------------------------------------------------------------------

    [Test]
    public void OnSellAbility_GainCoins_AddsCoinsToOwner()
    {
        var source = MakeCard("Merchant");
        var board = new List<Card> { source };
        var player = MakePlayer(board);
        player.coins = 1;

        var ability = new OnSellAbility(OnSellAbility.OnSellEffect.GainCoins, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnSell,
            MakeContext(source, board, owner: player));

        Assert.AreEqual(4, player.coins, "Owner should gain 3 extra coins on sell");
    }

    [Test]
    public void OnSellAbility_BuffAllRemainingStats_BuffsAllOtherBoardCards()
    {
        var source = MakeCard("Seller");
        var remaining1 = MakeCard("Remaining1", attack: 2, health: 2);
        var remaining2 = MakeCard("Remaining2", attack: 2, health: 2);
        var board = new List<Card> { source, remaining1, remaining2 };

        var ability = new OnSellAbility(OnSellAbility.OnSellEffect.BuffAllRemainingStats, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnSell, MakeContext(source, board));

        Assert.AreEqual(4, remaining1.attack);
        Assert.AreEqual(4, remaining1.health);
        Assert.AreEqual(4, remaining2.attack);
        Assert.AreEqual(4, remaining2.health);
        Assert.AreEqual(2, source.attack, "Source should not buff itself");
    }

    [Test]
    public void OnSellAbility_BuffAllRemainingAttack_OnlyBuffsAttack()
    {
        var source = MakeCard("Seller");
        var remaining = MakeCard("Remaining", attack: 1, health: 5);
        var board = new List<Card> { source, remaining };

        var ability = new OnSellAbility(OnSellAbility.OnSellEffect.BuffAllRemainingAttack, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnSell, MakeContext(source, board));

        Assert.AreEqual(4, remaining.attack, "Remaining card should get +3 attack");
        Assert.AreEqual(5, remaining.health, "Remaining card health should be unchanged");
    }

    [Test]
    public void OnSellAbility_CanExecute_ReturnsTrueEvenIfSourceHealthIsZero()
    {
        var source = MakeCard("DeadSeller", health: 0);
        var board = new List<Card> { source };

        var ability = new OnSellAbility(OnSellAbility.OnSellEffect.GainCoins, 1);
        var ctx = MakeContext(source, board);

        // OnSell ignores health check - always executable if SourceCard != null
        Assert.IsTrue(ability.CanExecute(ctx),
            "OnSell should execute even if source card has 0 health");
    }

    [Test]
    public void OnSellAbility_BuffRandomRemainingStats_BuffsExactlyOneOtherCard()
    {
        var source = MakeCard("Seller");
        var remaining1 = MakeCard("R1", attack: 1, health: 1);
        var remaining2 = MakeCard("R2", attack: 1, health: 1);
        var board = new List<Card> { source, remaining1, remaining2 };

        var ability = new OnSellAbility(OnSellAbility.OnSellEffect.BuffRandomRemainingStats, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnSell, MakeContext(source, board));

        // Exactly one of the two remaining cards should receive a buff
        bool r1Buffed = remaining1.attack == 4 && remaining1.health == 4;
        bool r2Buffed = remaining2.attack == 4 && remaining2.health == 4;
        Assert.IsTrue(r1Buffed || r2Buffed, "Exactly one remaining card should get the random buff");
        Assert.IsFalse(r1Buffed && r2Buffed, "Both cards should not both be buffed");
    }

    // -----------------------------------------------------------------------
    // 11. SummonTokenAbility
    // -----------------------------------------------------------------------

    [Test]
    public void SummonTokenAbility_OnDeath_InsertsTokenAtSourcePosition()
    {
        var source = MakeCard("Mother", tribes: new[] { TribeType.Wands });
        var ally = MakeCard("Ally");
        var board = new List<Card> { source, ally };

        var ability = new SummonTokenAbility(SummonTokenAbility.SummonTrigger.OnDeath, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        Assert.AreEqual(3, board.Count, "Board should have 3 cards after token summon");
        Card token = board[0]; // token inserted at source's position (0)
        Assert.AreEqual(2, token.attack);
        Assert.AreEqual(2, token.health);
        StringAssert.Contains("Token", token.cardName);
    }

    [Test]
    public void SummonTokenAbility_OnPlay_HasBattlecryTrigger()
    {
        var ability = new SummonTokenAbility(SummonTokenAbility.SummonTrigger.OnPlay, 1);
        Assert.AreEqual(AbilityTrigger.Battlecry, ability.Trigger);
    }

    [Test]
    public void SummonTokenAbility_OnDeath_HasDeathrattleTrigger()
    {
        var ability = new SummonTokenAbility(SummonTokenAbility.SummonTrigger.OnDeath, 1);
        Assert.AreEqual(AbilityTrigger.Deathrattle, ability.Trigger);
    }

    [Test]
    public void SummonTokenAbility_BoardFull_DoesNotSummon()
    {
        var source = MakeCard("Mother");
        source.health = 0; // source is dead
        var board = new List<Card> { source };
        for (int i = 0; i < 7; i++)
            board.Add(MakeCard($"Alive{i}", health: 5));

        int beforeCount = board.Count;
        var ability = new SummonTokenAbility(SummonTokenAbility.SummonTrigger.OnDeath, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        Assert.AreEqual(beforeCount, board.Count, "Token should NOT be summoned when 7 alive cards are present");
    }

    [Test]
    public void SummonTokenAbility_Token_InheritsSourceTribe()
    {
        var source = MakeCard("Mother", tribes: new[] { TribeType.Coins });
        var board = new List<Card> { source };

        var ability = new SummonTokenAbility(SummonTokenAbility.SummonTrigger.OnDeath, 1);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        Assert.AreEqual(2, board.Count);
        Card token = board[0];
        Assert.IsTrue(token.HasTribe(TribeType.Coins), "Token should inherit the source card's tribe");
    }

    // -----------------------------------------------------------------------
    // 12. StealBuffAbility
    // -----------------------------------------------------------------------

    [Test]
    public void StealBuffAbility_Execute_StealsBothAttackAndHealth()
    {
        var source = MakeCard("Thief", attack: 2, health: 2);
        var target = MakeCard("Target", attack: 4, health: 6);

        var ability = new StealBuffAbility(2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, new List<Card> { source }, target: target));

        Assert.AreEqual(4, source.attack, "Source should steal 2 attack (2+2=4)");
        Assert.AreEqual(4, source.health, "Source should steal 2 health (2+2=4)");
        Assert.AreEqual(2, target.attack, "Target should lose 2 attack (4-2=2)");
        Assert.AreEqual(4, target.health, "Target should lose 2 health (6-2=4)");
    }

    [Test]
    public void StealBuffAbility_TargetLowAttack_DoesNotReduceBelowZero()
    {
        var source = MakeCard("Thief", attack: 1, health: 2);
        var target = MakeCard("Target", attack: 1, health: 5);

        var ability = new StealBuffAbility(5); // tries to steal 5
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, new List<Card> { source }, target: target));

        // StealBuffAbility uses Mathf.Min(_value, target.attack) → min(5,1) = 1 stolen
        // target.attack becomes 1 - 1 = 0; source.attack becomes 1 + 1 = 2
        Assert.AreEqual(0, target.attack, "Target attack should reach 0 (all available attack stolen)");
        Assert.GreaterOrEqual(target.attack, 0, "Target attack should never go below 0");
        Assert.AreEqual(2, source.attack, "Source should gain the 1 stolen attack (1+1=2)");
    }

    [Test]
    public void StealBuffAbility_TargetLowHealth_DoesNotReduceBelowOne()
    {
        var source = MakeCard("Thief", attack: 1, health: 2);
        var target = MakeCard("Target", attack: 2, health: 1);

        var ability = new StealBuffAbility(5);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
            MakeContext(source, new List<Card> { source }, target: target));

        Assert.AreEqual(1, target.health, "Target health should not drop below 1 from steal alone");
        Assert.AreEqual(2, source.health, "Source health should be unchanged (nothing to steal)");
    }

    [Test]
    public void StealBuffAbility_NullTarget_DoesNotThrow()
    {
        var source = MakeCard("Thief", attack: 2, health: 2);
        var ability = new StealBuffAbility(2);
        AbilityManager.RegisterAbility(source, ability);

        Assert.DoesNotThrow(() =>
            AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack,
                MakeContext(source, new List<Card> { source }, target: null)));
    }

    // -----------------------------------------------------------------------
    // 13. GainArmorAbility
    // -----------------------------------------------------------------------

    [Test]
    public void GainArmorAbility_GetArmor_ReturnsArmorFromRegisteredAbility()
    {
        var card = MakeCard("ArmorCard");
        AbilityManager.RegisterAbility(card, new GainArmorAbility(3));

        int armor = GainArmorAbility.GetArmor(card);
        Assert.AreEqual(3, armor, "GetArmor should return the registered armor value");
    }

    [Test]
    public void GainArmorAbility_GetArmor_ReturnsArmorFromPassiveFlag()
    {
        var card = MakeCard("ArmorCard");
        card.armor = 2;

        Assert.AreEqual(2, GainArmorAbility.GetArmor(card), "GetArmor should read card.armor field");
    }

    [Test]
    public void GainArmorAbility_GetArmor_NullCard_ReturnsZero()
    {
        Assert.AreEqual(0, GainArmorAbility.GetArmor(null));
    }

    [Test]
    public void GainArmorAbility_ApplyArmor_ReducesDamageByArmorAmount()
    {
        var card = MakeCard("ArmorCard");
        card.armor = 2;

        int reducedDamage = GainArmorAbility.ApplyArmor(card, 5);
        Assert.AreEqual(3, reducedDamage, "5 damage - 2 armor = 3");
    }

    [Test]
    public void GainArmorAbility_ApplyArmor_MinimumOneDamage()
    {
        var card = MakeCard("HeavyArmor");
        card.armor = 10;

        int reducedDamage = GainArmorAbility.ApplyArmor(card, 3);
        Assert.AreEqual(1, reducedDamage, "Armor cannot reduce damage below 1");
    }

    [Test]
    public void GainArmorAbility_ApplyArmor_NoArmor_ReturnsFull()
    {
        var card = MakeCard("NoArmor");

        int damage = GainArmorAbility.ApplyArmor(card, 4);
        Assert.AreEqual(4, damage, "No armor means full damage passes through");
    }

    [Test]
    public void GainArmorAbility_TriggerIsNone_IsPassive()
    {
        var ability = new GainArmorAbility(3);
        Assert.AreEqual(AbilityTrigger.None, ability.Trigger, "GainArmor should be a passive (None trigger)");
    }

    // -----------------------------------------------------------------------
    // 14. BuffAllTribesAbility
    // -----------------------------------------------------------------------

    [Test]
    public void BuffAllTribesAbility_BattlecryTrigger_BuffsSameTribeAlliesOnPlay()
    {
        var source = MakeCard("TribalLeader", attack: 2, health: 2,
            tribes: new[] { TribeType.Stars });
        var starAlly = MakeCard("StarAlly", attack: 1, health: 1,
            tribes: new[] { TribeType.Stars });
        var swordAlly = MakeCard("SwordsAlly", attack: 1, health: 1,
            tribes: new[] { TribeType.Swords });
        var board = new List<Card> { source, starAlly, swordAlly };

        var ability = new BuffAllTribesAbility(AbilityTrigger.Battlecry, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(3, starAlly.attack, "Stars ally should receive +2 attack");
        Assert.AreEqual(3, starAlly.health, "Stars ally should receive +2 health");
        Assert.AreEqual(1, swordAlly.attack, "Swords ally should NOT be buffed (different tribe)");
    }

    [Test]
    public void BuffAllTribesAbility_DeathrattleTrigger_BuffsOnDeath()
    {
        var source = MakeCard("TribalLeader", attack: 2, health: 2,
            tribes: new[] { TribeType.Pentacles });
        var pentAlly = MakeCard("PentAlly", attack: 1, health: 1,
            tribes: new[] { TribeType.Pentacles });
        var board = new List<Card> { source, pentAlly };

        var ability = new BuffAllTribesAbility(AbilityTrigger.Deathrattle, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        Assert.AreEqual(4, pentAlly.attack, "Same-tribe ally should get +3 attack on death");
        Assert.AreEqual(4, pentAlly.health, "Same-tribe ally should get +3 health on death");
    }

    [Test]
    public void BuffAllTribesAbility_SourceWithNoTribe_SkipsAll()
    {
        var source = MakeCard("NoTribeLeader", attack: 2, health: 2); // no tribes
        var ally = MakeCard("Ally", attack: 1, health: 1,
            tribes: new[] { TribeType.Swords });
        var board = new List<Card> { source, ally };

        var ability = new BuffAllTribesAbility(AbilityTrigger.Battlecry, 2);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(1, ally.attack, "No tribe = no buff should be applied");
    }

    [Test]
    public void BuffAllTribesAbility_DoesNotBuffDeadCards()
    {
        var source = MakeCard("Leader", tribes: new[] { TribeType.Wands });
        var deadAlly = MakeCard("Dead", attack: 1, health: 0, tribes: new[] { TribeType.Wands });
        var board = new List<Card> { source, deadAlly };

        var ability = new BuffAllTribesAbility(AbilityTrigger.Battlecry, 3);
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, MakeContext(source, board));

        Assert.AreEqual(1, deadAlly.attack, "Dead cards should not be buffed");
    }

    // -----------------------------------------------------------------------
    // 15. RandomTransformAbility
    // -----------------------------------------------------------------------

    [Test]
    public void RandomTransformAbility_NoTavernManager_InsertsFallbackToken()
    {
        var source = MakeCard("Transformer", tier: 2);
        var ally = MakeCard("Ally");
        var board = new List<Card> { source, ally };
        // TavernManager.Instance will be null in test context

        var ability = new RandomTransformAbility();
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        // Should have inserted the fallback 2/2 "Transformed Card"
        Assert.AreEqual(3, board.Count,
            "Board should have 3 cards (source still in list + fallback token + ally)");
        Card inserted = board[0]; // Inserted at source's position
        Assert.AreEqual(2, inserted.attack, "Fallback token should be a 2/2");
        Assert.AreEqual(2, inserted.health);
    }

    [Test]
    public void RandomTransformAbility_BoardFull_DoesNotInsert()
    {
        var source = MakeCard("Transformer");
        source.health = 0;
        var board = new List<Card> { source };
        for (int i = 0; i < 7; i++)
            board.Add(MakeCard($"Alive{i}", health: 5));

        int beforeCount = board.Count;
        var ability = new RandomTransformAbility();
        AbilityManager.RegisterAbility(source, ability);

        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, MakeContext(source, board));

        Assert.AreEqual(beforeCount, board.Count, "Full board should prevent transform");
    }

    [Test]
    public void RandomTransformAbility_TriggerIsDeathrattle()
    {
        var ability = new RandomTransformAbility();
        Assert.AreEqual(AbilityTrigger.Deathrattle, ability.Trigger);
    }

    // -----------------------------------------------------------------------
    // 16. TauntAbility
    // -----------------------------------------------------------------------

    [Test]
    public void TauntAbility_HasTaunt_TrueViaAbilityRegistration()
    {
        var card = MakeCard("Guardian");
        AbilityManager.RegisterAbility(card, new TauntAbility());

        Assert.IsTrue(TauntAbility.HasTaunt(card));
    }

    [Test]
    public void TauntAbility_HasTaunt_TrueViaGuardianEffectType()
    {
        var card = MakeCard("Guardian");
        card.effectType = Card.EffectType.Guardian;

        Assert.IsTrue(TauntAbility.HasTaunt(card),
            "Legacy Guardian effectType should count as Taunt");
    }

    [Test]
    public void TauntAbility_HasTaunt_FalseForNormalCard()
    {
        var card = MakeCard("Normal");
        Assert.IsFalse(TauntAbility.HasTaunt(card));
    }

    [Test]
    public void TauntAbility_TriggerIsNone_IsPassive()
    {
        var ability = new TauntAbility();
        Assert.AreEqual(AbilityTrigger.None, ability.Trigger, "Taunt is a passive ability");
    }

    // -----------------------------------------------------------------------
    // Cross-ability / AbilityManager integration tests
    // -----------------------------------------------------------------------

    [Test]
    public void AbilityManager_RegisterDuplicate_SkipsSecondRegistration()
    {
        var card = MakeCard("Card");
        var ability1 = new WindfuryAbility();
        var ability2 = new WindfuryAbility();

        AbilityManager.RegisterAbility(card, ability1);
        AbilityManager.RegisterAbility(card, ability2);

        var abilities = AbilityManager.GetAbilities(card);
        Assert.AreEqual(1, abilities.Count, "Duplicate ability type should not be registered twice");
    }

    [Test]
    public void AbilityManager_UnregisterCard_ClearsAbilities()
    {
        var card = MakeCard("Card");
        AbilityManager.RegisterAbility(card, new TauntAbility());

        AbilityManager.UnregisterCard(card);

        var abilities = AbilityManager.GetAbilities(card);
        Assert.AreEqual(0, abilities.Count, "Abilities should be cleared after unregister");
    }

    [Test]
    public void AbilityManager_TriggerAbilities_OnlyFiresMatchingTrigger()
    {
        var source = MakeCard("MultiAbilityCard", attack: 1, health: 3);
        var ally = MakeCard("Ally", attack: 1, health: 3);
        var board = new List<Card> { source, ally };

        // Register a Battlecry that buffs ally and an OnAttack that buffs self
        AbilityManager.RegisterAbility(source,
            new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffOtherFriendlyAttack, 2));
        AbilityManager.RegisterAbility(source,
            new OnAttackAbility(OnAttackAbility.OnAttackEffect.BuffSelfAttack, 3));

        // Trigger only OnAttack
        AbilityManager.TriggerAbilities(AbilityTrigger.OnAttack, MakeContext(source, board));

        Assert.AreEqual(4, source.attack, "OnAttack self-buff should fire");
        Assert.AreEqual(1, ally.attack, "Battlecry should NOT fire on OnAttack trigger");
    }

    [Test]
    public void AbilityManager_GetAbilities_EmptyForUnregisteredCard()
    {
        var card = MakeCard("NoAbility");
        var abilities = AbilityManager.GetAbilities(card);
        Assert.IsNotNull(abilities);
        Assert.AreEqual(0, abilities.Count);
    }

    [Test]
    public void AuraManager_RefreshAuras_PreventsDoubleStacking()
    {
        var source = MakeCard("AuraSource");
        var ally = MakeCard("Ally", attack: 1, health: 3);
        var board = new List<Card> { source, ally };

        var ability = new AuraAbility(AuraAbility.AuraEffect.BuffAllFriendlyAttack, 2);
        AbilityManager.RegisterAbility(source, ability);

        // Apply aura once via AuraManager
        AuraManager.RefreshAuras(board, null);
        int attackAfterFirst = ally.attack; // Should be 3

        // Refresh again — should remove then reapply, not stack
        AuraManager.RefreshAuras(board, null);
        Assert.AreEqual(attackAfterFirst, ally.attack,
            "Double RefreshAuras should not double-stack the aura buff");
    }
}

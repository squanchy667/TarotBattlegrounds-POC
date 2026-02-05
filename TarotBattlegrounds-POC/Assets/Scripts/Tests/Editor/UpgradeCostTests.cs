using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tests for tavern upgrade cost reduction logic (M5 bug fix).
/// Verifies that upgrade costs decrease globally each turn, not per-tier.
/// </summary>
[TestFixture]
public class UpgradeCostTests
{
    private GameObject gameManagerObj;
    private GameManager gameManager;
    private GameObject tavernManagerObj;
    private TavernManager tavernManager;
    private GameObject playerObj;
    private Player player;

    [SetUp]
    public void SetUp()
    {
        // Create GameManager
        gameManagerObj = new GameObject("GameManager");
        gameManager = gameManagerObj.AddComponent<GameManager>();

        // Create TavernManager
        tavernManagerObj = new GameObject("TavernManager");
        tavernManager = tavernManagerObj.AddComponent<TavernManager>();

        // Initialize master cards with test data
        tavernManager.masterCards = new List<Card>();
        for (int tier = 1; tier <= 6; tier++)
        {
            for (int i = 0; i < 3; i++)
            {
                var card = ScriptableObject.CreateInstance<Card>();
                card.cardName = $"TestCard_T{tier}_{i}";
                card.tier = tier;
                card.attack = tier;
                card.health = tier;
                card.tribe = "Wands";
                tavernManager.masterCards.Add(card);
            }
        }
        tavernManager.ResetPool();

        // Create Player
        playerObj = new GameObject("TestPlayer");
        player = playerObj.AddComponent<Player>();
        player.playerId = 1;
        player.coins = 20; // Enough to upgrade multiple times
        player.currentTavernTier = 1;

        // Initialize GameManager with test players
        gameManager.players = new List<Player> { player };
    }

    [TearDown]
    public void TearDown()
    {
        if (playerObj != null) Object.DestroyImmediate(playerObj);
        if (tavernManagerObj != null) Object.DestroyImmediate(tavernManagerObj);
        if (gameManagerObj != null) Object.DestroyImmediate(gameManagerObj);
    }

    [Test]
    public void UpgradeCost_Turn1_BaseCost()
    {
        // Arrange: Turn 1, Tier 1
        // Base cost for tier 1→2 should be 5 (no reduction on turn 1)

        // Act
        int cost = player.GetUpgradeCost();

        // Assert
        Assert.AreEqual(5, cost, "Turn 1 upgrade cost should be base cost (5)");
    }

    [Test]
    public void UpgradeCost_Turn2_ReducedBy1()
    {
        // Arrange: Simulate turn 2
        player.RefreshShop(2); // Turn 2

        // Act
        int cost = player.GetUpgradeCost();

        // Assert
        Assert.AreEqual(4, cost, "Turn 2 upgrade cost should be reduced by 1 (base 5 → 4)");
    }

    [Test]
    public void UpgradeCost_Turn3_ReducedBy2()
    {
        // Arrange: Simulate turn 3
        player.RefreshShop(2); // Turn 2
        player.RefreshShop(3); // Turn 3

        // Act
        int cost = player.GetUpgradeCost();

        // Assert
        Assert.AreEqual(3, cost, "Turn 3 upgrade cost should be reduced by 2 (base 5 → 3)");
    }

    [Test]
    public void UpgradeCost_AfterUpgrade_StillDecreasesGlobally()
    {
        // Arrange: Upgrade on turn 2, then check cost on turn 3
        player.RefreshShop(2); // Turn 2
        player.UpgradeTavern(); // Now tier 2
        player.RefreshShop(3); // Turn 3

        // Act: Cost for tier 2→3 upgrade
        int cost = player.GetUpgradeCost();

        // Assert: Base cost for tier 3 is 8, with 2 turns elapsed (turn 3 - 1) = 8 - 2 = 6
        Assert.AreEqual(6, cost, "Turn 3 cost for tier 2→3 should be 6 (base 8 - 2 turns = 6), NOT reset to base cost");
    }

    [Test]
    public void UpgradeCost_MultipleUpgrades_ContinuesDecreasing()
    {
        // Arrange: Upgrade twice, verify cost keeps decreasing
        player.RefreshShop(2); // Turn 2
        player.UpgradeTavern(); // Tier 1→2

        player.RefreshShop(3); // Turn 3
        int costBefore = player.GetUpgradeCost(); // Should be 6

        player.UpgradeTavern(); // Tier 2→3

        player.RefreshShop(4); // Turn 4
        int costAfter = player.GetUpgradeCost(); // Should be 6 (base 9 - 3 turns = 6)

        // Assert: Cost should continue to decrease based on game turn
        Assert.AreEqual(6, costBefore, "Turn 3: Tier 2→3 cost should be 6");
        Assert.AreEqual(6, costAfter, "Turn 4: Tier 3→4 cost should be 6 (base 9 - 3 = 6)");
    }

    [Test]
    public void UpgradeCost_NeverBelowOne()
    {
        // Arrange: Simulate many turns to test minimum cost
        for (int turn = 1; turn <= 20; turn++)
        {
            player.RefreshShop(turn);
        }

        // Act
        int cost = player.GetUpgradeCost();

        // Assert: Cost should be clamped to minimum of 1
        Assert.GreaterOrEqual(cost, 1, "Upgrade cost should never go below 1");
    }

    [Test]
    public void UpgradeCost_MaxTier_ReturnsZero()
    {
        // Arrange: Set player to max tier
        player.currentTavernTier = 6;

        // Act
        int cost = player.GetUpgradeCost();

        // Assert
        Assert.AreEqual(0, cost, "Max tier (6) should return 0 cost");
    }

    [Test]
    public void UpgradeCost_ConsistentForAllPlayers()
    {
        // Arrange: Create second player
        var player2Obj = new GameObject("TestPlayer2");
        var player2 = player2Obj.AddComponent<Player>();
        player2.playerId = 2;
        player2.coins = 20;
        player2.currentTavernTier = 1;
        gameManager.players.Add(player2);

        // Both players on turn 3
        player.RefreshShop(3);
        player2.RefreshShop(3);

        // Act
        int cost1 = player.GetUpgradeCost();
        int cost2 = player2.GetUpgradeCost();

        // Assert: Both should see same cost
        Assert.AreEqual(cost1, cost2, "All players should see same upgrade cost on same turn");

        Object.DestroyImmediate(player2Obj);
    }

    [Test]
    public void UpgradeCost_DifferentTiers_SameTurn_DifferentBaseCosts()
    {
        // Arrange: Player 1 at tier 1, Player 2 at tier 2
        var player2Obj = new GameObject("TestPlayer2");
        var player2 = player2Obj.AddComponent<Player>();
        player2.playerId = 2;
        player2.coins = 20;
        player2.currentTavernTier = 2; // Higher tier
        gameManager.players.Add(player2);

        // Both on turn 3
        player.RefreshShop(3); // Tier 1
        player2.RefreshShop(3); // Tier 2

        // Act
        int cost1 = player.GetUpgradeCost(); // Tier 1→2: base 5 - 2 = 3
        int cost2 = player2.GetUpgradeCost(); // Tier 2→3: base 8 - 2 = 6

        // Assert
        Assert.AreEqual(3, cost1, "Tier 1→2 on turn 3 should cost 3");
        Assert.AreEqual(6, cost2, "Tier 2→3 on turn 3 should cost 6");

        Object.DestroyImmediate(player2Obj);
    }
}

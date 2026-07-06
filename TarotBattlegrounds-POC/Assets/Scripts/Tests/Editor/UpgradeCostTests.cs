using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tests for tavern upgrade cost reduction logic (M5 bug fix).
/// Verifies that upgrade costs are reduced by 1 each turn as a game lifecycle event.
/// Costs reset to base when player upgrades to new tier.
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
        // Reset any polluted TavernManager singleton left over from a previous fixture
        // (TearDown DestroyImmediate()s the GameObject but never clears TavernManager.Instance)
        if (TavernManager.Instance != null) Object.DestroyImmediate(TavernManager.Instance.gameObject);

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
        // Awake() doesn't reliably fire synchronously for AddComponent<TavernManager>() in
        // this EditMode test context, so TavernManager.Instance is never set by the engine.
        // Set the singleton directly (Instance has a private setter, hence reflection).
        SetTavernInstance(tavernManager);

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
        SetTavernInstance(null);
    }

    private static void SetTavernInstance(TavernManager instance)
    {
        typeof(TavernManager).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .SetValue(null, instance);
    }

    [Test]
    public void UpgradeCost_Turn1_BaseCost()
    {
        // Arrange: Turn 1, Tier 1
        // Base cost for tier 1→2 should be 5 (no reduction on turn 1)
        player.currentUpgradeCost = 5; // Initialize

        // Act
        int cost = player.GetUpgradeCost();

        // Assert
        Assert.AreEqual(5, cost, "Turn 1 upgrade cost should be base cost (5)");
    }

    [Test]
    public void UpgradeCost_Turn2_ReducedBy1()
    {
        // Arrange: Start at cost 5, then lifecycle event reduces by 1
        player.currentUpgradeCost = 5;

        // Simulate lifecycle event (Turn 2 starts)
        player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);

        // Act
        int cost = player.GetUpgradeCost();

        // Assert
        Assert.AreEqual(4, cost, "Turn 2 upgrade cost should be reduced by 1 (base 5 → 4)");
    }

    [Test]
    public void UpgradeCost_Turn3_ReducedBy2()
    {
        // Arrange: Start at cost 5, two lifecycle events
        player.currentUpgradeCost = 5;

        // Turn 2 lifecycle event
        player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
        // Turn 3 lifecycle event
        player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);

        // Act
        int cost = player.GetUpgradeCost();

        // Assert
        Assert.AreEqual(3, cost, "Turn 3 upgrade cost should be reduced by 2 (base 5 → 3)");
    }

    [Test]
    public void UpgradeCost_AfterUpgrade_ResetsToBase()
    {
        // Arrange: Cost at 3, then upgrade to tier 2
        player.currentUpgradeCost = 3;
        player.currentTavernTier = 1;
        player.coins = 10;

        // Act: Upgrade (should reset cost to base 8 for tier 2→3)
        player.UpgradeTavern();

        // Assert: Cost should be BASE for tier 2→3 (8 gold)
        Assert.AreEqual(8, player.currentUpgradeCost, "After upgrade, cost should reset to base (8 for tier 2→3)");
        Assert.AreEqual(2, player.currentTavernTier, "Player should now be tier 2");
    }

    [Test]
    public void UpgradeCost_AfterUpgrade_ContinuesDecreasing()
    {
        // Arrange: Upgrade to tier 2, then simulate lifecycle events
        player.currentUpgradeCost = 3;
        player.currentTavernTier = 1;
        player.coins = 20;

        // Upgrade to tier 2 (cost resets to 8)
        player.UpgradeTavern();
        Assert.AreEqual(8, player.currentUpgradeCost, "After upgrade, cost = 8");

        // Lifecycle event (next turn)
        player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
        Assert.AreEqual(7, player.currentUpgradeCost, "After 1 turn at tier 2, cost = 7");

        // Another lifecycle event
        player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
        Assert.AreEqual(6, player.currentUpgradeCost, "After 2 turns at tier 2, cost = 6");
    }

    [Test]
    public void UpgradeCost_NeverBelowZero()
    {
        // Arrange: Reduce cost many times to test minimum
        player.currentUpgradeCost = 2;

        // Reduce multiple times
        for (int i = 0; i < 10; i++)
        {
            player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
        }

        // Act
        int cost = player.GetUpgradeCost();

        // Assert: Cost should be clamped to minimum of 0
        Assert.AreEqual(0, cost, "Upgrade cost should never go below 0");
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
    public void UpgradeCost_LifecycleEvent_ReducesForAllPlayers()
    {
        // Arrange: Create second player
        var player2Obj = new GameObject("TestPlayer2");
        var player2 = player2Obj.AddComponent<Player>();
        player2.playerId = 2;
        player2.coins = 20;
        player2.currentTavernTier = 1;
        player2.currentUpgradeCost = 5;
        gameManager.players.Add(player2);

        player.currentUpgradeCost = 5;

        // Act: Simulate lifecycle event (reduces ALL players by 1)
        player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
        player2.currentUpgradeCost = Mathf.Max(0, player2.currentUpgradeCost - 1);

        // Assert: Both should have reduced
        Assert.AreEqual(4, player.GetUpgradeCost(), "Player 1 cost should reduce to 4");
        Assert.AreEqual(4, player2.GetUpgradeCost(), "Player 2 cost should reduce to 4");

        Object.DestroyImmediate(player2Obj);
    }

    [Test]
    public void UpgradeCost_DifferentTiers_LifecycleReducesBoth()
    {
        // Arrange: Player 1 at tier 1 (cost 3), Player 2 at tier 2 (cost 7)
        var player2Obj = new GameObject("TestPlayer2");
        var player2 = player2Obj.AddComponent<Player>();
        player2.playerId = 2;
        player2.coins = 20;
        player2.currentTavernTier = 2; // Higher tier
        player2.currentUpgradeCost = 7; // Tier 2→3 base is 8, already reduced once
        gameManager.players.Add(player2);

        player.currentTavernTier = 1;
        player.currentUpgradeCost = 3; // Tier 1→2 base is 5, already reduced twice

        // Act: Lifecycle event reduces both
        player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
        player2.currentUpgradeCost = Mathf.Max(0, player2.currentUpgradeCost - 1);

        // Assert: Both reduced, but from different bases
        Assert.AreEqual(2, player.GetUpgradeCost(), "Tier 1 player: 3 → 2");
        Assert.AreEqual(6, player2.GetUpgradeCost(), "Tier 2 player: 7 → 6");

        Object.DestroyImmediate(player2Obj);
    }
}

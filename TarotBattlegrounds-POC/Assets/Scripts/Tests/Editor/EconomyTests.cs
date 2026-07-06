using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tests for gold economy and tier upgrade mechanics
/// </summary>
[TestFixture]
public class EconomyTests
{
    private GameObject playerGO;
    private Player player;
    private GameObject tavernGO;
    private TavernManager tavern;

    [SetUp]
    public void Setup()
    {
        // Reset any polluted TavernManager singleton left over from a previous fixture
        // (TearDown DestroyImmediate()s the GameObject but never clears TavernManager.Instance)
        if (TavernManager.Instance != null) Object.DestroyImmediate(TavernManager.Instance.gameObject);

        // Create TavernManager so Player's economy methods (RefreshShop, etc.) don't
        // early-return with a "TavernManager not found" error (Start() doesn't run in EditMode)
        tavernGO = new GameObject("TavernManager");
        tavern = tavernGO.AddComponent<TavernManager>();
        tavern.masterCards = CreateTestCards();
        tavern.ResetPool();
        // Awake() doesn't reliably fire synchronously for AddComponent<TavernManager>() in
        // this EditMode test context, so TavernManager.Instance is never set by the engine.
        // Set the singleton directly (Instance has a private setter, hence reflection).
        SetTavernInstance(tavern);

        playerGO = new GameObject("Player");
        player = playerGO.AddComponent<Player>();
        player.playerId = 1;
        player.coins = 0;

        tavern.availableCards[1] = new List<Card>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerGO);
        Object.DestroyImmediate(tavernGO);
        SetTavernInstance(null);
    }

    private static void SetTavernInstance(TavernManager instance)
    {
        typeof(TavernManager).GetProperty("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .SetValue(null, instance);
    }

    private List<Card> CreateTestCards()
    {
        var cards = new List<Card>();
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = "TestCard";
        card.tier = 1;
        card.attack = 1;
        card.health = 3;
        cards.Add(card);
        return cards;
    }
    
    [Test]
    public void GoldIncreases_PerTurn_Turn1()
    {
        // Turn 1 should give 3 gold
        player.RefreshShop(1);
        Assert.AreEqual(3, player.coins);
    }
    
    [Test]
    public void GoldIncreases_PerTurn_Turn5()
    {
        // Turn 5 should give 7 gold
        player.RefreshShop(5);
        Assert.AreEqual(7, player.coins);
    }
    
    [Test]
    public void GoldCapped_At10()
    {
        // Turn 10+ should cap at 10 gold
        player.RefreshShop(10);
        Assert.AreEqual(10, player.coins);
        
        player.RefreshShop(15);
        Assert.AreEqual(10, player.coins);
    }
    
    [Test]
    public void TierUpgrade_FromTier1To2_CostsCorrect()
    {
        player.currentTavernTier = 1;
        int cost = player.GetUpgradeCost();
        // Base cost for Tier 2 is 6, minus turns at current tier
        Assert.IsTrue(cost >= 1 && cost <= 6);
    }
    
    [Test]
    public void TierUpgrade_IncrementsTier()
    {
        player.currentTavernTier = 1;
        player.coins = 10;
        
        player.UpgradeTavern();
        
        Assert.AreEqual(2, player.currentTavernTier);
    }
    
    [Test]
    public void TierUpgrade_DeductsGold()
    {
        player.currentTavernTier = 1;
        player.coins = 10;
        int cost = player.GetUpgradeCost();
        
        player.UpgradeTavern();
        
        Assert.AreEqual(10 - cost, player.coins);
    }
    
    [Test]
    public void TierUpgrade_WithInsufficientGold_Fails()
    {
        player.currentTavernTier = 1;
        player.coins = 1; // Not enough
        
        player.UpgradeTavern();
        
        Assert.AreEqual(1, player.currentTavernTier);
        Assert.AreEqual(1, player.coins);
    }
    
    [Test]
    public void TierUpgrade_CostDecreases_OverTurns()
    {
        player.currentTavernTier = 1;
        int cost1 = player.GetUpgradeCost();
        
        // Simulate passing a turn (this increments the tier counter)
        player.RefreshShop(2);
        int cost2 = player.GetUpgradeCost();
        
        Assert.IsTrue(cost2 <= cost1, "Cost should decrease or stay same after turn");
    }
}

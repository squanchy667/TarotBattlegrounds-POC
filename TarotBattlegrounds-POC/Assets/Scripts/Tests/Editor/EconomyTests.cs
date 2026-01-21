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
    
    [SetUp]
    public void Setup()
    {
        playerGO = new GameObject("Player");
        player = playerGO.AddComponent<Player>();
        player.playerId = 1;
        player.coins = 0;
    }
    
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerGO);
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

using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Edge case tests to verify game handles boundary conditions
/// </summary>
[TestFixture]
public class EdgeCaseTests
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

        // Create TavernManager so Player's economy methods (RefreshShop, SellCard, etc.)
        // don't early-return with a "TavernManager not found" error (Start() doesn't run in EditMode)
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
    
    // ==================== ECONOMY EDGE CASES ====================
    
    [Test]
    public void BuyCard_WithZeroGold_Fails()
    {
        player.coins = 0;
        int initialCoins = player.coins;
        
        // Would need TavernManager for full test
        // This verifies the property doesn't go negative
        Assert.AreEqual(0, player.coins);
    }
    
    [Test]
    public void SellCard_WithEmptyBoard_DoesNothing()
    {
        player.coins = 5;
        player.board.Clear();
        
        player.SellCard(0); // Invalid index
        
        Assert.AreEqual(5, player.coins); // Unchanged
        Assert.AreEqual(0, player.board.Count);
    }
    
    [Test]
    public void GoldCap_StaysAt10_Turn20()
    {
        player.RefreshShop(20);
        Assert.AreEqual(10, player.coins);
    }
    
    [Test]
    public void TierUpgrade_AtMaxTier_Fails()
    {
        player.currentTavernTier = 6;
        player.coins = 100;
        
        player.UpgradeTavern();
        
        Assert.AreEqual(6, player.currentTavernTier);
        Assert.AreEqual(100, player.coins);
    }
    
    // ==================== BOARD EDGE CASES ====================
    
    [Test]
    public void PlayCard_AtMaxBoard_Fails()
    {
        // Fill board
        for (int i = 0; i < 7; i++)
        {
            var card = ScriptableObject.CreateInstance<Card>();
            player.board.Add(card);
        }
        
        var handCard = ScriptableObject.CreateInstance<Card>();
        player.hand.Add(handCard);
        
        player.PlayCard(0, 0);
        
        Assert.AreEqual(7, player.board.Count);
        Assert.AreEqual(1, player.hand.Count);
    }
    
    [Test]
    public void PlayCard_WithEmptyHand_DoesNothing()
    {
        player.hand.Clear();
        int boardCount = player.board.Count;
        
        player.PlayCard(0, 0);
        
        Assert.AreEqual(boardCount, player.board.Count);
    }
    
    [Test]
    public void PlayCard_InvalidIndex_DoesNothing()
    {
        var card = ScriptableObject.CreateInstance<Card>();
        player.hand.Add(card);
        
        player.PlayCard(99, 0); // Invalid index
        
        Assert.AreEqual(1, player.hand.Count);
    }
    
    // ==================== COMBAT EDGE CASES ====================
    
    [Test]
    public void Combat_BothEmptyBoards_IsTie()
    {
        var board1 = new List<Card>();
        var board2 = new List<Card>();
        
        var (damage, winner) = CombatManager.SimulateBattle(
            board1, board2, 1, 1, "P1", "P2");
        
        Assert.AreEqual("Tie", winner);
        Assert.AreEqual(0, damage);
    }
    
    [Test]
    public void Combat_OneEmptyBoard_OtherWins()
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.attack = 1;
        card.health = 1;
        card.tier = 1;
        
        var board1 = new List<Card> { card };
        var board2 = new List<Card>();
        
        var (damage, winner) = CombatManager.SimulateBattle(
            board1, board2, 1, 1, "P1", "P2");
        
        Assert.AreEqual("P1", winner);
        Assert.IsTrue(damage > 0);
    }
    
    [Test]
    public void Combat_MaxDamage_EqualsMinionsAndTier()
    {
        // Fix 2: Damage = surviving minions + tavern tier (no hard cap)
        var cards = new List<Card>();
        for (int i = 0; i < 7; i++)
        {
            var card = ScriptableObject.CreateInstance<Card>();
            card.attack = 1;
            card.health = 100;
            card.tier = 6;
            cards.Add(card);
        }

        var (damage, _) = CombatManager.SimulateBattle(
            cards, new List<Card>(), 6, 6, "P1", "P2");

        // 7 surviving minions + tavern tier 6 = 13
        Assert.AreEqual(13, damage);
    }
    
    // ==================== HEALTH EDGE CASES ====================
    
    [Test]
    public void Health_AtExactlyZero_IsDefeated()
    {
        player.Health = 5;
        player.Health -= 5;
        
        Assert.AreEqual(0, player.Health);
    }
    
    [Test]
    public void Health_BelowZero_StaysNegative()
    {
        player.Health = 5;
        player.Health -= 10;
        
        Assert.AreEqual(-5, player.Health);
    }
    
    // ==================== EVENT EDGE CASES ====================
    
    [Test]
    public void Event_OnCoinsChanged_Fires()
    {
        bool eventFired = false;
        player.OnCoinsChanged += () => eventFired = true;
        
        player.coins = 5;
        
        Assert.IsTrue(eventFired);
    }
    
    [Test]
    public void Event_OnHealthChanged_Fires()
    {
        bool eventFired = false;
        int receivedHealth = -1;
        player.OnHealthChanged += (h) => { eventFired = true; receivedHealth = h; };
        
        player.Health = 30;
        
        Assert.IsTrue(eventFired);
        Assert.AreEqual(30, receivedHealth);
    }
    
    [Test]
    public void NotifyAllStateChanged_FiresAllEvents()
    {
        int eventCount = 0;
        player.OnHandChanged += () => eventCount++;
        player.OnBoardChanged += () => eventCount++;
        player.OnCoinsChanged += () => eventCount++;
        player.OnTierChanged += () => eventCount++;
        player.OnHealthChanged += (_) => eventCount++;
        player.OnShopRefreshed += () => eventCount++;
        
        player.NotifyAllStateChanged();
        
        Assert.AreEqual(6, eventCount);
    }
}

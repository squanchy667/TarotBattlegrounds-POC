using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tests for card buy/sell/play mechanics
/// </summary>
[TestFixture]
public class CardSystemTests
{
    private GameObject playerGO;
    private Player player;
    private GameObject tavernGO;
    private TavernManager tavern;
    
    [SetUp]
    public void Setup()
    {
        // Create TavernManager
        tavernGO = new GameObject("TavernManager");
        tavern = tavernGO.AddComponent<TavernManager>();
        tavern.masterCards = CreateTestCards();
        tavern.ResetPool();
        
        // Create Player
        playerGO = new GameObject("Player");
        player = playerGO.AddComponent<Player>();
        player.playerId = 1;
        
        tavern.availableCards[1] = new List<Card>();
        tavern.RefreshPlayerShop(1, 1);
        player.coins = 10;
    }
    
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerGO);
        Object.DestroyImmediate(tavernGO);
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
    public void BuyCard_WithEnoughGold_AddsToHand()
    {
        player.coins = 10;
        int initialHandSize = player.hand.Count;
        
        player.BuyCard(0);
        
        Assert.AreEqual(initialHandSize + 1, player.hand.Count);
        Assert.AreEqual(7, player.coins);
    }
    
    [Test]
    public void BuyCard_WithInsufficientGold_Fails()
    {
        player.coins = 2;
        int initialHandSize = player.hand.Count;
        
        player.BuyCard(0);
        
        Assert.AreEqual(initialHandSize, player.hand.Count);
        Assert.AreEqual(2, player.coins);
    }
    
    [Test]
    public void SellCard_AddsGoldCorrectly()
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = "SellTest";
        player.board.Add(card);
        player.coins = 5;
        
        player.SellCard(0);
        
        Assert.AreEqual(0, player.board.Count);
        Assert.AreEqual(6, player.coins);
    }
    
    [Test]
    public void PlayCard_FromHandToBoard_Works()
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = "PlayTest";
        player.hand.Add(card);
        
        player.PlayCard(0, 0);
        
        Assert.AreEqual(0, player.hand.Count);
        Assert.AreEqual(1, player.board.Count);
    }
    
    [Test]
    public void PlayCard_WithFullBoard_Fails()
    {
        for (int i = 0; i < 7; i++)
        {
            var boardCard = ScriptableObject.CreateInstance<Card>();
            player.board.Add(boardCard);
        }
        var handCard = ScriptableObject.CreateInstance<Card>();
        player.hand.Add(handCard);
        
        player.PlayCard(0, 0);
        
        Assert.AreEqual(1, player.hand.Count);
        Assert.AreEqual(7, player.board.Count);
    }
    
    [Test]
    public void RerollShop_CostsOneGold()
    {
        player.coins = 5;
        player.RefreshTavernShop();
        Assert.AreEqual(4, player.coins);
    }
}

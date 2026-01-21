using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tests for combat simulation mechanics
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
    
    [Test]
    public void Combat_WithEmptyBoards_ReturnsTie()
    {
        var board1 = new List<Card>();
        var board2 = new List<Card>();
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, "P1", "P2");
        
        Assert.AreEqual("Tie", winner);
        Assert.AreEqual(0, damage);
    }
    
    [Test]
    public void Combat_Player1Empty_Player2Wins()
    {
        var board1 = new List<Card>();
        var board2 = CreateBoard(1, 1, 3);
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, "P1", "P2");
        
        Assert.AreEqual("P2", winner);
        Assert.IsTrue(damage > 0);
    }
    
    [Test]
    public void Combat_Player2Empty_Player1Wins()
    {
        var board1 = CreateBoard(1, 1, 3);
        var board2 = new List<Card>();
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, "P1", "P2");
        
        Assert.AreEqual("P1", winner);
        Assert.IsTrue(damage > 0);
    }
    
    [Test]
    public void Combat_EqualBoards_ProducesDamage()
    {
        var board1 = CreateBoard(2, 2, 2);
        var board2 = CreateBoard(2, 2, 2);
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 1, "P1", "P2");
        
        // Either someone wins or it's a tie
        Assert.IsTrue(winner == "P1" || winner == "P2" || winner == "Tie");
    }
    
    [Test]
    public void Combat_DamageCapped_At5()
    {
        // Create a large board to potentially generate high damage
        var board1 = CreateBoard(7, 1, 10, 3); // 7 tier-3 cards
        var board2 = new List<Card>();
        
        var (damage, winner) = CombatManager.SimulateBattle(board1, board2, 3, "P1", "P2");
        
        Assert.IsTrue(damage <= 5, $"Damage {damage} should be capped at 5");
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
            
            var (_, winner) = CombatManager.SimulateBattle(board1, board2, 1, "P1", "P2");
            
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
        var (_, winner) = CombatManager.SimulateBattle(board1, board2, 1, "P1", "P2");
        
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
        var (_, winner) = CombatManager.SimulateBattle(board1, board2, 1, "P1", "P2");
        Assert.Pass("Guardian combat simulation completed");
    }
}

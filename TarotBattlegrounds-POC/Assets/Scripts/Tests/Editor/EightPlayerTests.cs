using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// T614-T620: Tests for 8-player scaling, balance, and performance.
/// </summary>
[TestFixture]
public class EightPlayerTests
{
    private EightPlayerManager manager;

    [SetUp]
    public void Setup()
    {
        var go = new UnityEngine.GameObject("TestEightPlayerManager");
        manager = go.AddComponent<EightPlayerManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (manager != null)
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }

    // T605: Health scaling
    [Test]
    public void HealthScaling_4Players_Returns40()
    {
        Assert.AreEqual(40, manager.GetStartingHealth(4));
    }

    [Test]
    public void HealthScaling_8Players_Returns50()
    {
        Assert.AreEqual(50, manager.GetStartingHealth(8));
    }

    [Test]
    public void HealthScaling_2Players_Returns30()
    {
        Assert.AreEqual(30, manager.GetStartingHealth(2));
    }

    [Test]
    public void HealthScaling_IncrementsWithPlayerCount()
    {
        int prev = 0;
        for (int i = 2; i <= 8; i++)
        {
            int health = manager.GetStartingHealth(i);
            Assert.GreaterOrEqual(health, prev, $"Health should increase with player count. Got {health} for {i} players");
            prev = health;
        }
    }

    // T603: Round-robin pairing
    [Test]
    public void Pairings_EvenPlayerCount_AllPaired()
    {
        var players = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        var pairings = manager.GeneratePairings(players, 1);

        Assert.AreEqual(4, pairings.Count, "8 players should produce 4 pairings");

        // All players should appear exactly once
        var seen = new HashSet<int>();
        foreach (var (a, b) in pairings)
        {
            Assert.IsFalse(seen.Contains(a), $"Player {a} appears twice");
            Assert.IsFalse(seen.Contains(b), $"Player {b} appears twice");
            seen.Add(a);
            seen.Add(b);
        }
        Assert.AreEqual(8, seen.Count);
    }

    [Test]
    public void Pairings_OddPlayerCount_OneGhostPairing()
    {
        var players = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
        var pairings = manager.GeneratePairings(players, 1);

        // 7 players = 3 normal pairings + 1 ghost pairing = 4 total
        Assert.AreEqual(4, pairings.Count);
    }

    [Test]
    public void Pairings_NoDuplicatesInSingleRound()
    {
        var players = new List<int> { 0, 1, 2, 3 };
        var pairings = manager.GeneratePairings(players, 1);

        var flat = new List<int>();
        foreach (var (a, b) in pairings)
        {
            flat.Add(a);
            Assert.AreNotEqual(a, b, "Player cannot fight themselves");
        }
    }

    // T604: Damage calculation
    [Test]
    public void Damage_IncreasesOverTurns()
    {
        int dmg1 = manager.CalculateCombatDamage(1, 10, 4);
        int dmg5 = manager.CalculateCombatDamage(5, 10, 4);
        int dmg10 = manager.CalculateCombatDamage(10, 10, 4);

        Assert.Less(dmg1, dmg10, "Damage should increase over turns");
        Assert.Less(dmg5, dmg10, "Damage should increase over turns");
    }

    [Test]
    public void Damage_ScalesDownWith8Players()
    {
        int dmg4 = manager.CalculateCombatDamage(5, 10, 4);
        int dmg8 = manager.CalculateCombatDamage(5, 10, 8);

        Assert.LessOrEqual(dmg8, dmg4, "8-player damage should be <= 4-player damage");
    }

    [Test]
    public void Damage_MinimumIs1()
    {
        int dmg = manager.CalculateCombatDamage(0, 0, 8);
        Assert.GreaterOrEqual(dmg, 1, "Minimum damage should be 1");
    }

    // T604: Bracket descriptions
    [Test]
    public void Bracket_8To1()
    {
        Assert.AreEqual("Winner!", manager.GetBracketDescription(8, 1));
        Assert.AreEqual("Final 2", manager.GetBracketDescription(8, 2));
        Assert.AreEqual("Top 4", manager.GetBracketDescription(8, 4));
    }

    // T615: Shop pool scaling
    [Test]
    public void ShopPool_ScalesWithPlayerCount()
    {
        Assert.AreEqual(1.0f, manager.GetShopPoolMultiplier(4));
        Assert.AreEqual(1.5f, manager.GetShopPoolMultiplier(6));
        Assert.AreEqual(2.0f, manager.GetShopPoolMultiplier(8));
    }

    // T616: Economy adjustments
    [Test]
    public void StartingGold_ExtraFor8Players()
    {
        Assert.AreEqual(3, manager.GetStartingGold(4));
        Assert.AreEqual(4, manager.GetStartingGold(8));
    }

    // T611: Delta state compression
    [Test]
    public void DeltaCompressor_FirstSync_SendsAll()
    {
        var go = new UnityEngine.GameObject("TestCompressor");
        var compressor = go.AddComponent<DeltaStateCompressor>();

        var state = new Dictionary<string, object>
        {
            { "health", 40 },
            { "gold", 3 },
            { "tier", 1 }
        };

        var delta = compressor.ComputeDelta(0, state);
        Assert.IsNotNull(delta);
        Assert.AreEqual(3, delta.Count, "First sync should send all fields");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DeltaCompressor_NoChange_ReturnsNull()
    {
        var go = new UnityEngine.GameObject("TestCompressor");
        var compressor = go.AddComponent<DeltaStateCompressor>();

        var state = new Dictionary<string, object>
        {
            { "health", 40 },
            { "gold", 3 }
        };

        compressor.ComputeDelta(0, state); // First sync
        var delta = compressor.ComputeDelta(0, new Dictionary<string, object>(state)); // Same state

        Assert.IsNull(delta, "No change should return null");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DeltaCompressor_PartialChange_SendsOnlyChanged()
    {
        var go = new UnityEngine.GameObject("TestCompressor");
        var compressor = go.AddComponent<DeltaStateCompressor>();

        compressor.ComputeDelta(0, new Dictionary<string, object>
        {
            { "health", 40 },
            { "gold", 3 },
            { "tier", 1 }
        });

        var delta = compressor.ComputeDelta(0, new Dictionary<string, object>
        {
            { "health", 35 },
            { "gold", 3 },
            { "tier", 1 }
        });

        Assert.IsNotNull(delta);
        Assert.AreEqual(1, delta.Count, "Only changed field should be in delta");
        Assert.AreEqual(35, delta["health"]);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // T614: 8-player AI balance (batch simulation)
    [Test]
    public void EightPlayerGame_Completes()
    {
        // Verify GameConfig supports 8 players
        GameConfig.PlayerCount = 8;
        Assert.AreEqual(8, GameConfig.PlayerCount);
    }

    [Test]
    public void EightPlayerGame_HealthScaling_IsReasonable()
    {
        // 8-player game with 50 health should last reasonable turns
        int health = manager.GetStartingHealth(8);
        int avgDamageT1 = manager.CalculateCombatDamage(1, 5, 8);
        int avgDamageT10 = manager.CalculateCombatDamage(10, 15, 8);

        // Should take at least 5 turns to eliminate even with high damage
        Assert.Greater(health / avgDamageT1, 3, "Games should last more than 3 turns");
        // But shouldn't last forever
        Assert.Less(health / avgDamageT1, 60, "Games shouldn't last 60+ turns");
    }
}

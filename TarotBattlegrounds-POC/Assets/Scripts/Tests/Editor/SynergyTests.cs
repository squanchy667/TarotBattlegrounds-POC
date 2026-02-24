using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Automated tests for tribe synergy system.
/// Covers threshold activation (2/4/6), cross-tribe combos,
/// multi-tribe card counting, and synergy effect application.
/// Updated to use per-player snapshot pattern instead of global state.
/// </summary>
[TestFixture]
public class SynergyTests
{
    private GameObject themeManagerGO;
    private GameObject synergyManagerGO;
    private SynergyManager synergyManager;

    [SetUp]
    public void Setup()
    {
        // Create ThemeManager (needed for tribe name parsing)
        themeManagerGO = new GameObject("ThemeManager");
        themeManagerGO.AddComponent<ThemeManager>();

        // Create SynergyManager and initialize with test synergies
        synergyManagerGO = new GameObject("SynergyManager");
        synergyManager = synergyManagerGO.AddComponent<SynergyManager>();
        synergyManager.tribeSynergies = SynergyTestData.CreateAllTribeSynergies();
        synergyManager.InitializeSynergyCache();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(synergyManagerGO);
        Object.DestroyImmediate(themeManagerGO);
    }

    private Card CreateCard(string name, TribeType[] tribes, int attack = 1, int health = 1, int tier = 1)
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tribes = tribes;
        card.attack = attack;
        card.health = health;
        card.tier = tier;
        return card;
    }

    private int GetTribeCount(SynergyManager.SynergySnapshot snapshot, TribeType tribe)
    {
        return snapshot.tribeCounts.TryGetValue(tribe, out int count) ? count : 0;
    }

    private SynergyTier GetActiveTier(SynergyManager.SynergySnapshot snapshot, TribeType tribe)
    {
        // C6 fix: activeTiers is now List<SynergyTier> per tribe — return highest for backward compat
        return snapshot.GetHighestTier(tribe);
    }

    private bool IsComboActive(SynergyManager.SynergySnapshot snapshot, TribeType tribe1, TribeType tribe2)
    {
        var combo = tribe1 < tribe2 ? (tribe1, tribe2) : (tribe2, tribe1);
        return snapshot.activeCombos.Contains(combo);
    }

    // =====================================================
    // TRIBE COUNTING TESTS
    // =====================================================

    [Test]
    public void TribeCounting_EmptyBoard_NoTribes()
    {
        var board = new List<Card>();
        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Pentacles));
        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Cups));
        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Swords));
        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Wands));
    }

    [Test]
    public void TribeCounting_SingleTribeCards_CountsCorrectly()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords }),
            CreateCard("Cup1", new[] { TribeType.Cups })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Swords));
        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Cups));
        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Pentacles));
        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Wands));
    }

    [Test]
    public void TribeCounting_MultiTribeCard_CountsBothTribes()
    {
        var board = new List<Card>
        {
            CreateCard("DualCard", new[] { TribeType.Pentacles, TribeType.Cups })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Pentacles));
        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Cups));
    }

    [Test]
    public void TribeCounting_TripleTribeCard_CountsAllThree()
    {
        var board = new List<Card>
        {
            CreateCard("TripleCard", new[] { TribeType.Cups, TribeType.Wands, TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Cups));
        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Wands));
        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Swords));
        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Pentacles));
    }

    [Test]
    public void TribeCounting_MixedSingleAndMultiTribe_AccumulatesCorrectly()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("SwordPent", new[] { TribeType.Swords, TribeType.Pentacles }),
            CreateCard("Pent1", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Swords));
        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Pentacles));
    }

    [Test]
    public void TribeCounting_NullBoard_NoError()
    {
        var snapshot = synergyManager.CalculateSynergies(null);
        Assert.AreEqual(0, GetTribeCount(snapshot, TribeType.Swords));
    }

    [Test]
    public void TribeCounting_CardWithNoTribes_NotCounted()
    {
        var board = new List<Card>
        {
            CreateCard("Neutral", new TribeType[0]),
            CreateCard("Sword1", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Swords));
        Assert.AreEqual(1, snapshot.tribeCounts.Count, "Only one tribe should be counted");
    }

    // =====================================================
    // THRESHOLD TIER ACTIVATION TESTS (2/4/6)
    // =====================================================

    [Test]
    public void Threshold_OneTribeMember_NoTierActive()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsNull(GetActiveTier(snapshot, TribeType.Swords),
            "1 tribe member should not activate any tier");
    }

    [Test]
    public void Threshold_TwoSwords_Tier2Active()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Swords);
        Assert.IsNotNull(tier, "2 Swords should activate tier 2");
        Assert.AreEqual(2, tier.threshold);
        Assert.AreEqual(SynergyEffect.BuffAttack, tier.effect);
    }

    [Test]
    public void Threshold_FourSwords_Tier4Active()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords }),
            CreateCard("Sword3", new[] { TribeType.Swords }),
            CreateCard("Sword4", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Swords);
        Assert.IsNotNull(tier, "4 Swords should activate tier 4");
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BonusDamage, tier.effect);
    }

    [Test]
    public void Threshold_SixWands_Tier6Active()
    {
        var board = SynergyTestCards.CreateTestBoard_WandsTier6();
        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Wands);
        Assert.IsNotNull(tier, "6 Wands should activate tier 6");
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyEffect.BuffAttack, tier.effect);
        Assert.AreEqual(SynergyTarget.AllFriendly, tier.target);
    }

    [Test]
    public void Threshold_ThreeMembers_StillTier2()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles }),
            CreateCard("Pent3", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Pentacles);
        Assert.IsNotNull(tier);
        Assert.AreEqual(2, tier.threshold, "3 members should activate tier 2, not tier 4");
    }

    [Test]
    public void Threshold_FiveMembers_StillTier4()
    {
        var board = new List<Card>
        {
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups }),
            CreateCard("Cup3", new[] { TribeType.Cups }),
            CreateCard("Cup4", new[] { TribeType.Cups }),
            CreateCard("Cup5", new[] { TribeType.Cups })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Cups);
        Assert.IsNotNull(tier);
        Assert.AreEqual(4, tier.threshold, "5 members should activate tier 4, not tier 6");
    }

    [Test]
    public void Threshold_SevenMembers_Tier6Active()
    {
        var board = new List<Card>
        {
            CreateCard("Wand1", new[] { TribeType.Wands }),
            CreateCard("Wand2", new[] { TribeType.Wands }),
            CreateCard("Wand3", new[] { TribeType.Wands }),
            CreateCard("Wand4", new[] { TribeType.Wands }),
            CreateCard("Wand5", new[] { TribeType.Wands }),
            CreateCard("Wand6", new[] { TribeType.Wands }),
            CreateCard("Wand7", new[] { TribeType.Wands })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Wands);
        Assert.IsNotNull(tier);
        Assert.AreEqual(6, tier.threshold, "7+ members should still be tier 6 (highest)");
    }

    [Test]
    public void Threshold_AllFourTribes_Tier2Each()
    {
        // 2 of each tribe
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles }),
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups }),
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords }),
            CreateCard("Wand1", new[] { TribeType.Wands })
            // Only 1 Wand - should NOT activate
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Pentacles), "Pentacles tier 2 should be active");
        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Cups), "Cups tier 2 should be active");
        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Swords), "Swords tier 2 should be active");
        Assert.IsNull(GetActiveTier(snapshot, TribeType.Wands), "Wands should NOT be active (only 1)");
    }

    [Test]
    public void Threshold_MultiTribeCards_ContributeToMultipleTiers()
    {
        // 1 Pentacles + 1 Pentacles/Cups dual + 1 Cups = 2 Pentacles, 2 Cups
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("PentCup", new[] { TribeType.Pentacles, TribeType.Cups }),
            CreateCard("Cup1", new[] { TribeType.Cups })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Pentacles), "Pentacles should be active at tier 2");
        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Cups), "Cups should be active at tier 2");
    }

    // =====================================================
    // TRIBE-SPECIFIC TIER TESTS
    // =====================================================

    [Test]
    public void Pentacles_Tier2_OnSellBonusGold()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Pentacles);
        Assert.AreEqual(SynergyTrigger.OnSell, tier.trigger);
        Assert.AreEqual(SynergyEffect.BonusGold, tier.effect);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Cups_Tier2_EndOfTurnHealAdjacent()
    {
        var board = new List<Card>
        {
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Cups);
        Assert.AreEqual(SynergyTrigger.EndOfTurn, tier.trigger);
        Assert.AreEqual(SynergyEffect.HealFlat, tier.effect);
        Assert.AreEqual(SynergyTarget.Adjacent, tier.target);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Swords_Tier4_BonusDamage()
    {
        var board = SynergyTestCards.CreateTestBoard_SwordsTier4();
        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Swords);
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BonusDamage, tier.effect);
        Assert.AreEqual(2, tier.value);
    }

    [Test]
    public void Wands_Tier4_BuffStatsTribeMembers()
    {
        var board = new List<Card>
        {
            CreateCard("Wand1", new[] { TribeType.Wands }),
            CreateCard("Wand2", new[] { TribeType.Wands }),
            CreateCard("Wand3", new[] { TribeType.Wands }),
            CreateCard("Wand4", new[] { TribeType.Wands })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Wands);
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BuffStats, tier.effect);
        Assert.AreEqual(SynergyTarget.AllTribeMembers, tier.target);
    }

    [Test]
    public void Cups_Tier6_ShieldAllFriendly()
    {
        var board = new List<Card>
        {
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups }),
            CreateCard("Cup3", new[] { TribeType.Cups }),
            CreateCard("Cup4", new[] { TribeType.Cups }),
            CreateCard("Cup5", new[] { TribeType.Cups }),
            CreateCard("Cup6", new[] { TribeType.Cups })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Cups);
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyTrigger.StartOfCombat, tier.trigger);
        Assert.AreEqual(SynergyEffect.Shield, tier.effect);
        Assert.AreEqual(SynergyTarget.AllFriendly, tier.target);
    }

    [Test]
    public void Pentacles_Tier6_ReduceCost()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles }),
            CreateCard("Pent3", new[] { TribeType.Pentacles }),
            CreateCard("Pent4", new[] { TribeType.Pentacles }),
            CreateCard("Pent5", new[] { TribeType.Pentacles }),
            CreateCard("Pent6", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Pentacles);
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyEffect.ReduceCost, tier.effect);
    }

    // =====================================================
    // CROSS-TRIBE COMBO TESTS
    // =====================================================

    [Test]
    public void Combo_PentaclesCups_ActiveWith2Each()
    {
        var board = SynergyTestCards.CreateTestBoard_PentaclesCups();
        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(IsComboActive(snapshot, TribeType.Pentacles, TribeType.Cups),
            "Pentacles+Cups combo should be active with 2 of each");
    }

    [Test]
    public void Combo_PentaclesCups_InactiveWithOnlyOneCup()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles }),
            CreateCard("Cup1", new[] { TribeType.Cups }) // Only 1 Cup
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsFalse(IsComboActive(snapshot, TribeType.Pentacles, TribeType.Cups),
            "Combo should NOT be active with only 1 Cup");
    }

    [Test]
    public void Combo_SwordsPentacles_ActiveWith2Each()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords }),
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(IsComboActive(snapshot, TribeType.Swords, TribeType.Pentacles));
    }

    [Test]
    public void Combo_WandsSwords_ActiveWith2Each()
    {
        var board = new List<Card>
        {
            CreateCard("Wand1", new[] { TribeType.Wands }),
            CreateCard("Wand2", new[] { TribeType.Wands }),
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(IsComboActive(snapshot, TribeType.Wands, TribeType.Swords));
    }

    [Test]
    public void Combo_CupsWands_ActiveWith2Each()
    {
        var board = new List<Card>
        {
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups }),
            CreateCard("Wand1", new[] { TribeType.Wands }),
            CreateCard("Wand2", new[] { TribeType.Wands })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(IsComboActive(snapshot, TribeType.Cups, TribeType.Wands));
    }

    [Test]
    public void Combo_UnrelatedTribes_NotActive()
    {
        var board = new List<Card>
        {
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups }),
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        // Cups combo partner is Wands, not Swords
        Assert.IsFalse(IsComboActive(snapshot, TribeType.Cups, TribeType.Swords),
            "Cups+Swords is NOT a valid combo pair");
    }

    [Test]
    public void Combo_MultiTribeCards_CanActivateCombo()
    {
        // A dual Pentacles/Cups card + 1 Pentacles + 1 Cups = 2 Pentacles, 2 Cups
        var board = new List<Card>
        {
            CreateCard("PentCup", new[] { TribeType.Pentacles, TribeType.Cups }),
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Cup1", new[] { TribeType.Cups })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(IsComboActive(snapshot, TribeType.Pentacles, TribeType.Cups),
            "Multi-tribe card should enable combo activation");
    }

    [Test]
    public void Combo_SymmetricCheck_OrderDoesNotMatter()
    {
        var board = SynergyTestCards.CreateTestBoard_PentaclesCups();
        var snapshot = synergyManager.CalculateSynergies(board);

        bool checkAB = IsComboActive(snapshot, TribeType.Pentacles, TribeType.Cups);
        bool checkBA = IsComboActive(snapshot, TribeType.Cups, TribeType.Pentacles);

        Assert.AreEqual(checkAB, checkBA, "Combo check should be symmetric (A+B == B+A)");
    }

    [Test]
    public void Combo_MultipleCombos_CanBeActiveSimultaneously()
    {
        // Board with 2 of each: Pentacles, Cups, Swords, Pentacles
        // This should activate Pentacles+Cups combo AND Swords+Pentacles combo
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles }),
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups }),
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(snapshot.activeCombos.Count >= 2, $"Expected at least 2 combos, got {snapshot.activeCombos.Count}");
    }

    // =====================================================
    // SYNERGY EFFECT APPLICATION TESTS
    // =====================================================

    [Test]
    public void Effect_SwordsBuffAttack_IncreasesAttack()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }, attack: 2, health: 3),
            CreateCard("Sword2", new[] { TribeType.Swords }, attack: 3, health: 2)
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        int originalAtk1 = board[0].attack;
        int originalAtk2 = board[1].attack;

        // Swords tier 2: +1 attack to all tribe members at StartOfCombat
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null, snapshot);

        Assert.AreEqual(originalAtk1 + 1, board[0].attack, "Sword card 1 should gain +1 attack");
        Assert.AreEqual(originalAtk2 + 1, board[1].attack, "Sword card 2 should gain +1 attack");
    }

    [Test]
    public void Effect_SwordsBuffAttack_DoesNotAffectNonTribeCards()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }, attack: 2),
            CreateCard("Sword2", new[] { TribeType.Swords }, attack: 3),
            CreateCard("Cup1", new[] { TribeType.Cups }, attack: 1)
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        int cupOriginalAtk = board[2].attack;
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null, snapshot);

        Assert.AreEqual(cupOriginalAtk, board[2].attack,
            "Non-Swords card should not receive Swords attack buff");
    }

    [Test]
    public void Effect_WandsTier6_BuffsAllFriendly()
    {
        var board = SynergyTestCards.CreateTestBoard_WandsTier6();
        var snapshot = synergyManager.CalculateSynergies(board);

        int[] originalAttacks = board.Select(c => c.attack).ToArray();

        // Wands tier 6: +2 attack to all friendly at StartOfCombat
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null, snapshot);

        for (int i = 0; i < board.Count; i++)
        {
            Assert.AreEqual(originalAttacks[i] + 2, board[i].attack,
                $"Card {i} ({board[i].cardName}) should gain +2 attack from Wands tier 6");
        }
    }

    [Test]
    public void Effect_CupsTier6_GrantsAegisToAll()
    {
        var board = new List<Card>
        {
            CreateCard("Cup1", new[] { TribeType.Cups }),
            CreateCard("Cup2", new[] { TribeType.Cups }),
            CreateCard("Cup3", new[] { TribeType.Cups }),
            CreateCard("Cup4", new[] { TribeType.Cups }),
            CreateCard("Cup5", new[] { TribeType.Cups }),
            CreateCard("Cup6", new[] { TribeType.Cups }),
            CreateCard("Neutral", new TribeType[0]) // Non-Cup also gets Aegis (AllFriendly)
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null, snapshot);

        foreach (var card in board)
        {
            Assert.IsTrue(card.hasAegis,
                $"{card.cardName} should have Aegis from Cups tier 6");
        }
    }

    [Test]
    public void Effect_SellBonus_PentaclesTier2()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        int bonus = synergyManager.GetSellBonus(board[0], snapshot);
        Assert.AreEqual(1, bonus, "Pentacles tier 2 should give +1 sell bonus");
    }

    [Test]
    public void Effect_CostReduction_PentaclesTier6()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles }),
            CreateCard("Pent3", new[] { TribeType.Pentacles }),
            CreateCard("Pent4", new[] { TribeType.Pentacles }),
            CreateCard("Pent5", new[] { TribeType.Pentacles }),
            CreateCard("Pent6", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        int reduction = synergyManager.GetCostReduction(board[0], snapshot);
        Assert.AreEqual(1, reduction, "Pentacles tier 6 should give -1 cost reduction");
    }

    // =====================================================
    // BOARD UPDATE / RECALCULATION TESTS
    // =====================================================

    [Test]
    public void Update_AddingCards_RecalculatesTiers()
    {
        // Start with 1 Sword (no tier)
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords })
        };

        var snapshot1 = synergyManager.CalculateSynergies(board);
        Assert.IsNull(GetActiveTier(snapshot1, TribeType.Swords));

        // Add second Sword (tier 2)
        board.Add(CreateCard("Sword2", new[] { TribeType.Swords }));
        var snapshot2 = synergyManager.CalculateSynergies(board);

        Assert.IsNotNull(GetActiveTier(snapshot2, TribeType.Swords),
            "Adding a second Sword should activate tier 2");
    }

    [Test]
    public void Update_RemovingCards_RecalculatesTiers()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords })
        };

        var snapshot1 = synergyManager.CalculateSynergies(board);
        Assert.IsNotNull(GetActiveTier(snapshot1, TribeType.Swords));

        // Remove a Sword
        board.RemoveAt(1);
        var snapshot2 = synergyManager.CalculateSynergies(board);

        Assert.IsNull(GetActiveTier(snapshot2, TribeType.Swords),
            "Removing a Sword should deactivate tier 2");
    }

    // =====================================================
    // PREBUILT BOARD TESTS (using SynergyTestCards)
    // =====================================================

    [Test]
    public void PrebuiltBoard_PentaclesCups_BothTier2AndCombo()
    {
        var board = SynergyTestCards.CreateTestBoard_PentaclesCups();
        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Pentacles));
        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Cups));
        Assert.IsTrue(IsComboActive(snapshot, TribeType.Pentacles, TribeType.Cups));
    }

    [Test]
    public void PrebuiltBoard_SwordsTier4_CorrectTier()
    {
        var board = SynergyTestCards.CreateTestBoard_SwordsTier4();
        var snapshot = synergyManager.CalculateSynergies(board);

        var tier = GetActiveTier(snapshot, TribeType.Swords);
        Assert.IsNotNull(tier);
        Assert.AreEqual(4, tier.threshold);
    }

    [Test]
    public void PrebuiltBoard_AllTribes_HasMultipleActiveTiers()
    {
        var board = SynergyTestCards.CreateTestBoard_AllTribes();
        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(snapshot.activeTiers.Count >= 2,
            $"All-tribes board should activate multiple synergies, got {snapshot.activeTiers.Count}");
    }

    // =====================================================
    // STARS TRIBE TESTS
    // =====================================================

    [Test]
    public void Stars_TribeCounting_TwoMembers_CountsTwo()
    {
        // Stars synergy is auto-generated by EnsureDefaultSynergies — no manual setup required.
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }),
            CreateCard("Star2", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Stars),
            "Two Stars cards should yield a tribe count of 2");
    }

    [Test]
    public void Stars_TribeCounting_FourMembers_CountsFour()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }),
            CreateCard("Star2", new[] { TribeType.Stars }),
            CreateCard("Star3", new[] { TribeType.Stars }),
            CreateCard("Star4", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(4, GetTribeCount(snapshot, TribeType.Stars));
    }

    [Test]
    public void Stars_TribeCounting_SixMembers_CountsSix()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }),
            CreateCard("Star2", new[] { TribeType.Stars }),
            CreateCard("Star3", new[] { TribeType.Stars }),
            CreateCard("Star4", new[] { TribeType.Stars }),
            CreateCard("Star5", new[] { TribeType.Stars }),
            CreateCard("Star6", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(6, GetTribeCount(snapshot, TribeType.Stars));
    }

    [Test]
    public void Stars_Tier2_ActivatesAtTwoMembers()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }),
            CreateCard("Star2", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Stars);

        Assert.IsNotNull(tier, "2 Stars should activate tier 2");
        Assert.AreEqual(2, tier.threshold);
        Assert.AreEqual(SynergyTrigger.StartOfCombat, tier.trigger);
        Assert.AreEqual(SynergyEffect.BuffAttack, tier.effect);
        Assert.AreEqual(SynergyTarget.AllTribeMembers, tier.target);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Stars_Tier4_ActivatesAtFourMembers()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }),
            CreateCard("Star2", new[] { TribeType.Stars }),
            CreateCard("Star3", new[] { TribeType.Stars }),
            CreateCard("Star4", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Stars);

        Assert.IsNotNull(tier, "4 Stars should activate tier 4");
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BuffStats, tier.effect);
        Assert.AreEqual(2, tier.value);
    }

    [Test]
    public void Stars_Tier6_ActivatesAtSixMembers()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }),
            CreateCard("Star2", new[] { TribeType.Stars }),
            CreateCard("Star3", new[] { TribeType.Stars }),
            CreateCard("Star4", new[] { TribeType.Stars }),
            CreateCard("Star5", new[] { TribeType.Stars }),
            CreateCard("Star6", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Stars);

        Assert.IsNotNull(tier, "6 Stars should activate tier 6");
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyEffect.BuffStats, tier.effect);
        Assert.AreEqual(3, tier.value);
    }

    [Test]
    public void Stars_Tier2_BuffsAttackAtStartOfCombat()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }, attack: 2, health: 3),
            CreateCard("Star2", new[] { TribeType.Stars }, attack: 1, health: 4)
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        int originalAtk0 = board[0].attack;
        int originalAtk1 = board[1].attack;

        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null, snapshot);

        Assert.AreEqual(originalAtk0 + 1, board[0].attack, "Star1 should gain +1 attack from Stars tier 2");
        Assert.AreEqual(originalAtk1 + 1, board[1].attack, "Star2 should gain +1 attack from Stars tier 2");
    }

    [Test]
    public void Stars_Tier6_BuffsStatsAtStartOfCombat()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }, attack: 2, health: 3),
            CreateCard("Star2", new[] { TribeType.Stars }, attack: 1, health: 4),
            CreateCard("Star3", new[] { TribeType.Stars }, attack: 3, health: 2),
            CreateCard("Star4", new[] { TribeType.Stars }, attack: 2, health: 2),
            CreateCard("Star5", new[] { TribeType.Stars }, attack: 1, health: 1),
            CreateCard("Star6", new[] { TribeType.Stars }, attack: 4, health: 5)
        };

        int[] origAtk = board.Select(c => c.attack).ToArray();
        int[] origHp  = board.Select(c => c.health).ToArray();

        var snapshot = synergyManager.CalculateSynergies(board);
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null, snapshot);

        // Tier 6 grants +3/+3 (value=3, BuffStats). Tier 2 (+1 attack) and tier 4 (+2/+2)
        // also fire because tiers stack, but we only validate tier 6 contribution here.
        // After all stacked tiers: each card gains +1 atk (T2) + +2/+2 (T4) + +3/+3 (T6)
        // = +6 attack and +5 health total.
        for (int i = 0; i < board.Count; i++)
        {
            Assert.IsTrue(board[i].attack > origAtk[i],
                $"Stars card {i} attack should increase from Stars tier 6 stacked effects");
            Assert.IsTrue(board[i].health > origHp[i],
                $"Stars card {i} health should increase from Stars tier 6 stacked effects");
        }
    }

    [Test]
    public void Stars_Combo_StarsAndSwords_ActiveWithTwoEach()
    {
        var board = new List<Card>
        {
            CreateCard("Star1",  new[] { TribeType.Stars }),
            CreateCard("Star2",  new[] { TribeType.Stars }),
            CreateCard("Sword1", new[] { TribeType.Swords }),
            CreateCard("Sword2", new[] { TribeType.Swords })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(IsComboActive(snapshot, TribeType.Stars, TribeType.Swords),
            "Stars+Swords combo should be active with 2 of each tribe");
    }

    [Test]
    public void Stars_MultiTribeCard_ContributesToStarsCount()
    {
        // A Stars/Cups dual-tribe card should count toward both Stars and Cups thresholds.
        var board = new List<Card>
        {
            CreateCard("StarCup", new[] { TribeType.Stars, TribeType.Cups }),
            CreateCard("Star2",   new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Stars),
            "Dual-tribe Stars/Cups card should contribute 1 to Stars count");
        Assert.AreEqual(1, GetTribeCount(snapshot, TribeType.Cups),
            "Dual-tribe Stars/Cups card should contribute 1 to Cups count");
        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Stars),
            "2 Stars (including dual-tribe card) should activate Stars tier 2");
    }

    // =====================================================
    // COINS TRIBE TESTS
    // =====================================================

    [Test]
    public void Coins_TribeCounting_TwoMembers_CountsTwo()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Coins),
            "Two Coins cards should yield a tribe count of 2");
    }

    [Test]
    public void Coins_TribeCounting_FourMembers_CountsFour()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins }),
            CreateCard("Coin3", new[] { TribeType.Coins }),
            CreateCard("Coin4", new[] { TribeType.Coins })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(4, GetTribeCount(snapshot, TribeType.Coins));
    }

    [Test]
    public void Coins_Tier2_ActivatesAtTwoMembers_OnBuyTrigger()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Coins);

        Assert.IsNotNull(tier, "2 Coins should activate tier 2");
        Assert.AreEqual(2, tier.threshold);
        Assert.AreEqual(SynergyTrigger.OnBuy, tier.trigger,
            "Coins tier 2 should have OnBuy trigger");
        Assert.AreEqual(SynergyEffect.BonusGold, tier.effect,
            "Coins tier 2 should grant BonusGold");
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Coins_Tier4_ActivatesAtFourMembers_PassiveBuffHealth()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins }),
            CreateCard("Coin3", new[] { TribeType.Coins }),
            CreateCard("Coin4", new[] { TribeType.Coins })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Coins);

        Assert.IsNotNull(tier, "4 Coins should activate tier 4");
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyTrigger.Passive, tier.trigger,
            "Coins tier 4 should be Passive");
        Assert.AreEqual(SynergyEffect.BuffHealth, tier.effect,
            "Coins tier 4 should grant BuffHealth");
        Assert.AreEqual(2, tier.value);
    }

    [Test]
    public void Coins_Tier6_ActivatesAtSixMembers_StartOfCombatBuffStats()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins }),
            CreateCard("Coin3", new[] { TribeType.Coins }),
            CreateCard("Coin4", new[] { TribeType.Coins }),
            CreateCard("Coin5", new[] { TribeType.Coins }),
            CreateCard("Coin6", new[] { TribeType.Coins })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Coins);

        Assert.IsNotNull(tier, "6 Coins should activate tier 6");
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyTrigger.StartOfCombat, tier.trigger);
        Assert.AreEqual(SynergyEffect.BuffStats, tier.effect);
        Assert.AreEqual(2, tier.value);
    }

    [Test]
    public void Coins_Tier6_BuffsStatsOnAllCoinsMembersAtStartOfCombat()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }, attack: 1, health: 2),
            CreateCard("Coin2", new[] { TribeType.Coins }, attack: 2, health: 3),
            CreateCard("Coin3", new[] { TribeType.Coins }, attack: 1, health: 1),
            CreateCard("Coin4", new[] { TribeType.Coins }, attack: 3, health: 2),
            CreateCard("Coin5", new[] { TribeType.Coins }, attack: 2, health: 2),
            CreateCard("Coin6", new[] { TribeType.Coins }, attack: 1, health: 4)
        };

        int[] origAtk = board.Select(c => c.attack).ToArray();
        int[] origHp  = board.Select(c => c.health).ToArray();

        var snapshot = synergyManager.CalculateSynergies(board);
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null, snapshot);

        for (int i = 0; i < board.Count; i++)
        {
            Assert.IsTrue(board[i].attack > origAtk[i],
                $"Coin card {i} attack should increase from Coins tier 6 StartOfCombat buff");
            Assert.IsTrue(board[i].health > origHp[i],
                $"Coin card {i} health should increase from Coins tier 6 StartOfCombat buff");
        }
    }

    [Test]
    public void Coins_Combo_CoinsAndPentacles_ActiveWithTwoEach()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins }),
            CreateCard("Pent1", new[] { TribeType.Pentacles }),
            CreateCard("Pent2", new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsTrue(IsComboActive(snapshot, TribeType.Coins, TribeType.Pentacles),
            "Coins+Pentacles combo should be active with 2 of each tribe");
    }

    [Test]
    public void Coins_Combo_CoinsAndPentacles_InactiveWithOnePentacles()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins }),
            CreateCard("Pent1", new[] { TribeType.Pentacles }) // Only 1 Pentacles
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsFalse(IsComboActive(snapshot, TribeType.Coins, TribeType.Pentacles),
            "Coins+Pentacles combo should NOT be active with fewer than 2 Pentacles");
    }

    [Test]
    public void Coins_MultiTribeCard_ContributesToCoinsCount()
    {
        // A Coins/Pentacles dual-tribe card should push both tribe counts to 2,
        // activating Stars and Coins tier 2 without needing 4 distinct cards.
        var board = new List<Card>
        {
            CreateCard("CoinPent", new[] { TribeType.Coins, TribeType.Pentacles }),
            CreateCard("Coin2",    new[] { TribeType.Coins }),
            CreateCard("Pent2",    new[] { TribeType.Pentacles })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Coins),
            "Dual-tribe Coins/Pentacles card contributes 1 to Coins count");
        Assert.AreEqual(2, GetTribeCount(snapshot, TribeType.Pentacles),
            "Dual-tribe Coins/Pentacles card contributes 1 to Pentacles count");
        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Coins),
            "2 Coins should activate Coins tier 2");
        Assert.IsNotNull(GetActiveTier(snapshot, TribeType.Pentacles),
            "2 Pentacles should activate Pentacles tier 2");
    }

    // =====================================================
    // STARS / COINS ISOLATION TESTS
    // =====================================================

    [Test]
    public void Stars_OneMember_NoTierActive()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsNull(GetActiveTier(snapshot, TribeType.Stars),
            "A single Stars card should NOT activate any tier");
    }

    [Test]
    public void Coins_OneMember_NoTierActive()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins })
        };

        var snapshot = synergyManager.CalculateSynergies(board);

        Assert.IsNull(GetActiveTier(snapshot, TribeType.Coins),
            "A single Coins card should NOT activate any tier");
    }

    [Test]
    public void Stars_ThreeMembers_StillActivatesTier2()
    {
        var board = new List<Card>
        {
            CreateCard("Star1", new[] { TribeType.Stars }),
            CreateCard("Star2", new[] { TribeType.Stars }),
            CreateCard("Star3", new[] { TribeType.Stars })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Stars);

        Assert.IsNotNull(tier, "3 Stars should still activate at least tier 2");
        Assert.AreEqual(2, tier.threshold,
            "With 3 Stars the highest active tier is tier 2 (not tier 4 which needs 4)");
    }

    [Test]
    public void Coins_FiveMembers_StillActivatesTier4()
    {
        var board = new List<Card>
        {
            CreateCard("Coin1", new[] { TribeType.Coins }),
            CreateCard("Coin2", new[] { TribeType.Coins }),
            CreateCard("Coin3", new[] { TribeType.Coins }),
            CreateCard("Coin4", new[] { TribeType.Coins }),
            CreateCard("Coin5", new[] { TribeType.Coins })
        };

        var snapshot = synergyManager.CalculateSynergies(board);
        var tier = GetActiveTier(snapshot, TribeType.Coins);

        Assert.IsNotNull(tier, "5 Coins should still activate at least tier 4");
        Assert.AreEqual(4, tier.threshold,
            "With 5 Coins the highest active tier is tier 4 (not tier 6 which needs 6)");
    }
}

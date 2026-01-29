using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Automated tests for tribe synergy system.
/// Covers threshold activation (2/4/6), cross-tribe combos,
/// multi-tribe card counting, and synergy effect application.
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

    // =====================================================
    // TRIBE COUNTING TESTS
    // =====================================================

    [Test]
    public void TribeCounting_EmptyBoard_NoTribes()
    {
        var board = new List<Card>();
        synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Pentacles));
        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Cups));
        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Wands));
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

        synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(2, synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(1, synergyManager.GetTribeCount(TribeType.Cups));
        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Pentacles));
        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Wands));
    }

    [Test]
    public void TribeCounting_MultiTribeCard_CountsBothTribes()
    {
        var board = new List<Card>
        {
            CreateCard("DualCard", new[] { TribeType.Pentacles, TribeType.Cups })
        };

        synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(1, synergyManager.GetTribeCount(TribeType.Pentacles));
        Assert.AreEqual(1, synergyManager.GetTribeCount(TribeType.Cups));
    }

    [Test]
    public void TribeCounting_TripleTribeCard_CountsAllThree()
    {
        var board = new List<Card>
        {
            CreateCard("TripleCard", new[] { TribeType.Cups, TribeType.Wands, TribeType.Swords })
        };

        synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(1, synergyManager.GetTribeCount(TribeType.Cups));
        Assert.AreEqual(1, synergyManager.GetTribeCount(TribeType.Wands));
        Assert.AreEqual(1, synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Pentacles));
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

        synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(2, synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(2, synergyManager.GetTribeCount(TribeType.Pentacles));
    }

    [Test]
    public void TribeCounting_NullBoard_NoError()
    {
        synergyManager.UpdateTribeCounts(null);
        Assert.AreEqual(0, synergyManager.GetTribeCount(TribeType.Swords));
    }

    [Test]
    public void TribeCounting_CardWithNoTribes_NotCounted()
    {
        var board = new List<Card>
        {
            CreateCard("Neutral", new TribeType[0]),
            CreateCard("Sword1", new[] { TribeType.Swords })
        };

        synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(1, synergyManager.GetTribeCount(TribeType.Swords));
        var allCounts = synergyManager.GetAllTribeCounts();
        Assert.AreEqual(1, allCounts.Count, "Only one tribe should be counted");
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsNull(synergyManager.GetActiveTier(TribeType.Swords),
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Swords);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier, "4 Swords should activate tier 4");
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BonusDamage, tier.effect);
    }

    [Test]
    public void Threshold_SixWands_Tier6Active()
    {
        var board = SynergyTestCards.CreateTestBoard_WandsTier6();
        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Wands);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Pentacles);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Cups);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Wands);
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Pentacles), "Pentacles tier 2 should be active");
        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Cups), "Cups tier 2 should be active");
        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Swords), "Swords tier 2 should be active");
        Assert.IsNull(synergyManager.GetActiveTier(TribeType.Wands), "Wands should NOT be active (only 1)");
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Pentacles), "Pentacles should be active at tier 2");
        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Cups), "Cups should be active at tier 2");
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Pentacles);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Cups);
        Assert.AreEqual(SynergyTrigger.EndOfTurn, tier.trigger);
        Assert.AreEqual(SynergyEffect.HealFlat, tier.effect);
        Assert.AreEqual(SynergyTarget.Adjacent, tier.target);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Swords_Tier4_BonusDamage()
    {
        var board = SynergyTestCards.CreateTestBoard_SwordsTier4();
        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Swords);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Wands);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Cups);
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

        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Pentacles);
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
        synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups),
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsFalse(synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups),
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(synergyManager.IsComboActive(TribeType.Swords, TribeType.Pentacles));
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(synergyManager.IsComboActive(TribeType.Wands, TribeType.Swords));
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(synergyManager.IsComboActive(TribeType.Cups, TribeType.Wands));
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

        synergyManager.UpdateTribeCounts(board);

        // Cups combo partner is Wands, not Swords
        Assert.IsFalse(synergyManager.IsComboActive(TribeType.Cups, TribeType.Swords),
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

        synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups),
            "Multi-tribe card should enable combo activation");
    }

    [Test]
    public void Combo_SymmetricCheck_OrderDoesNotMatter()
    {
        var board = SynergyTestCards.CreateTestBoard_PentaclesCups();
        synergyManager.UpdateTribeCounts(board);

        bool checkAB = synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups);
        bool checkBA = synergyManager.IsComboActive(TribeType.Cups, TribeType.Pentacles);

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

        synergyManager.UpdateTribeCounts(board);

        var combos = synergyManager.GetActiveCombos();
        Assert.IsTrue(combos.Count >= 2, $"Expected at least 2 combos, got {combos.Count}");
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

        synergyManager.UpdateTribeCounts(board);

        int originalAtk1 = board[0].attack;
        int originalAtk2 = board[1].attack;

        // Swords tier 2: +1 attack to all tribe members at StartOfCombat
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null);

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

        synergyManager.UpdateTribeCounts(board);

        int cupOriginalAtk = board[2].attack;
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null);

        Assert.AreEqual(cupOriginalAtk, board[2].attack,
            "Non-Swords card should not receive Swords attack buff");
    }

    [Test]
    public void Effect_WandsTier6_BuffsAllFriendly()
    {
        var board = SynergyTestCards.CreateTestBoard_WandsTier6();
        synergyManager.UpdateTribeCounts(board);

        int[] originalAttacks = board.Select(c => c.attack).ToArray();

        // Wands tier 6: +2 attack to all friendly at StartOfCombat
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null);

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

        synergyManager.UpdateTribeCounts(board);
        synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null);

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

        synergyManager.UpdateTribeCounts(board);

        int bonus = synergyManager.GetSellBonus(board[0]);
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

        synergyManager.UpdateTribeCounts(board);

        int reduction = synergyManager.GetCostReduction(board[0]);
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

        synergyManager.UpdateTribeCounts(board);
        Assert.IsNull(synergyManager.GetActiveTier(TribeType.Swords));

        // Add second Sword (tier 2)
        board.Add(CreateCard("Sword2", new[] { TribeType.Swords }));
        synergyManager.UpdateTribeCounts(board);

        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Swords),
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

        synergyManager.UpdateTribeCounts(board);
        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Swords));

        // Remove a Sword
        board.RemoveAt(1);
        synergyManager.UpdateTribeCounts(board);

        Assert.IsNull(synergyManager.GetActiveTier(TribeType.Swords),
            "Removing a Sword should deactivate tier 2");
    }

    // =====================================================
    // PREBUILT BOARD TESTS (using SynergyTestCards)
    // =====================================================

    [Test]
    public void PrebuiltBoard_PentaclesCups_BothTier2AndCombo()
    {
        var board = SynergyTestCards.CreateTestBoard_PentaclesCups();
        synergyManager.UpdateTribeCounts(board);

        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Pentacles));
        Assert.IsNotNull(synergyManager.GetActiveTier(TribeType.Cups));
        Assert.IsTrue(synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups));
    }

    [Test]
    public void PrebuiltBoard_SwordsTier4_CorrectTier()
    {
        var board = SynergyTestCards.CreateTestBoard_SwordsTier4();
        synergyManager.UpdateTribeCounts(board);

        var tier = synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier);
        Assert.AreEqual(4, tier.threshold);
    }

    [Test]
    public void PrebuiltBoard_AllTribes_HasMultipleActiveTiers()
    {
        var board = SynergyTestCards.CreateTestBoard_AllTribes();
        synergyManager.UpdateTribeCounts(board);

        var allTiers = synergyManager.GetAllActiveTiers();
        Assert.IsTrue(allTiers.Count >= 2,
            $"All-tribes board should activate multiple synergies, got {allTiers.Count}");
    }
}

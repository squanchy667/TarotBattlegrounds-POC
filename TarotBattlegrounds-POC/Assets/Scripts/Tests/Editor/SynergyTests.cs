using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tests for tribe synergy mechanics including tier thresholds (2/4/6) and cross-tribe combos.
/// Task C13 & C14 from PLAN.md
/// </summary>
[TestFixture]
public class SynergyTests
{
    private SynergyManager _synergyManager;
    private GameObject _synergyManagerObj;
    private GameObject _themeManagerObj;

    [SetUp]
    public void SetUp()
    {
        // Create ThemeManager first
        _themeManagerObj = new GameObject("ThemeManager");
        _themeManagerObj.AddComponent<ThemeManager>();

        // Create SynergyManager
        _synergyManagerObj = new GameObject("SynergyManager");
        _synergyManager = _synergyManagerObj.AddComponent<SynergyManager>();

        // Initialize with test synergies
        _synergyManager.tribeSynergies = SynergyTestData.CreateAllTribeSynergies();
        _synergyManager.InitializeSynergyCache();
    }

    [TearDown]
    public void TearDown()
    {
        if (_synergyManagerObj != null)
            Object.DestroyImmediate(_synergyManagerObj);
        if (_themeManagerObj != null)
            Object.DestroyImmediate(_themeManagerObj);
    }

    #region Helper Methods

    private Card CreateCard(string name, TribeType tribe, int attack = 2, int health = 2)
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tribes = new TribeType[] { tribe };
        card.attack = attack;
        card.health = health;
        card.tier = 1;
        return card;
    }

    private Card CreateMultiTribeCard(string name, TribeType[] tribes, int attack = 2, int health = 2)
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tribes = tribes;
        card.attack = attack;
        card.health = health;
        card.tier = 1;
        return card;
    }

    private List<Card> CreateBoard(TribeType tribe, int count)
    {
        var board = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            board.Add(CreateCard($"{tribe}_{i}", tribe));
        }
        return board;
    }

    #endregion

    #region Tribe Counting Tests

    [Test]
    public void TribeCount_SingleTribe_CountsCorrectly()
    {
        var board = CreateBoard(TribeType.Swords, 3);
        _synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(3, _synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(0, _synergyManager.GetTribeCount(TribeType.Cups));
    }

    [Test]
    public void TribeCount_MixedTribes_CountsEachCorrectly()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", TribeType.Swords),
            CreateCard("Sword2", TribeType.Swords),
            CreateCard("Cup1", TribeType.Cups),
            CreateCard("Wand1", TribeType.Wands),
        };
        _synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(2, _synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(1, _synergyManager.GetTribeCount(TribeType.Cups));
        Assert.AreEqual(1, _synergyManager.GetTribeCount(TribeType.Wands));
        Assert.AreEqual(0, _synergyManager.GetTribeCount(TribeType.Pentacles));
    }

    [Test]
    public void TribeCount_MultiTribeCard_CountsForBothTribes()
    {
        var board = new List<Card>
        {
            CreateMultiTribeCard("MultiTribe", new[] { TribeType.Swords, TribeType.Cups }),
            CreateCard("Sword1", TribeType.Swords),
        };
        _synergyManager.UpdateTribeCounts(board);

        // Multi-tribe card should count for both
        Assert.AreEqual(2, _synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(1, _synergyManager.GetTribeCount(TribeType.Cups));
    }

    [Test]
    public void TribeCount_EmptyBoard_AllZero()
    {
        var board = new List<Card>();
        _synergyManager.UpdateTribeCounts(board);

        Assert.AreEqual(0, _synergyManager.GetTribeCount(TribeType.Swords));
        Assert.AreEqual(0, _synergyManager.GetTribeCount(TribeType.Cups));
        Assert.AreEqual(0, _synergyManager.GetTribeCount(TribeType.Wands));
        Assert.AreEqual(0, _synergyManager.GetTribeCount(TribeType.Pentacles));
    }

    #endregion

    #region Tier Threshold Tests - Swords (Aggro)

    [Test]
    public void Swords_Tier2_ActivatesAt2Members()
    {
        var board = CreateBoard(TribeType.Swords, 2);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier, "Swords tier should be active at 2 members");
        Assert.AreEqual(2, tier.threshold);
        Assert.AreEqual(SynergyEffect.BuffAttack, tier.effect);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Swords_Tier4_ActivatesAt4Members()
    {
        var board = CreateBoard(TribeType.Swords, 4);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier, "Swords tier should be active at 4 members");
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BonusDamage, tier.effect);
        Assert.AreEqual(2, tier.value);
    }

    [Test]
    public void Swords_Tier6_ActivatesAt6Members()
    {
        var board = CreateBoard(TribeType.Swords, 6);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier, "Swords tier should be active at 6 members");
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyEffect.Cleave, tier.effect);
    }

    [Test]
    public void Swords_NoTier_At1Member()
    {
        var board = CreateBoard(TribeType.Swords, 1);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNull(tier, "Swords should not have active tier at 1 member");
    }

    #endregion

    #region Tier Threshold Tests - Cups (Healing)

    [Test]
    public void Cups_Tier2_HealsAdjacent()
    {
        var board = CreateBoard(TribeType.Cups, 2);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Cups);
        Assert.IsNotNull(tier);
        Assert.AreEqual(2, tier.threshold);
        Assert.AreEqual(SynergyTrigger.EndOfTurn, tier.trigger);
        Assert.AreEqual(SynergyEffect.HealFlat, tier.effect);
        Assert.AreEqual(SynergyTarget.Adjacent, tier.target);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Cups_Tier4_HealsTribeMembers()
    {
        var board = CreateBoard(TribeType.Cups, 4);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Cups);
        Assert.IsNotNull(tier);
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.HealFlat, tier.effect);
        Assert.AreEqual(SynergyTarget.AllTribeMembers, tier.target);
        Assert.AreEqual(2, tier.value);
    }

    [Test]
    public void Cups_Tier6_GrantsAegisToAll()
    {
        var board = CreateBoard(TribeType.Cups, 6);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Cups);
        Assert.IsNotNull(tier);
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyTrigger.StartOfCombat, tier.trigger);
        Assert.AreEqual(SynergyEffect.Shield, tier.effect);
        Assert.AreEqual(SynergyTarget.AllFriendly, tier.target);
    }

    #endregion

    #region Tier Threshold Tests - Wands (Buffs)

    [Test]
    public void Wands_Tier2_BuffsRandomFriendly()
    {
        var board = CreateBoard(TribeType.Wands, 2);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Wands);
        Assert.IsNotNull(tier);
        Assert.AreEqual(2, tier.threshold);
        Assert.AreEqual(SynergyTrigger.EndOfTurn, tier.trigger);
        Assert.AreEqual(SynergyEffect.BuffStats, tier.effect);
        Assert.AreEqual(SynergyTarget.Random, tier.target);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Wands_Tier4_BuffsAllWands()
    {
        var board = CreateBoard(TribeType.Wands, 4);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Wands);
        Assert.IsNotNull(tier);
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BuffStats, tier.effect);
        Assert.AreEqual(SynergyTarget.AllTribeMembers, tier.target);
    }

    [Test]
    public void Wands_Tier6_BuffsAllFriendlyAttack()
    {
        var board = CreateBoard(TribeType.Wands, 6);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Wands);
        Assert.IsNotNull(tier);
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyTrigger.StartOfCombat, tier.trigger);
        Assert.AreEqual(SynergyEffect.BuffAttack, tier.effect);
        Assert.AreEqual(SynergyTarget.AllFriendly, tier.target);
        Assert.AreEqual(2, tier.value);
    }

    #endregion

    #region Tier Threshold Tests - Pentacles (Economy)

    [Test]
    public void Pentacles_Tier2_BonusGoldOnSell()
    {
        var board = CreateBoard(TribeType.Pentacles, 2);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Pentacles);
        Assert.IsNotNull(tier);
        Assert.AreEqual(2, tier.threshold);
        Assert.AreEqual(SynergyTrigger.OnSell, tier.trigger);
        Assert.AreEqual(SynergyEffect.BonusGold, tier.effect);
        Assert.AreEqual(1, tier.value);
    }

    [Test]
    public void Pentacles_Tier4_MoreBonusGoldOnSell()
    {
        var board = CreateBoard(TribeType.Pentacles, 4);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Pentacles);
        Assert.IsNotNull(tier);
        Assert.AreEqual(4, tier.threshold);
        Assert.AreEqual(SynergyEffect.BonusGold, tier.effect);
        Assert.AreEqual(2, tier.value);
    }

    [Test]
    public void Pentacles_Tier6_ReducesCost()
    {
        var board = CreateBoard(TribeType.Pentacles, 6);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Pentacles);
        Assert.IsNotNull(tier);
        Assert.AreEqual(6, tier.threshold);
        Assert.AreEqual(SynergyTrigger.Passive, tier.trigger);
        Assert.AreEqual(SynergyEffect.ReduceCost, tier.effect);
        Assert.AreEqual(1, tier.value);
    }

    #endregion

    #region Tier Threshold Edge Cases

    [Test]
    public void Tier_At3Members_UsesTier2()
    {
        var board = CreateBoard(TribeType.Swords, 3);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier);
        Assert.AreEqual(2, tier.threshold, "With 3 members, tier 2 should be active (not tier 4)");
    }

    [Test]
    public void Tier_At5Members_UsesTier4()
    {
        var board = CreateBoard(TribeType.Swords, 5);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier);
        Assert.AreEqual(4, tier.threshold, "With 5 members, tier 4 should be active (not tier 6)");
    }

    [Test]
    public void Tier_At7Members_UsesTier6()
    {
        var board = CreateBoard(TribeType.Swords, 7);
        _synergyManager.UpdateTribeCounts(board);

        var tier = _synergyManager.GetActiveTier(TribeType.Swords);
        Assert.IsNotNull(tier);
        Assert.AreEqual(6, tier.threshold, "With 7 members, tier 6 should be active");
    }

    #endregion

    #region Cross-Tribe Combo Tests (C14)

    [Test]
    public void Combo_PentaclesCups_ActivatesAt2Each()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", TribeType.Pentacles),
            CreateCard("Pent2", TribeType.Pentacles),
            CreateCard("Cup1", TribeType.Cups),
            CreateCard("Cup2", TribeType.Cups),
        };
        _synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(_synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups),
            "Pentacles + Cups combo should be active with 2 of each");
    }

    [Test]
    public void Combo_CupsWands_ActivatesAt2Each()
    {
        var board = new List<Card>
        {
            CreateCard("Cup1", TribeType.Cups),
            CreateCard("Cup2", TribeType.Cups),
            CreateCard("Wand1", TribeType.Wands),
            CreateCard("Wand2", TribeType.Wands),
        };
        _synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(_synergyManager.IsComboActive(TribeType.Cups, TribeType.Wands),
            "Cups + Wands combo should be active with 2 of each");
    }

    [Test]
    public void Combo_SwordsPentacles_ActivatesAt2Each()
    {
        var board = new List<Card>
        {
            CreateCard("Sword1", TribeType.Swords),
            CreateCard("Sword2", TribeType.Swords),
            CreateCard("Pent1", TribeType.Pentacles),
            CreateCard("Pent2", TribeType.Pentacles),
        };
        _synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(_synergyManager.IsComboActive(TribeType.Swords, TribeType.Pentacles),
            "Swords + Pentacles combo should be active with 2 of each");
    }

    [Test]
    public void Combo_WandsSwords_ActivatesAt2Each()
    {
        var board = new List<Card>
        {
            CreateCard("Wand1", TribeType.Wands),
            CreateCard("Wand2", TribeType.Wands),
            CreateCard("Sword1", TribeType.Swords),
            CreateCard("Sword2", TribeType.Swords),
        };
        _synergyManager.UpdateTribeCounts(board);

        Assert.IsTrue(_synergyManager.IsComboActive(TribeType.Wands, TribeType.Swords),
            "Wands + Swords combo should be active with 2 of each");
    }

    [Test]
    public void Combo_NotActive_WhenOnlyOneTribeMet()
    {
        var board = new List<Card>
        {
            CreateCard("Pent1", TribeType.Pentacles),
            CreateCard("Pent2", TribeType.Pentacles),
            CreateCard("Cup1", TribeType.Cups), // Only 1 cup
        };
        _synergyManager.UpdateTribeCounts(board);

        Assert.IsFalse(_synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups),
            "Combo should not be active with only 1 cup");
    }

    [Test]
    public void Combo_MultiTribeCard_EnablesCombo()
    {
        // A multi-tribe card counting for both can enable a combo
        var board = new List<Card>
        {
            CreateMultiTribeCard("Multi", new[] { TribeType.Pentacles, TribeType.Cups }),
            CreateCard("Pent1", TribeType.Pentacles),
            CreateCard("Cup1", TribeType.Cups),
        };
        _synergyManager.UpdateTribeCounts(board);

        // Should have 2 Pentacles (multi + pent1) and 2 Cups (multi + cup1)
        Assert.AreEqual(2, _synergyManager.GetTribeCount(TribeType.Pentacles));
        Assert.AreEqual(2, _synergyManager.GetTribeCount(TribeType.Cups));
        Assert.IsTrue(_synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups),
            "Multi-tribe card should enable combo by counting for both tribes");
    }

    [Test]
    public void Combo_MultipleCombosCanBeActive()
    {
        // Board with enough cards to activate multiple combos
        var board = new List<Card>
        {
            CreateCard("Pent1", TribeType.Pentacles),
            CreateCard("Pent2", TribeType.Pentacles),
            CreateCard("Cup1", TribeType.Cups),
            CreateCard("Cup2", TribeType.Cups),
            CreateCard("Wand1", TribeType.Wands),
            CreateCard("Wand2", TribeType.Wands),
        };
        _synergyManager.UpdateTribeCounts(board);

        var activeCombos = _synergyManager.GetActiveCombos();
        Assert.IsTrue(activeCombos.Count >= 2,
            $"Should have multiple combos active, got {activeCombos.Count}");
        Assert.IsTrue(_synergyManager.IsComboActive(TribeType.Pentacles, TribeType.Cups));
        Assert.IsTrue(_synergyManager.IsComboActive(TribeType.Cups, TribeType.Wands));
    }

    #endregion

    #region Synergy Effect Application Tests

    [Test]
    public void SynergyEffect_BuffAttack_IncreasesAttack()
    {
        var board = CreateBoard(TribeType.Swords, 2);
        int originalAttack = board[0].attack;

        _synergyManager.UpdateTribeCounts(board);
        _synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null);

        // Swords (2) gives +1 attack to Swords members
        Assert.AreEqual(originalAttack + 1, board[0].attack,
            "Swords synergy should increase attack by 1");
    }

    [Test]
    public void SynergyEffect_BuffStats_IncreasesBoth()
    {
        var board = CreateBoard(TribeType.Wands, 4);
        int originalAttack = board[0].attack;
        int originalHealth = board[0].health;

        _synergyManager.UpdateTribeCounts(board);
        _synergyManager.TriggerSynergies(SynergyTrigger.EndOfTurn, board, null);

        // Wands (4) gives +1/+1 to all Wands
        Assert.AreEqual(originalAttack + 1, board[0].attack, "Wands synergy should increase attack");
        Assert.AreEqual(originalHealth + 1, board[0].health, "Wands synergy should increase health");
    }

    [Test]
    public void SynergyEffect_Shield_GrantsAegis()
    {
        var board = CreateBoard(TribeType.Cups, 6);
        Assert.IsFalse(board[0].hasAegis, "Card should not have Aegis initially");

        _synergyManager.UpdateTribeCounts(board);
        _synergyManager.TriggerSynergies(SynergyTrigger.StartOfCombat, board, null);

        // Cups (6) gives Aegis to all friendly
        Assert.IsTrue(board[0].hasAegis, "Cups (6) synergy should grant Aegis");
    }

    #endregion

    #region Sell Bonus Tests

    [Test]
    public void GetSellBonus_PentaclesTier2_Returns1()
    {
        var board = CreateBoard(TribeType.Pentacles, 2);
        _synergyManager.UpdateTribeCounts(board);

        int bonus = _synergyManager.GetSellBonus(board[0]);
        Assert.AreEqual(1, bonus, "Pentacles (2) should give +1 sell bonus");
    }

    [Test]
    public void GetSellBonus_PentaclesTier4_Returns2()
    {
        var board = CreateBoard(TribeType.Pentacles, 4);
        _synergyManager.UpdateTribeCounts(board);

        int bonus = _synergyManager.GetSellBonus(board[0]);
        Assert.AreEqual(2, bonus, "Pentacles (4) should give +2 sell bonus");
    }

    [Test]
    public void GetSellBonus_NonPentacles_Returns0()
    {
        var board = CreateBoard(TribeType.Swords, 4);
        _synergyManager.UpdateTribeCounts(board);

        int bonus = _synergyManager.GetSellBonus(board[0]);
        Assert.AreEqual(0, bonus, "Non-economy tribe should have 0 sell bonus");
    }

    #endregion

    #region Cost Reduction Tests

    [Test]
    public void GetCostReduction_PentaclesTier6_Returns1()
    {
        var board = CreateBoard(TribeType.Pentacles, 6);
        _synergyManager.UpdateTribeCounts(board);

        int reduction = _synergyManager.GetCostReduction(board[0]);
        Assert.AreEqual(1, reduction, "Pentacles (6) should give -1 cost reduction");
    }

    [Test]
    public void GetCostReduction_PentaclesTier4_Returns0()
    {
        var board = CreateBoard(TribeType.Pentacles, 4);
        _synergyManager.UpdateTribeCounts(board);

        int reduction = _synergyManager.GetCostReduction(board[0]);
        Assert.AreEqual(0, reduction, "Pentacles (4) should not give cost reduction");
    }

    #endregion
}

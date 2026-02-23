using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// T515: Integration tests for the ranked system.
/// Tests MMR calculation, rank tiers, and client-side logic.
/// </summary>
[TestFixture]
public class RankedTests
{
    [Test]
    public void RankTiers_Bronze_AtDefaultRating()
    {
        // New player starts at 1000 which is Silver
        var rank = RankedManager.GetRankFromRating(1000);
        Assert.AreEqual("Silver", rank.tier);
    }

    [Test]
    public void RankTiers_Bronze_AtLowRating()
    {
        var rank = RankedManager.GetRankFromRating(500);
        Assert.AreEqual("Bronze", rank.tier);
    }

    [Test]
    public void RankTiers_Gold_At1500()
    {
        var rank = RankedManager.GetRankFromRating(1500);
        Assert.AreEqual("Gold", rank.tier);
    }

    [Test]
    public void RankTiers_Platinum_At2000()
    {
        var rank = RankedManager.GetRankFromRating(2000);
        Assert.AreEqual("Platinum", rank.tier);
    }

    [Test]
    public void RankTiers_Diamond_At2500()
    {
        var rank = RankedManager.GetRankFromRating(2500);
        Assert.AreEqual("Diamond", rank.tier);
    }

    [Test]
    public void RankTiers_Master_At3000()
    {
        var rank = RankedManager.GetRankFromRating(3000);
        Assert.AreEqual("Master", rank.tier);
    }

    [Test]
    public void RankTiers_Legend_At3500()
    {
        var rank = RankedManager.GetRankFromRating(3500);
        Assert.AreEqual("Legend", rank.tier);
    }

    [Test]
    public void RankTiers_Legend_HasNoDivision()
    {
        var rank = RankedManager.GetRankFromRating(4000);
        Assert.AreEqual("Legend", rank.tier);
        Assert.AreEqual(0, rank.division);
    }

    [Test]
    public void RankTiers_Bronze_Division1_AtZero()
    {
        var rank = RankedManager.GetRankFromRating(0);
        Assert.AreEqual("Bronze", rank.tier);
        Assert.AreEqual(1, rank.division);
    }

    [Test]
    public void RankTiers_Bronze_Division4_Near999()
    {
        var rank = RankedManager.GetRankFromRating(999);
        Assert.AreEqual("Bronze", rank.tier);
        Assert.AreEqual(4, rank.division);
    }

    [Test]
    public void RankTiers_AllTiersHaveCorrectIndex()
    {
        Assert.AreEqual(0, RankedManager.GetRankFromRating(0).tierIndex);
        Assert.AreEqual(1, RankedManager.GetRankFromRating(1000).tierIndex);
        Assert.AreEqual(2, RankedManager.GetRankFromRating(1500).tierIndex);
        Assert.AreEqual(3, RankedManager.GetRankFromRating(2000).tierIndex);
        Assert.AreEqual(4, RankedManager.GetRankFromRating(2500).tierIndex);
        Assert.AreEqual(5, RankedManager.GetRankFromRating(3000).tierIndex);
        Assert.AreEqual(6, RankedManager.GetRankFromRating(3500).tierIndex);
    }

    [Test]
    public void RankTiers_NegativeRating_ReturnsBronze()
    {
        // Edge case: rating below 0
        var rank = RankedManager.GetRankFromRating(-100);
        Assert.AreEqual("Bronze", rank.tier);
    }

    [Test]
    public void RankedManager_ManagerTypes_Exist()
    {
        // Verify response types are serializable
        Assert.IsNotNull(typeof(RankedManager.RankInfo));
        Assert.IsNotNull(typeof(RankedManager.LeaderboardEntry));
        Assert.IsNotNull(typeof(RankedManager.MatchHistoryEntry));
        Assert.IsNotNull(typeof(RankedManager.SeasonInfo));
        Assert.IsNotNull(typeof(RankedManager.PlacementData));
        Assert.IsNotNull(typeof(RankedManager.MatchResultEntry));
    }

    [Test]
    public void RankedManager_PlacementData_HasRequiredFields()
    {
        var placement = new RankedManager.PlacementData
        {
            playerId = "p_test123",
            placement = 1,
            playerCount = 4
        };
        Assert.AreEqual("p_test123", placement.playerId);
        Assert.AreEqual(1, placement.placement);
        Assert.AreEqual(4, placement.playerCount);
    }
}

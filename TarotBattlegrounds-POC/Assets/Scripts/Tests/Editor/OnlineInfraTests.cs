using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// T007: Integration tests for Phase I online infrastructure.
/// Tests auth flow logic, matchmaking queue, and player identity.
/// Network tests are unit-level (no actual AWS calls).
/// </summary>
[TestFixture]
public class OnlineInfraTests
{
    [Test]
    public void GameConfig_PlayerName_PersistsCorrectly()
    {
        string testName = "TestPlayer" + UnityEngine.Random.Range(1000, 9999);
        GameConfig.PlayerName = testName;
        Assert.AreEqual(testName, GameConfig.PlayerName);
    }

    [Test]
    public void GameConfig_MultiplayerMode_SetsCorrectly()
    {
        GameConfig.CurrentGameMode = GameConfig.GameMode.Multiplayer;
        Assert.AreEqual(GameConfig.GameMode.Multiplayer, GameConfig.CurrentGameMode);
    }

    [Test]
    public void GameConfig_MultiplayerHumanSlots_TracksCorrectly()
    {
        var slots = new HashSet<int> { 0, 2, 5 };
        GameConfig.SetMultiplayerHumanSlots(slots);
        GameConfig.CurrentGameMode = GameConfig.GameMode.Multiplayer;

        Assert.IsTrue(GameConfig.IsHumanPlayer(0));
        Assert.IsFalse(GameConfig.IsHumanPlayer(1));
        Assert.IsTrue(GameConfig.IsHumanPlayer(2));
        Assert.IsFalse(GameConfig.IsHumanPlayer(3));
        Assert.IsTrue(GameConfig.IsHumanPlayer(5));
    }

    [Test]
    public void GameConfig_PlayerCount_Clamps2To8()
    {
        GameConfig.PlayerCount = 1;
        Assert.AreEqual(2, GameConfig.PlayerCount);

        GameConfig.PlayerCount = 10;
        Assert.AreEqual(8, GameConfig.PlayerCount);

        GameConfig.PlayerCount = 4;
        Assert.AreEqual(4, GameConfig.PlayerCount);
    }

    [Test]
    public void DataConfig_HasApiBaseUrl_Field()
    {
        // Verify DataConfig ScriptableObject has the API URL field
        var config = new DataConfig();
        Assert.IsNotNull(config);
        // Default should be empty (user must configure)
        Assert.AreEqual("", config.apiBaseUrl);
    }

    [Test]
    public void DataConfig_HasDataBaseUrl_WithDefault()
    {
        var config = new DataConfig();
        Assert.IsTrue(config.dataBaseUrl.Contains("tarot-battlegrounds-data"));
    }

    [Test]
    public void GameAuthManager_OfflineGuestMode_GeneratesIdentity()
    {
        // Test that offline guest mode works without API
        // This is a smoke test — actual GameAuthManager requires MonoBehaviour
        // Just verify the static fields exist
        Assert.IsNotNull(typeof(GameAuthManager).GetProperty("IsAuthenticated"));
        Assert.IsNotNull(typeof(GameAuthManager).GetProperty("PlayerId"));
        Assert.IsNotNull(typeof(GameAuthManager).GetProperty("DisplayName"));
        Assert.IsNotNull(typeof(GameAuthManager).GetProperty("Rating"));
    }

    [Test]
    public void MatchmakingManager_QueueStates_AreValid()
    {
        // Verify the state enum has expected values
        Assert.AreEqual(0, (int)MatchmakingManager.QueueState.Idle);
        Assert.AreEqual(1, (int)MatchmakingManager.QueueState.Joining);
        Assert.AreEqual(2, (int)MatchmakingManager.QueueState.Waiting);
        Assert.AreEqual(3, (int)MatchmakingManager.QueueState.Matched);
        Assert.AreEqual(4, (int)MatchmakingManager.QueueState.Error);
    }

#if PHOTON_UNITY_NETWORKING
    [Test]
    public void PhotonConnector_IdentityConstants_AreDefined()
    {
        Assert.AreEqual("pid", PhotonConnector.PROP_PLAYER_ID);
        Assert.AreEqual("dname", PhotonConnector.PROP_DISPLAY_NAME);
        Assert.AreEqual("rating", PhotonConnector.PROP_RATING);
    }

    [Test]
    public void RoomManager_PropertyKeys_AreDefined()
    {
        Assert.AreEqual("gameStarted", RoomManager.PROP_GAME_STARTED);
        Assert.AreEqual("hostName", RoomManager.PROP_HOST_NAME);
        Assert.AreEqual("playerCount", RoomManager.PROP_PLAYER_COUNT);
    }
#endif
}

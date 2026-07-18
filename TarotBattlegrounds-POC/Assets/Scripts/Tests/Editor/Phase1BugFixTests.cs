using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Phase 1 regression tests: T765 (Coins OnBuy), T766 (Intuitive Novice Aegis),
/// T767 (RandomTransform pool reserve).
/// </summary>
[TestFixture]
public class Phase1BugFixTests
{
    private GameObject _tavernGO;
    private TavernManager _tavern;
    private GameObject _playerGO;
    private Player _player;
    private GameObject _synergyGO;
    private SynergyManager _synergy;
    private GameObject _themeGO;
    private List<Object> _toDestroy = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        AbilityManager.ClearAll();

        if (TavernManager.Instance != null)
            Object.DestroyImmediate(TavernManager.Instance.gameObject);

        _themeGO = new GameObject("ThemeManager");
        _themeGO.AddComponent<ThemeManager>();

        // Clear any leftover singleton from a prior fixture (Awake is unreliable in EditMode)
        if (SynergyManager.Instance != null)
            Object.DestroyImmediate(SynergyManager.Instance.gameObject);

        _synergyGO = new GameObject("SynergyManager");
        _synergy = _synergyGO.AddComponent<SynergyManager>();
        _synergy.tribeSynergies = new TribeSynergy[0];
        SetSynergyInstance(_synergy);
        _synergy.InitializeSynergyCache(); // fills Coins via DefaultSynergyFactory

        _tavernGO = new GameObject("TavernManager");
        _tavern = _tavernGO.AddComponent<TavernManager>();
        _tavern.masterCards = new List<Card>();
        for (int i = 0; i < 8; i++)
        {
            var c = ScriptableObject.CreateInstance<Card>();
            c.cardName = $"PoolCard_{i}";
            c.tier = 1;
            c.attack = 1;
            c.health = 1;
            c.tribes = new[] { TribeType.Wands };
            _tavern.masterCards.Add(c);
            _toDestroy.Add(c);
        }
        _tavern.ResetPool();
        SetTavernInstance(_tavern);

        _playerGO = new GameObject("TestPlayer");
        _player = _playerGO.AddComponent<Player>();
        _player.playerId = 1;
        _player.coins = 10;
        _player.currentTavernTier = 1;
    }

    [TearDown]
    public void TearDown()
    {
        AbilityManager.ClearAll();
        if (_playerGO != null) Object.DestroyImmediate(_playerGO);
        if (_tavernGO != null) Object.DestroyImmediate(_tavernGO);
        if (_synergyGO != null) Object.DestroyImmediate(_synergyGO);
        if (_themeGO != null) Object.DestroyImmediate(_themeGO);
        SetTavernInstance(null);
        SetSynergyInstance(null);
        foreach (var o in _toDestroy)
            if (o != null) Object.DestroyImmediate(o);
        _toDestroy.Clear();
    }

    private static void SetTavernInstance(TavernManager instance)
    {
        typeof(TavernManager).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, instance);
    }

    private static void SetSynergyInstance(SynergyManager instance)
    {
        typeof(SynergyManager).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, instance);
    }

    private Card MakeCoin(string name)
    {
        var c = ScriptableObject.CreateInstance<Card>();
        c.cardName = name;
        c.tier = 1;
        c.attack = 1;
        c.health = 2;
        c.tribes = new[] { TribeType.Coins };
        _toDestroy.Add(c);
        return c;
    }

    // =====================================================
    // T765 — Coins OnBuy
    // =====================================================

    [Test]
    public void T765_CoinsTier2_OnBuy_GrantsBonusGold()
    {
        // Arrange: 2 Coins on board activates T2 OnBuy (+1 gold per buy)
        _player.board.Add(MakeCoin("CoinA"));
        _player.board.Add(MakeCoin("CoinB"));

        var shopCard = ScriptableObject.CreateInstance<Card>();
        shopCard.cardName = "ShopOffer";
        shopCard.tier = 1;
        shopCard.attack = 1;
        shopCard.health = 1;
        shopCard.tribes = new[] { TribeType.Wands };
        shopCard.buyCostModifier = 0; // base cost 3
        _toDestroy.Add(shopCard);

        if (!_tavern.availableCards.ContainsKey(_player.playerId))
            _tavern.availableCards[_player.playerId] = new List<Card>();
        _tavern.availableCards[_player.playerId].Clear();
        _tavern.availableCards[_player.playerId].Add(shopCard);

        _player.coins = 5;
        int coinsBefore = _player.coins;

        // Act
        _player.BuyCard(0);

        // Assert: paid 3 for the card, then OnBuy grants +1 → net -2
        Assert.AreEqual(1, _player.hand.Count, "Bought card should be in hand");
        Assert.AreEqual(coinsBefore - 3 + 1, _player.coins,
            "Coins T2 OnBuy must grant +1 gold after the buy cost is paid");
    }

    [Test]
    public void T765_CoinsOnBuy_DoesNotFire_WithoutThreshold()
    {
        // Only 1 Coin on board — T2 not active
        _player.board.Add(MakeCoin("LoneCoin"));

        var shopCard = ScriptableObject.CreateInstance<Card>();
        shopCard.cardName = "ShopOffer2";
        shopCard.tier = 1;
        shopCard.attack = 1;
        shopCard.health = 1;
        shopCard.tribes = new[] { TribeType.Wands };
        _toDestroy.Add(shopCard);

        if (!_tavern.availableCards.ContainsKey(_player.playerId))
            _tavern.availableCards[_player.playerId] = new List<Card>();
        _tavern.availableCards[_player.playerId].Clear();
        _tavern.availableCards[_player.playerId].Add(shopCard);

        _player.coins = 5;
        _player.BuyCard(0);

        Assert.AreEqual(2, _player.coins, "Without 2 Coins on board, buy costs 3 with no OnBuy refund");
    }

    // =====================================================
    // T766 — Intuitive Novice Aegis
    // =====================================================

    [Test]
    public void T766_IntuitiveNovice_Clone_HasAegis()
    {
        var all = CardDatabase.GenerateAllCards();
        var novice = all.FirstOrDefault(c => c.cardName == "Intuitive Novice");
        Assert.IsNotNull(novice, "Intuitive Novice must exist in CardDatabase");
        Assert.AreEqual(Card.EffectType.Aegis, novice.effectType);

        var clone = novice.Clone();
        _toDestroy.Add(clone);

        Assert.IsTrue(clone.hasAegis,
            "Clone of Intuitive Novice (EffectType.Aegis) must set hasAegis=true");
    }

    [Test]
    public void T766_PlayIntuitiveNovice_GrantsAegisOnBoard()
    {
        var all = CardDatabase.GenerateAllCards();
        var template = all.First(c => c.cardName == "Intuitive Novice");
        var card = template.Clone();
        _toDestroy.Add(card);

        _player.hand.Add(card);
        _player.PlayCard(0, 0);

        Assert.AreEqual(1, _player.board.Count);
        Assert.IsTrue(_player.board[0].hasAegis,
            "Playing Intuitive Novice must leave a board minion with Aegis");
    }

    // =====================================================
    // T767 — RandomTransform pool reserve
    // =====================================================

    [Test]
    public void T767_RandomTransform_RemovesCardFromPool()
    {
        int poolBefore = _tavern.GetFullPool().Count;
        Assert.Greater(poolBefore, 0, "Pool must be non-empty");

        var source = ScriptableObject.CreateInstance<Card>();
        source.cardName = "Transformer";
        source.tier = 1;
        source.attack = 1;
        source.health = 0; // dead, deathrattle context
        source.tribes = new TribeType[0];
        _toDestroy.Add(source);

        var board = new List<Card> { source };
        var ability = new RandomTransformAbility();
        AbilityManager.RegisterAbility(source, ability);

        var ctx = new AbilityContext
        {
            SourceCard = source,
            Owner = _player,
            OwnerBoard = board
        };
        AbilityManager.TriggerAbilities(AbilityTrigger.Deathrattle, ctx);

        int poolAfter = _tavern.GetFullPool().Count;
        Assert.AreEqual(poolBefore - 1, poolAfter,
            "RandomTransform must remove the reserved pool card before cloning");
        Assert.Greater(board.Count, 1, "A transformed card should be inserted on the board");
    }
}

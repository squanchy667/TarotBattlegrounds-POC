using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Comprehensive NUnit tests for all 12 hero power implementations.
///
/// Test categories per power:
///   - PowerName / Description / CoinCost / IsPassive metadata
///   - CanActivate: returns correct true/false under various states
///   - Activate: applies the correct stat/state effect
///   - ResetForNewTurn: clears UsedThisTurn flag
///   - OnCombatStart: passive and armed triggers fire correctly
///
/// Architecture note: HeroPowerManager.ActivateHeroPower() deducts coins BEFORE
/// calling power.Activate(). Individual Activate() methods therefore never
/// deduct coins themselves. Tests invoke Activate() directly to isolate the
/// effect, and verify coin deduction via CanActivate / HeroPowerManager paths
/// separately.
/// </summary>
[TestFixture]
public class HeroPowerTests
{
    // ---------------------------------------------------------------
    // Shared fixtures
    // ---------------------------------------------------------------
    private GameObject _playerGO;
    private GameObject _opponentGO;
    private Player _player;
    private Player _opponent;

    [SetUp]
    public void SetUp()
    {
        _playerGO = new GameObject("Player");
        _player = _playerGO.AddComponent<Player>();
        _player.playerId = 1;
        _player.coins = 10;
        _player.Health = 40;
        _player.board = new List<Card>();
        _player.hand = new List<Card>();

        _opponentGO = new GameObject("Opponent");
        _opponent = _opponentGO.AddComponent<Player>();
        _opponent.playerId = 2;
        _opponent.coins = 10;
        _opponent.Health = 40;
        _opponent.board = new List<Card>();
        _opponent.hand = new List<Card>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_playerGO);
        Object.DestroyImmediate(_opponentGO);
    }

    // ---------------------------------------------------------------
    // Helper: create a minimal Card ScriptableObject
    // ---------------------------------------------------------------
    private Card MakeCard(string name = "TestCard",
                          int attack = 2, int health = 3,
                          TribeType tribe = TribeType.None,
                          bool hasAegis = false)
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.attack = attack;
        card.health = health;
        card.tribes = tribe == TribeType.None
            ? new TribeType[0]
            : new[] { tribe };
        card.hasAegis = hasAegis;
        return card;
    }

    // ---------------------------------------------------------------
    // Helper: simulate what HeroPowerManager does when activating
    // (deduct cost, set UsedThisTurn, then call Activate)
    // ---------------------------------------------------------------
    private bool SimulateManagerActivate(HeroPowerBase power, Player player)
    {
        if (!power.CanActivate(player)) return false;
        player.coins -= power.CoinCost;
        power.UsedThisTurn = true;
        power.Activate(player);
        return true;
    }

    // ================================================================
    // 1. MidasTouchPower
    // ================================================================

    [Test]
    public void MidasTouch_PowerName_IsMidasTouch()
    {
        var power = new MidasTouchPower();
        Assert.AreEqual("The Empress", power.PowerName);
    }

    [Test]
    public void MidasTouch_CoinCost_IsZero()
    {
        var power = new MidasTouchPower();
        Assert.AreEqual(0, power.CoinCost);
    }

    [Test]
    public void MidasTouch_IsNotPassive()
    {
        var power = new MidasTouchPower();
        Assert.IsFalse(power.IsPassive);
    }

    [Test]
    public void MidasTouch_CanActivate_TrueWithZeroCoins()
    {
        // Cost is 0, so even 0 coins should allow activation
        var power = new MidasTouchPower();
        _player.coins = 0;
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void MidasTouch_CanActivate_FalseWhenAlreadyUsed()
    {
        var power = new MidasTouchPower();
        power.UsedThisTurn = true;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void MidasTouch_CanActivate_FalseWithNullOwner()
    {
        var power = new MidasTouchPower();
        Assert.IsFalse(power.CanActivate(null));
    }

    [Test]
    public void MidasTouch_Activate_GivesOneCoin()
    {
        var power = new MidasTouchPower();
        _player.coins = 3;
        power.Activate(_player);
        Assert.AreEqual(4, _player.coins);
    }

    [Test]
    public void MidasTouch_Activate_CappedAtMaxCoins()
    {
        var power = new MidasTouchPower();
        _player.coins = Player.MAX_COINS; // Should not exceed cap
        power.Activate(_player);
        Assert.AreEqual(Player.MAX_COINS, _player.coins);
    }

    [Test]
    public void MidasTouch_ResetForNewTurn_ClearsUsedThisTurn()
    {
        var power = new MidasTouchPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    [Test]
    public void MidasTouch_FullManagerFlow_DeductsCostAndAddsGold()
    {
        // Cost 0 means coins should not decrease, and +1 coin from effect
        var power = new MidasTouchPower();
        _player.coins = 5;
        bool activated = SimulateManagerActivate(power, _player);
        Assert.IsTrue(activated);
        // cost=0 deducted, +1 from Activate => net +1
        Assert.AreEqual(6, _player.coins);
        Assert.IsTrue(power.UsedThisTurn);
    }

    // ================================================================
    // 2. EmpowerPower
    // ================================================================

    [Test]
    public void Empower_PowerName_IsEmpower()
    {
        var power = new EmpowerPower();
        Assert.AreEqual("The Magician", power.PowerName);
    }

    [Test]
    public void Empower_CoinCost_IsTwo()
    {
        var power = new EmpowerPower();
        Assert.AreEqual(2, power.CoinCost);
    }

    [Test]
    public void Empower_CanActivate_FalseWithEmptyBoard()
    {
        var power = new EmpowerPower();
        _player.board.Clear();
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Empower_CanActivate_FalseWithInsufficientCoins()
    {
        var power = new EmpowerPower();
        _player.board.Add(MakeCard());
        _player.coins = 1; // Cost is 2
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Empower_CanActivate_TrueWithBoardAndCoins()
    {
        var power = new EmpowerPower();
        _player.board.Add(MakeCard());
        _player.coins = 2;
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void Empower_Activate_GivesPlusOneHealthToAllMinions()
    {
        var power = new EmpowerPower();
        var card1 = MakeCard("A", health: 3);
        var card2 = MakeCard("B", health: 5);
        _player.board.Add(card1);
        _player.board.Add(card2);

        power.Activate(_player);

        Assert.AreEqual(4, card1.health, "Card A should have +1 Health");
        Assert.AreEqual(6, card2.health, "Card B should have +1 Health");
    }

    [Test]
    public void Empower_Activate_SkipsDeadMinions()
    {
        var power = new EmpowerPower();
        var living = MakeCard("Living", health: 3);
        var dead = MakeCard("Dead", health: 0); // dead minion (health <= 0)
        _player.board.Add(living);
        _player.board.Add(dead);

        power.Activate(_player);

        Assert.AreEqual(4, living.health, "Living minion should gain +1 Health");
        Assert.AreEqual(0, dead.health, "Dead minion should NOT be buffed");
    }

    [Test]
    public void Empower_Activate_EmptyBoard_DoesNotThrow()
    {
        var power = new EmpowerPower();
        // Empty board — Activate is still callable (CanActivate would block, but raw call should not throw)
        Assert.DoesNotThrow(() => power.Activate(_player));
    }

    [Test]
    public void Empower_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new EmpowerPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 3. HealerPower
    // ================================================================

    [Test]
    public void Healer_PowerName_IsHealer()
    {
        var power = new HealerPower();
        Assert.AreEqual("Temperance", power.PowerName);
    }

    [Test]
    public void Healer_CoinCost_IsOne()
    {
        var power = new HealerPower();
        Assert.AreEqual(1, power.CoinCost);
    }

    [Test]
    public void Healer_CanActivate_FalseWithEmptyBoard()
    {
        var power = new HealerPower();
        _player.board.Clear();
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Healer_CanActivate_FalseWithInsufficientCoins()
    {
        var power = new HealerPower();
        _player.board.Add(MakeCard());
        _player.coins = 0;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Healer_CanActivate_TrueWithBoardAndOneCoin()
    {
        var power = new HealerPower();
        _player.board.Add(MakeCard());
        _player.coins = 1;
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void Healer_Activate_SingleMinion_GainsPlusThreeHealth()
    {
        var power = new HealerPower();
        var card = MakeCard(health: 2);
        _player.board.Add(card);

        power.Activate(_player);

        Assert.AreEqual(5, card.health, "Single minion should gain +3 Health");
    }

    [Test]
    public void Healer_Activate_EmptyBoard_DoesNotThrow()
    {
        var power = new HealerPower();
        // CanActivate guards this, but raw Activate should be safe
        Assert.DoesNotThrow(() => power.Activate(_player));
    }

    [Test]
    public void Healer_Activate_MultipleMinions_TotalHealthIncreasedByThree()
    {
        // With multiple minions, exactly one gets +3 (random pick)
        var power = new HealerPower();
        var card1 = MakeCard("A", health: 2);
        var card2 = MakeCard("B", health: 2);
        _player.board.Add(card1);
        _player.board.Add(card2);

        int totalBefore = card1.health + card2.health; // 4
        power.Activate(_player);
        int totalAfter = card1.health + card2.health;

        Assert.AreEqual(totalBefore + 3, totalAfter,
            "Exactly one minion should receive +3 Health");
    }

    [Test]
    public void Healer_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new HealerPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 4. ArcaneBoltPower
    // ================================================================

    [Test]
    public void ArcaneBolt_PowerName_IsArcaneBolt()
    {
        var power = new ArcaneBoltPower();
        Assert.AreEqual("The Tower", power.PowerName);
    }

    [Test]
    public void ArcaneBolt_CoinCost_IsTwo()
    {
        var power = new ArcaneBoltPower();
        Assert.AreEqual(2, power.CoinCost);
    }

    [Test]
    public void ArcaneBolt_IsNotPassive()
    {
        var power = new ArcaneBoltPower();
        Assert.IsFalse(power.IsPassive);
    }

    [Test]
    public void ArcaneBolt_CanActivate_TrueWithEnoughCoins()
    {
        var power = new ArcaneBoltPower();
        _player.coins = 2;
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void ArcaneBolt_CanActivate_FalseWithInsufficientCoins()
    {
        var power = new ArcaneBoltPower();
        _player.coins = 1;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void ArcaneBolt_Activate_ArmsThePower()
    {
        // After Activate, OnCombatStart should deal damage (armed state)
        var power = new ArcaneBoltPower();
        var enemyCard = MakeCard("Enemy", health: 10);
        _opponent.board.Add(enemyCard);

        power.Activate(_player);
        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(7, enemyCard.health,
            "Enemy minion should take 3 damage after bolt armed + combat start");
    }

    [Test]
    public void ArcaneBolt_OnCombatStart_WithoutActivate_DealsNoDamage()
    {
        // OnCombatStart fires without ever arming — should be a no-op
        var power = new ArcaneBoltPower();
        var enemyCard = MakeCard("Enemy", health: 10);
        _opponent.board.Add(enemyCard);

        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(10, enemyCard.health,
            "Unarmed bolt should deal no damage at combat start");
    }

    [Test]
    public void ArcaneBolt_OnCombatStart_UnarmsBoltAfterFiring()
    {
        // Second combat start should NOT deal damage again
        var power = new ArcaneBoltPower();
        var enemyCard = MakeCard("Enemy", health: 20);
        _opponent.board.Add(enemyCard);

        power.Activate(_player);
        power.OnCombatStart(_player, _opponent); // fires and disarms
        power.OnCombatStart(_player, _opponent); // should not fire again

        Assert.AreEqual(17, enemyCard.health,
            "Bolt should fire only once (deal 3 damage once, not twice)");
    }

    [Test]
    public void ArcaneBolt_OnCombatStart_EmptyOpponentBoard_DoesNotThrow()
    {
        var power = new ArcaneBoltPower();
        _opponent.board.Clear();
        power.Activate(_player);
        Assert.DoesNotThrow(() => power.OnCombatStart(_player, _opponent));
    }

    [Test]
    public void ArcaneBolt_OnCombatStart_KillsWeakMinion_RemovesFromBoard()
    {
        var power = new ArcaneBoltPower();
        var fragile = MakeCard("Fragile", health: 2); // 2 health, bolt deals 3
        _opponent.board.Add(fragile);

        power.Activate(_player);
        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(0, _opponent.board.Count,
            "Minion killed by Arcane Bolt should be removed from opponent board");
    }

    [Test]
    public void ArcaneBolt_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new ArcaneBoltPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 5. FortifyPower
    // ================================================================

    [Test]
    public void Fortify_PowerName_IsFortify()
    {
        var power = new FortifyPower();
        Assert.AreEqual("Strength", power.PowerName);
    }

    [Test]
    public void Fortify_CoinCost_IsTwo()
    {
        var power = new FortifyPower();
        Assert.AreEqual(2, power.CoinCost);
    }

    [Test]
    public void Fortify_CanActivate_FalseWithEmptyBoard()
    {
        var power = new FortifyPower();
        _player.board.Clear();
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Fortify_CanActivate_FalseWhenAllMinionsHaveAegis()
    {
        var power = new FortifyPower();
        _player.coins = 5;
        _player.board.Add(MakeCard(hasAegis: true));
        _player.board.Add(MakeCard(hasAegis: true));
        Assert.IsFalse(power.CanActivate(_player),
            "Should not activate when every minion already has Aegis");
    }

    [Test]
    public void Fortify_CanActivate_TrueWhenAtLeastOneMinionLacksAegis()
    {
        var power = new FortifyPower();
        _player.coins = 5;
        _player.board.Add(MakeCard(hasAegis: true));
        _player.board.Add(MakeCard(hasAegis: false)); // one without Aegis
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void Fortify_CanActivate_FalseWithInsufficientCoins()
    {
        var power = new FortifyPower();
        _player.coins = 1;
        _player.board.Add(MakeCard(hasAegis: false));
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Fortify_Activate_SingleMinion_GrantsAegis()
    {
        var power = new FortifyPower();
        var card = MakeCard(hasAegis: false);
        _player.board.Add(card);

        power.Activate(_player);

        Assert.IsTrue(card.hasAegis, "Minion should have Aegis after Fortify");
    }

    [Test]
    public void Fortify_Activate_NeverGrantsAegisToAlreadyShielded()
    {
        // Board has one shielded and one unshielded — activate should target unshielded
        var power = new FortifyPower();
        var shielded = MakeCard("Shielded", hasAegis: true);
        var unshielded = MakeCard("Unshielded", hasAegis: false);
        _player.board.Add(shielded);
        _player.board.Add(unshielded);

        power.Activate(_player);

        // unshielded must now have Aegis; shielded was already set
        Assert.IsTrue(unshielded.hasAegis, "Unshielded minion should receive Aegis");
    }

    [Test]
    public void Fortify_Activate_MultipleValidTargets_ExactlyOneGainsAegis()
    {
        var power = new FortifyPower();
        var cardA = MakeCard("A", hasAegis: false);
        var cardB = MakeCard("B", hasAegis: false);
        var cardC = MakeCard("C", hasAegis: false);
        _player.board.Add(cardA);
        _player.board.Add(cardB);
        _player.board.Add(cardC);

        power.Activate(_player);

        int aegisCount = _player.board.Count(c => c.hasAegis);
        Assert.AreEqual(1, aegisCount,
            "Exactly one minion should gain Aegis per activation");
    }

    [Test]
    public void Fortify_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new FortifyPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 6. RerollerPower
    // ================================================================

    [Test]
    public void Reroller_PowerName_IsReroller()
    {
        var power = new RerollerPower();
        Assert.AreEqual("The Fool", power.PowerName);
    }

    [Test]
    public void Reroller_CoinCost_IsOne()
    {
        var power = new RerollerPower();
        Assert.AreEqual(1, power.CoinCost);
    }

    [Test]
    public void Reroller_IsNotPassive()
    {
        var power = new RerollerPower();
        Assert.IsFalse(power.IsPassive);
    }

    [Test]
    public void Reroller_CanActivate_TrueWithOneCoin()
    {
        var power = new RerollerPower();
        _player.coins = 1;
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void Reroller_CanActivate_FalseWithZeroCoins()
    {
        var power = new RerollerPower();
        _player.coins = 0;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Reroller_CanActivate_FalseWhenAlreadyUsed()
    {
        var power = new RerollerPower();
        power.UsedThisTurn = true;
        _player.coins = 5;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Reroller_Activate_WithoutTavernManager_DoesNotThrow()
    {
        // TavernManager.Instance is null in pure unit tests — Activate must handle this gracefully
        var power = new RerollerPower();
        Assert.DoesNotThrow(() => power.Activate(_player),
            "Reroller should not throw when TavernManager is absent");
    }

    [Test]
    public void Reroller_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new RerollerPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 7. LifeTapPower
    // ================================================================

    [Test]
    public void LifeTap_PowerName_IsLifeTap()
    {
        var power = new LifeTapPower();
        Assert.AreEqual("The Hanged Man", power.PowerName);
    }

    [Test]
    public void LifeTap_CoinCost_IsZero()
    {
        var power = new LifeTapPower();
        Assert.AreEqual(0, power.CoinCost);
    }

    [Test]
    public void LifeTap_IsNotPassive()
    {
        var power = new LifeTapPower();
        Assert.IsFalse(power.IsPassive);
    }

    [Test]
    public void LifeTap_CanActivate_TrueWhenHealthAboveTwo()
    {
        var power = new LifeTapPower();
        _player.Health = 3; // > 2
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void LifeTap_CanActivate_FalseWhenHealthIsExactlyTwo()
    {
        var power = new LifeTapPower();
        _player.Health = 2; // NOT > 2
        Assert.IsFalse(power.CanActivate(_player),
            "Should refuse to self-damage when at exactly 2 HP");
    }

    [Test]
    public void LifeTap_CanActivate_FalseWhenHealthIsOne()
    {
        var power = new LifeTapPower();
        _player.Health = 1;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void LifeTap_CanActivate_FalseWhenAlreadyUsed()
    {
        var power = new LifeTapPower();
        _player.Health = 20;
        power.UsedThisTurn = true;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void LifeTap_Activate_TakesTwoDamageAndGainsOneCoin()
    {
        var power = new LifeTapPower();
        _player.Health = 20;
        _player.coins = 3;

        power.Activate(_player);

        Assert.AreEqual(18, _player.Health, "Player should take 2 damage");
        Assert.AreEqual(4, _player.coins, "Player should gain 1 coin");
    }

    [Test]
    public void LifeTap_FullManagerFlow_NetGainIsOneCoin()
    {
        // Cost 0, so coins unchanged by manager, +1 from Activate
        var power = new LifeTapPower();
        _player.Health = 20;
        _player.coins = 5;

        bool activated = SimulateManagerActivate(power, _player);

        Assert.IsTrue(activated);
        Assert.AreEqual(6, _player.coins, "Net coin gain should be +1 (cost=0, effect=+1)");
        Assert.AreEqual(18, _player.Health, "Player took 2 self-damage");
    }

    [Test]
    public void LifeTap_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new LifeTapPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 8. BladeMasterPower
    // ================================================================

    [Test]
    public void BladeMaster_PowerName_IsBladeMaster()
    {
        var power = new BladeMasterPower();
        Assert.AreEqual("The Chariot", power.PowerName);
    }

    [Test]
    public void BladeMaster_CoinCost_IsTwo()
    {
        var power = new BladeMasterPower();
        Assert.AreEqual(2, power.CoinCost);
    }

    [Test]
    public void BladeMaster_CanActivate_FalseWithEmptyBoard()
    {
        var power = new BladeMasterPower();
        _player.board.Clear();
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void BladeMaster_CanActivate_FalseWithInsufficientCoins()
    {
        var power = new BladeMasterPower();
        _player.board.Add(MakeCard());
        _player.coins = 1;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void BladeMaster_CanActivate_TrueWithBoardAndCoins()
    {
        var power = new BladeMasterPower();
        _player.board.Add(MakeCard());
        _player.coins = 2;
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void BladeMaster_Activate_SingleMinion_GainsPlusOneAttack()
    {
        var power = new BladeMasterPower();
        var card = MakeCard(attack: 3);
        _player.board.Add(card);

        power.Activate(_player);

        Assert.AreEqual(4, card.attack, "Single minion should gain +1 Attack");
    }

    [Test]
    public void BladeMaster_Activate_MultipleMinions_ExactlyOnePlusBuffed()
    {
        var power = new BladeMasterPower();
        var cardA = MakeCard("A", attack: 2);
        var cardB = MakeCard("B", attack: 2);
        _player.board.Add(cardA);
        _player.board.Add(cardB);

        int totalBefore = cardA.attack + cardB.attack; // 4
        power.Activate(_player);
        int totalAfter = cardA.attack + cardB.attack;

        Assert.AreEqual(totalBefore + 1, totalAfter,
            "Exactly one minion should receive +1 Attack");
    }

    [Test]
    public void BladeMaster_Activate_SkipsDeadMinions()
    {
        var power = new BladeMasterPower();
        var dead = MakeCard("Dead", attack: 2, health: 0);
        _player.board.Add(dead);
        // CanActivate checks board.Any(c => c.health > 0), so this bypasses via raw call
        power.Activate(_player);
        Assert.AreEqual(2, dead.attack, "Dead minion should not receive the attack buff");
    }

    [Test]
    public void BladeMaster_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new BladeMasterPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 9. WarChiefPower  (passive)
    // ================================================================

    [Test]
    public void WarChief_PowerName_IsWarChief()
    {
        var power = new WarChiefPower();
        Assert.AreEqual("The Emperor", power.PowerName);
    }

    [Test]
    public void WarChief_IsPassive_True()
    {
        var power = new WarChiefPower();
        Assert.IsTrue(power.IsPassive);
    }

    [Test]
    public void WarChief_CanActivate_AlwaysFalse()
    {
        var power = new WarChiefPower();
        _player.coins = 10;
        _player.board.Add(MakeCard(tribe: TribeType.Swords));
        Assert.IsFalse(power.CanActivate(_player),
            "Passive power must always return false from CanActivate");
    }

    [Test]
    public void WarChief_OnCombatStart_BuffsSwordsMinions()
    {
        var power = new WarChiefPower();
        var swordCard = MakeCard("Sword", attack: 2, tribe: TribeType.Swords);
        _player.board.Add(swordCard);

        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(3, swordCard.attack, "Swords minion should gain +1 Attack from War Chief");
    }

    [Test]
    public void WarChief_OnCombatStart_DoesNotBuffNonSwords()
    {
        var power = new WarChiefPower();
        var cupsCard = MakeCard("Cups", attack: 2, tribe: TribeType.Cups);
        var wandsCard = MakeCard("Wands", attack: 3, tribe: TribeType.Wands);
        _player.board.Add(cupsCard);
        _player.board.Add(wandsCard);

        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(2, cupsCard.attack, "Cups minion should NOT be buffed by War Chief");
        Assert.AreEqual(3, wandsCard.attack, "Wands minion should NOT be buffed by War Chief");
    }

    [Test]
    public void WarChief_OnCombatStart_BuffsMultipleSwordsMinions()
    {
        var power = new WarChiefPower();
        var sword1 = MakeCard("S1", attack: 2, tribe: TribeType.Swords);
        var sword2 = MakeCard("S2", attack: 4, tribe: TribeType.Swords);
        _player.board.Add(sword1);
        _player.board.Add(sword2);

        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(3, sword1.attack, "First Swords minion gains +1 Attack");
        Assert.AreEqual(5, sword2.attack, "Second Swords minion gains +1 Attack");
    }

    [Test]
    public void WarChief_OnCombatStart_EmptyBoard_DoesNotThrow()
    {
        var power = new WarChiefPower();
        Assert.DoesNotThrow(() => power.OnCombatStart(_player, _opponent));
    }

    [Test]
    public void WarChief_OnCombatStart_SkipsDeadSwordsMinions()
    {
        var power = new WarChiefPower();
        var deadSword = MakeCard("DeadSword", attack: 2, health: 0, tribe: TribeType.Swords);
        _player.board.Add(deadSword);

        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(2, deadSword.attack, "Dead Swords minion should NOT be buffed");
    }

    // ================================================================
    // 10. TacticianPower  (passive)
    // ================================================================

    [Test]
    public void Tactician_PowerName_IsTactician()
    {
        var power = new TacticianPower();
        Assert.AreEqual("The High Priestess", power.PowerName);
    }

    [Test]
    public void Tactician_IsPassive_True()
    {
        var power = new TacticianPower();
        Assert.IsTrue(power.IsPassive);
    }

    [Test]
    public void Tactician_CanActivate_AlwaysFalse()
    {
        var power = new TacticianPower();
        _player.coins = 10;
        _player.board.Add(MakeCard());
        Assert.IsFalse(power.CanActivate(_player),
            "Passive power must always return false from CanActivate");
    }

    [Test]
    public void Tactician_OnCombatStart_SingleMinion_GainsTwoPlusTwoStats()
    {
        var power = new TacticianPower();
        var card = MakeCard(attack: 2, health: 3);
        _player.board.Add(card);

        power.OnCombatStart(_player, _opponent);

        Assert.AreEqual(4, card.attack, "Minion should gain +2 Attack");
        Assert.AreEqual(5, card.health, "Minion should gain +2 Health");
    }

    [Test]
    public void Tactician_OnCombatStart_MultipleMinions_ExactlyOneGetsBuff()
    {
        var power = new TacticianPower();
        var cardA = MakeCard("A", attack: 1, health: 1);
        var cardB = MakeCard("B", attack: 1, health: 1);
        _player.board.Add(cardA);
        _player.board.Add(cardB);

        int totalAttackBefore = cardA.attack + cardB.attack; // 2
        int totalHealthBefore = cardA.health + cardB.health; // 2
        power.OnCombatStart(_player, _opponent);
        int totalAttackAfter = cardA.attack + cardB.attack;
        int totalHealthAfter = cardA.health + cardB.health;

        Assert.AreEqual(totalAttackBefore + 2, totalAttackAfter,
            "Exactly one minion should gain +2 Attack");
        Assert.AreEqual(totalHealthBefore + 2, totalHealthAfter,
            "Exactly one minion should gain +2 Health");
    }

    [Test]
    public void Tactician_OnCombatStart_EmptyBoard_DoesNotThrow()
    {
        var power = new TacticianPower();
        Assert.DoesNotThrow(() => power.OnCombatStart(_player, _opponent));
    }

    // ================================================================
    // 11. EconomistPower
    // ================================================================

    [Test]
    public void Economist_PowerName_IsEconomist()
    {
        var power = new EconomistPower();
        Assert.AreEqual("Wheel of Fortune", power.PowerName);
    }

    [Test]
    public void Economist_CoinCost_IsOne()
    {
        var power = new EconomistPower();
        Assert.AreEqual(1, power.CoinCost);
    }

    [Test]
    public void Economist_IsNotPassive()
    {
        var power = new EconomistPower();
        Assert.IsFalse(power.IsPassive);
    }

    [Test]
    public void Economist_CanActivate_TrueWithOneCoin()
    {
        var power = new EconomistPower();
        _player.coins = 1;
        Assert.IsTrue(power.CanActivate(_player));
    }

    [Test]
    public void Economist_CanActivate_FalseWithZeroCoins()
    {
        var power = new EconomistPower();
        _player.coins = 0;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Economist_CanActivate_FalseWhenAlreadyUsed()
    {
        var power = new EconomistPower();
        _player.coins = 5;
        power.UsedThisTurn = true;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Economist_Activate_GivesTwoCoins()
    {
        var power = new EconomistPower();
        _player.coins = 3;

        power.Activate(_player);

        Assert.AreEqual(5, _player.coins, "Economist should add 2 coins");
    }

    [Test]
    public void Economist_FullManagerFlow_NetCoinGainIsOne()
    {
        // Manager deducts cost=1 BEFORE activate (+2). Net = +1
        var power = new EconomistPower();
        _player.coins = 5;

        bool activated = SimulateManagerActivate(power, _player);

        Assert.IsTrue(activated);
        Assert.AreEqual(6, _player.coins, "Net gain should be +1 (cost 1, effect +2)");
        Assert.IsTrue(power.UsedThisTurn);
    }

    [Test]
    public void Economist_Activate_CappedAtMaxCoins()
    {
        var power = new EconomistPower();
        _player.coins = Player.MAX_COINS - 1; // e.g., 9
        power.Activate(_player);
        Assert.AreEqual(Player.MAX_COINS, _player.coins,
            "Coins should be clamped at MAX_COINS");
    }

    [Test]
    public void Economist_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new EconomistPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // 12. RecruiterPower
    // ================================================================

    [Test]
    public void Recruiter_PowerName_IsRecruiter()
    {
        var power = new RecruiterPower();
        Assert.AreEqual("The Hierophant", power.PowerName);
    }

    [Test]
    public void Recruiter_CoinCost_IsTwo()
    {
        var power = new RecruiterPower();
        Assert.AreEqual(2, power.CoinCost);
    }

    [Test]
    public void Recruiter_IsNotPassive()
    {
        var power = new RecruiterPower();
        Assert.IsFalse(power.IsPassive);
    }

    [Test]
    public void Recruiter_CanActivate_FalseWhenHandIsFull()
    {
        var power = new RecruiterPower();
        _player.coins = 5;
        for (int i = 0; i < 10; i++)
            _player.hand.Add(MakeCard());
        Assert.IsFalse(power.CanActivate(_player),
            "Cannot recruit when hand is full (10 cards)");
    }

    [Test]
    public void Recruiter_CanActivate_TrueWhenHandHasNineCards()
    {
        var power = new RecruiterPower();
        _player.coins = 5;
        for (int i = 0; i < 9; i++)
            _player.hand.Add(MakeCard());
        Assert.IsTrue(power.CanActivate(_player),
            "Should be able to recruit when hand has fewer than 10 cards");
    }

    [Test]
    public void Recruiter_CanActivate_FalseWithInsufficientCoins()
    {
        var power = new RecruiterPower();
        _player.coins = 1; // Cost is 2
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Recruiter_CanActivate_FalseWhenAlreadyUsed()
    {
        var power = new RecruiterPower();
        _player.coins = 5;
        power.UsedThisTurn = true;
        Assert.IsFalse(power.CanActivate(_player));
    }

    [Test]
    public void Recruiter_Activate_WithoutTavernManager_DoesNotThrow()
    {
        // TavernManager.Instance is null in unit tests — must degrade gracefully
        var power = new RecruiterPower();
        Assert.DoesNotThrow(() => power.Activate(_player),
            "Recruiter should not throw when TavernManager is absent");
    }

    [Test]
    public void Recruiter_Activate_WithoutTavernManager_HandUnchanged()
    {
        var power = new RecruiterPower();
        int handBefore = _player.hand.Count;
        power.Activate(_player);
        Assert.AreEqual(handBefore, _player.hand.Count,
            "Hand should not change if TavernManager is absent");
    }

    [Test]
    public void Recruiter_ResetForNewTurn_ClearsUsedFlag()
    {
        var power = new RecruiterPower();
        power.UsedThisTurn = true;
        power.ResetForNewTurn();
        Assert.IsFalse(power.UsedThisTurn);
    }

    // ================================================================
    // Cross-cutting / HeroPowerDatabase tests
    // ================================================================

    [Test]
    public void HeroPowerDatabase_GetAllPowers_ReturnsAllTwelvePowers()
    {
        var powers = HeroPowerDatabase.GetAllPowers();
        Assert.AreEqual(12, powers.Count, "Database should expose all 12 hero powers");
    }

    [Test]
    public void HeroPowerDatabase_GetAllPowers_ReturnsFreshInstances()
    {
        var powersA = HeroPowerDatabase.GetAllPowers();
        var powersB = HeroPowerDatabase.GetAllPowers();
        // Mutating one list's power should not affect the other (no shared state)
        powersA[0].UsedThisTurn = true;
        Assert.IsFalse(powersB[0].UsedThisTurn,
            "GetAllPowers must return fresh instances to prevent shared state bugs");
    }

    [Test]
    public void HeroPowerDatabase_GetByName_FindsEachPower()
    {
        string[] expectedNames = {
            "The Empress", "The Magician", "Temperance", "The Tower",
            "Strength", "The Fool", "The Hanged Man", "The Chariot",
            "The Emperor", "The High Priestess", "Wheel of Fortune", "The Hierophant"
        };

        foreach (var name in expectedNames)
        {
            var power = HeroPowerDatabase.GetByName(name);
            Assert.IsNotNull(power, $"GetByName should find power '{name}'");
        }
    }

    [Test]
    public void HeroPowerDatabase_GetByName_UnknownName_ReturnsNull()
    {
        var power = HeroPowerDatabase.GetByName("NonExistentPower");
        Assert.IsNull(power);
    }

    [Test]
    public void PassivePowers_NeverSetUsedThisTurnViaCanActivate()
    {
        // Passive powers return false from CanActivate — no code path should
        // set UsedThisTurn during a spurious activate attempt
        HeroPowerBase[] passives = { new WarChiefPower(), new TacticianPower() };
        foreach (var power in passives)
        {
            bool result = power.CanActivate(_player);
            Assert.IsFalse(result);
            Assert.IsFalse(power.UsedThisTurn,
                $"{power.PowerName} should not set UsedThisTurn when CanActivate is checked");
        }
    }

    [Test]
    public void AllActivePowers_ResetForNewTurn_ClearsFlag()
    {
        var activePowers = HeroPowerDatabase.GetAllPowers()
                                            .Where(p => !p.IsPassive)
                                            .ToList();
        foreach (var power in activePowers)
        {
            power.UsedThisTurn = true;
            power.ResetForNewTurn();
            Assert.IsFalse(power.UsedThisTurn,
                $"{power.PowerName}.ResetForNewTurn() did not clear UsedThisTurn");
        }
    }

    [Test]
    public void AllActivePowers_CanActivate_FalseAfterUsedThisTurnIsTrue()
    {
        var activePowers = HeroPowerDatabase.GetAllPowers()
                                            .Where(p => !p.IsPassive)
                                            .ToList();
        _player.coins = 10;
        _player.board.Add(MakeCard(hasAegis: false));

        foreach (var power in activePowers)
        {
            power.UsedThisTurn = true;
            Assert.IsFalse(power.CanActivate(_player),
                $"{power.PowerName} should not be activatable when UsedThisTurn=true");
        }
    }

    [Test]
    public void AllActivePowers_CanActivate_FalseWithNullOwner()
    {
        var activePowers = HeroPowerDatabase.GetAllPowers()
                                            .Where(p => !p.IsPassive)
                                            .ToList();
        foreach (var power in activePowers)
        {
            Assert.IsFalse(power.CanActivate(null),
                $"{power.PowerName} should return false when owner is null");
        }
    }
}

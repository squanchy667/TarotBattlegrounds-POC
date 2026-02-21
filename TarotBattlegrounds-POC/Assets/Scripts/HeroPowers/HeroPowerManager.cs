using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages hero power assignment and activation for all players (T113).
/// Singleton — attach to a persistent GameObject.
/// </summary>
public class HeroPowerManager : MonoBehaviour
{
    public static HeroPowerManager Instance { get; private set; }

    private Dictionary<int, HeroPowerBase> _playerPowers = new Dictionary<int, HeroPowerBase>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Assign a hero power to a player.</summary>
    public void AssignHeroPower(int playerId, HeroPowerBase power)
    {
        _playerPowers[playerId] = power;
        Debug.Log($"[HeroPowerManager] Player {playerId} assigned hero power: {power.PowerName}");
    }

    /// <summary>Get the hero power for a player (null if none).</summary>
    public HeroPowerBase GetHeroPower(int playerId)
    {
        return _playerPowers.TryGetValue(playerId, out var power) ? power : null;
    }

    /// <summary>
    /// Activate a player's hero power. Deducts coins and marks as used.
    /// Returns true if successfully activated.
    /// </summary>
    public bool ActivateHeroPower(int playerId, Player player)
    {
        var power = GetHeroPower(playerId);
        if (power == null)
        {
            Debug.LogWarning($"[HeroPowerManager] Player {playerId} has no hero power");
            return false;
        }

        if (!power.CanActivate(player))
        {
            Debug.Log($"[HeroPowerManager] Player {playerId} cannot activate {power.PowerName} (used={power.UsedThisTurn}, coins={player.coins}, cost={power.CoinCost})");
            return false;
        }

        player.coins -= power.CoinCost;
        power.UsedThisTurn = true;
        power.Activate(player);
        Debug.Log($"[HeroPowerManager] Player {playerId} activated {power.PowerName} for {power.CoinCost} coins");
        return true;
    }

    /// <summary>Reset all hero powers for a new turn.</summary>
    public void ResetAllForNewTurn()
    {
        foreach (var kvp in _playerPowers)
        {
            kvp.Value.ResetForNewTurn();
        }
    }

    /// <summary>Trigger passive hero powers at recruit phase start.</summary>
    public void TriggerRecruitPassives(Player player)
    {
        var power = GetHeroPower(player.playerId);
        if (power != null && power.IsPassive)
        {
            power.OnRecruitStart(player);
        }
    }

    /// <summary>Trigger passive hero powers at combat start.</summary>
    public void TriggerCombatPassives(Player player)
    {
        var power = GetHeroPower(player.playerId);
        if (power != null && power.IsPassive)
        {
            power.OnCombatStart(player);
        }
    }

    /// <summary>
    /// Get N random hero power choices for selection (T115).
    /// Returns distinct powers.
    /// </summary>
    public List<HeroPowerBase> GetRandomChoices(int count)
    {
        var allPowers = HeroPowerDatabase.GetAllPowers();
        return allPowers.OrderBy(x => Random.value).Take(count).ToList();
    }

    /// <summary>
    /// Auto-assign a random hero power for AI players.
    /// </summary>
    public void AutoAssignForAI(int playerId)
    {
        var choices = GetRandomChoices(1);
        if (choices.Count > 0)
        {
            AssignHeroPower(playerId, choices[0]);
        }
    }

    /// <summary>Check if any players have hero powers assigned.</summary>
    public bool HasAnyAssignments()
    {
        return _playerPowers.Count > 0;
    }

    /// <summary>Clear all assignments (for new game).</summary>
    public void ClearAll()
    {
        _playerPowers.Clear();
    }
}

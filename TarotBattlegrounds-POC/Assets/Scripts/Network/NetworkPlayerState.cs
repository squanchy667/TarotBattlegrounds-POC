using System;

/// <summary>
/// Serializable full player state for network transmission.
/// Sent from host to clients to synchronize game state.
/// </summary>
[Serializable]
public struct NetworkPlayerState
{
    public int playerIndex;
    public int coins;
    public int tavernTier;
    public int health;
    public bool shopFrozen;
    public bool isAlive;
    public bool isReady;
    public int upgradeCost; // M5: Synced upgrade cost for UI
    public string heroPowerId; // H5 fix: sync hero power selection to clients
    public bool heroPowerUsedThisTurn; // H5 fix: track hero power cooldown
    public NetworkCardData[] hand;
    public NetworkCardData[] board;
    public NetworkCardData[] shopCards;
}

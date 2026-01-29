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
    public NetworkCardData[] hand;
    public NetworkCardData[] board;
    public NetworkCardData[] shopCards;
}

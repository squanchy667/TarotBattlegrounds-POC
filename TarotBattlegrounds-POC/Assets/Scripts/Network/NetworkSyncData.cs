// ================================================================
// JSON helper structs (moved verbatim from NetworkGameBridge.cs,
// batch B god-class refactor). Plain data — no Photon types
// referenced, so unlike NetworkGameBridge.cs this file does NOT
// need the #if PHOTON_UNITY_NETWORKING wrapper (consistent with
// NetworkPlayerState.cs / NetworkCardData.cs, also unwrapped).
// ================================================================

[System.Serializable]
public struct ShopSyncData
{
    public int playerIndex;
    public NetworkCardData[] shopCards;
}

[System.Serializable]
public struct CombatResultData
{
    public int player1Index;
    public int player2Index;
    public string winner;
    public int damage;
}

[System.Serializable]
public struct NetworkGameOverData
{
    public int winnerPlayerIndex;
    public int[] standings;
    public int totalTurns;
}

[System.Serializable]
public struct DiscoverySyncData
{
    public int playerIndex;
    public NetworkCardData[] discoveryCards;
}

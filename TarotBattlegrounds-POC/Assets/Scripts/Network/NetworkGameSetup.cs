using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Runs when the Game scene loads in Multiplayer mode.
/// Assigns Photon players to game slots, fills remaining with AI,
/// configures GameConfig, and triggers game start on host.
/// </summary>
public class NetworkGameSetup : MonoBehaviour
{
    /// <summary>
    /// Set up network game when the scene loads.
    /// Should be called after GameManager.Awake() has run.
    /// </summary>
    private IEnumerator Start()
    {
        if (GameConfig.CurrentGameMode != GameConfig.GameMode.Multiplayer)
        {
            Debug.Log("[NetworkGameSetup] Not in multiplayer mode, skipping.");
            Destroy(gameObject);
            yield break;
        }

        // Wait for essential singletons
        yield return new WaitUntil(() => GameManager.Instance != null);
        yield return new WaitUntil(() => NetworkGameBridge.Instance != null);

        // Initialize card lookup for network serialization
        CardLookup.Initialize();

        AssignSlots();

        // Signal that network setup is complete
        GameManager.Instance.OnNetworkSetupComplete();

        Debug.Log("[NetworkGameSetup] Setup complete.");
    }

    private void AssignSlots()
    {
        int maxPlayers = GameConfig.PlayerCount;
        var photonPlayers = PhotonNetwork.PlayerList;

        // Sort by ActorNumber for deterministic ordering
        System.Array.Sort(photonPlayers, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        Dictionary<int, int> actorToSlot = new Dictionary<int, int>();
        Dictionary<int, int> slotToActor = new Dictionary<int, int>();
        HashSet<int> humanSlots = new HashSet<int>();

        // Assign each Photon player to a slot
        int slotIndex = 0;
        foreach (var player in photonPlayers)
        {
            if (slotIndex >= maxPlayers) break;

            actorToSlot[player.ActorNumber] = slotIndex;
            slotToActor[slotIndex] = player.ActorNumber;
            humanSlots.Add(slotIndex);

            Debug.Log($"[NetworkGameSetup] Actor {player.ActorNumber} ('{player.NickName}') → Slot {slotIndex}");
            slotIndex++;
        }

        // Fill remaining slots with AI (-1 actor number)
        for (int i = slotIndex; i < maxPlayers; i++)
        {
            slotToActor[i] = -1;
            Debug.Log($"[NetworkGameSetup] Slot {i} → AI");
        }

        // Configure GameConfig with the human slot set
        GameConfig.SetMultiplayerHumanSlots(humanSlots);

        // Pass mappings to the bridge
        NetworkGameBridge.Instance.AssignSlots(actorToSlot, slotToActor);

        Debug.Log($"[NetworkGameSetup] {humanSlots.Count} human players, {maxPlayers - humanSlots.Count} AI players.");
    }
}

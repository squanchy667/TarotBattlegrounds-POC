using UnityEngine;
using TarotBattlegrounds.Combat.Replay;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace TarotBattlegrounds.Combat.Network
{
    /// <summary>
    /// T318: Network replay broadcast — sends CombatReplay data from host to all clients
    /// via Photon RPCs so every client can play the animated replay locally.
    /// </summary>
    public class ReplayBroadcaster : MonoBehaviour
    {
        public static ReplayBroadcaster Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Broadcast a combat replay to all clients.
        /// Called by GameManager on the host after SimulateBattle.
        /// </summary>
        public void BroadcastReplay(CombatReplay replay, int p1, int p2)
        {
            if (replay == null) return;

#if PHOTON_UNITY_NETWORKING
            if (!PhotonNetwork.IsMasterClient) return;

            // Serialize replay to compressed bytes
            byte[] data = ReplaySerializer.SerializeCompressed(replay);
            if (data == null || data.Length == 0)
            {
                Debug.LogError("[ReplayBroadcaster] Failed to serialize replay");
                return;
            }

            Debug.Log($"[ReplayBroadcaster] Broadcasting replay: {data.Length} bytes for P{p1+1} vs P{p2+1}");

            // For large replays, chunk the data (Photon has ~500KB RPC limit)
            if (data.Length > 400000)
            {
                Debug.LogWarning($"[ReplayBroadcaster] Replay too large ({data.Length} bytes), skipping broadcast");
                return;
            }

            var view = GetComponent<PhotonView>();
            if (view != null)
            {
                view.RPC("RpcReceiveReplay", RpcTarget.Others, data, p1, p2);
            }
#endif
        }

#if PHOTON_UNITY_NETWORKING
        [PunRPC]
        private void RpcReceiveReplay(byte[] data, int p1, int p2)
        {
            Debug.Log($"[ReplayBroadcaster] Received replay: {data.Length} bytes for P{p1+1} vs P{p2+1}");

            CombatReplay replay = ReplaySerializer.DeserializeCompressed(data);
            if (replay == null)
            {
                Debug.LogError("[ReplayBroadcaster] Failed to deserialize replay");
                return;
            }

            // Check if this battle involves the local player
            int localSlot = NetworkGameBridge.Instance != null ? NetworkGameBridge.Instance.LocalPlayerSlot : -1;
            if (localSlot < 0 || (localSlot != p1 && localSlot != p2))
            {
                Debug.Log("[ReplayBroadcaster] Replay not for local player, skipping animation");
                return;
            }

            // Play the animated replay
            if (Animator.CombatAnimator.Instance != null)
            {
                Animator.CombatAnimator.Instance.PlayReplay(replay);
            }
        }
#endif

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// T611-T613: Network optimization for 8-player games.
/// DeltaStateCompressor sends only changed fields per sync.
/// MessageBatcher aggregates RPCs per frame to reduce overhead.
/// BandwidthProfiler tracks bytes sent/received for monitoring.
/// </summary>
public class DeltaStateCompressor : MonoBehaviour
{
    public static DeltaStateCompressor Instance { get; private set; }

    // T611: Delta state tracking per player
    private Dictionary<int, Dictionary<string, object>> lastKnownState = new Dictionary<int, Dictionary<string, object>>();

    // T612: Message batching
    private List<BatchedMessage> pendingMessages = new List<BatchedMessage>();
    private float batchTimer;
    private const float BATCH_INTERVAL = 0.05f; // 20 batches per second max

    // T613: Bandwidth profiling
    public long BytesSent { get; private set; }
    public long BytesReceived { get; private set; }
    public int MessagesSent { get; private set; }
    public int MessagesReceived { get; private set; }
    public float BytesPerSecondSent { get; private set; }
    public float BytesPerSecondReceived { get; private set; }

    private long byteSentWindow;
    private long byteReceivedWindow;
    private float windowTimer;
    private const float PROFILE_WINDOW = 1f;

    private struct BatchedMessage
    {
        public string type;
        public string data;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // T612: Flush batched messages
        batchTimer += Time.deltaTime;
        if (batchTimer >= BATCH_INTERVAL && pendingMessages.Count > 0)
        {
            FlushBatch();
            batchTimer = 0f;
        }

        // T613: Update bandwidth profiler
        windowTimer += Time.deltaTime;
        if (windowTimer >= PROFILE_WINDOW)
        {
            BytesPerSecondSent = byteSentWindow / windowTimer;
            BytesPerSecondReceived = byteReceivedWindow / windowTimer;
            byteSentWindow = 0;
            byteReceivedWindow = 0;
            windowTimer = 0f;
        }
    }

    /// <summary>
    /// T611: Compute delta between current state and last known state.
    /// Returns only the fields that changed. Returns null if nothing changed.
    /// </summary>
    public Dictionary<string, object> ComputeDelta(int playerIndex, Dictionary<string, object> currentState)
    {
        if (!lastKnownState.ContainsKey(playerIndex))
        {
            lastKnownState[playerIndex] = new Dictionary<string, object>(currentState);
            return currentState; // First sync: send everything
        }

        var last = lastKnownState[playerIndex];
        var delta = new Dictionary<string, object>();

        foreach (var kvp in currentState)
        {
            if (!last.ContainsKey(kvp.Key) || !Equals(last[kvp.Key], kvp.Value))
            {
                delta[kvp.Key] = kvp.Value;
            }
        }

        if (delta.Count == 0) return null;

        // Update last known state
        foreach (var kvp in delta)
            last[kvp.Key] = kvp.Value;

        return delta;
    }

    /// <summary>
    /// T611: Apply received delta to reconstruct full state.
    /// </summary>
    public Dictionary<string, object> ApplyDelta(int playerIndex, Dictionary<string, object> delta)
    {
        if (!lastKnownState.ContainsKey(playerIndex))
            lastKnownState[playerIndex] = new Dictionary<string, object>();

        var state = lastKnownState[playerIndex];
        foreach (var kvp in delta)
            state[kvp.Key] = kvp.Value;

        return state;
    }

    /// <summary>
    /// T612: Queue a message for batched sending.
    /// </summary>
    public void QueueMessage(string type, string data)
    {
        pendingMessages.Add(new BatchedMessage { type = type, data = data });
    }

    /// <summary>
    /// T612: Flush all pending messages as a single batch.
    /// </summary>
    private void FlushBatch()
    {
        if (pendingMessages.Count == 0) return;

        string batchJson = JsonConvert.SerializeObject(pendingMessages);
        int byteSize = System.Text.Encoding.UTF8.GetByteCount(batchJson);

        // Track bandwidth
        BytesSent += byteSize;
        byteSentWindow += byteSize;
        MessagesSent += pendingMessages.Count;

        // Send via Photon RPC (would be called by NetworkGameBridge)
        Debug.Log($"[DeltaState] Flushed batch: {pendingMessages.Count} messages, {byteSize} bytes");
        pendingMessages.Clear();
    }

    /// <summary>
    /// T613: Record received data for bandwidth tracking.
    /// </summary>
    public void RecordReceived(int bytes)
    {
        BytesReceived += bytes;
        byteReceivedWindow += bytes;
        MessagesReceived++;
    }

    /// <summary>
    /// T613: Get bandwidth report string.
    /// </summary>
    public string GetBandwidthReport()
    {
        return $"Bandwidth: {BytesPerSecondSent:F0} B/s out, {BytesPerSecondReceived:F0} B/s in | " +
               $"Total: {BytesSent / 1024f:F1} KB sent, {BytesReceived / 1024f:F1} KB received | " +
               $"Messages: {MessagesSent} sent, {MessagesReceived} received";
    }

    /// <summary>Reset all state (on game end).</summary>
    public void ResetState()
    {
        lastKnownState.Clear();
        pendingMessages.Clear();
        BytesSent = 0;
        BytesReceived = 0;
        MessagesSent = 0;
        MessagesReceived = 0;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Initializes the card pool with CardDatabase cards.
/// If RuntimeDataLoader is present and loading, waits for it to complete first.
/// Attach to a scene GameObject to auto-initialize on start.
/// </summary>
public class CardPoolInitializer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Use the generated CardDatabase instead of assigned masterCards")]
    public bool useGeneratedDatabase = true;

    [Tooltip("Also initialize synergy test data")]
    public bool initializeSynergies = true;

    [Header("Debug")]
    [Tooltip("Print card pool summary on start")]
    public bool printSummary = true;

    private void Start()
    {
        // If RuntimeDataLoader exists and hasn't finished yet, wait for it
        if (RuntimeDataLoader.Instance != null && !RuntimeDataLoader.Instance.IsComplete)
        {
            Debug.Log("[CardPoolInitializer] Waiting for RuntimeDataLoader to complete...");
            RuntimeDataLoader.Instance.OnLoadComplete += OnRuntimeDataLoaded;
        }
        else
        {
            Initialize();
        }
    }

    private void OnRuntimeDataLoaded()
    {
        if (RuntimeDataLoader.Instance != null)
            RuntimeDataLoader.Instance.OnLoadComplete -= OnRuntimeDataLoaded;

        Debug.Log($"[CardPoolInitializer] RuntimeDataLoader complete (loaded={RuntimeDataLoader.Instance?.IsLoaded})");
        Initialize();
    }

    /// <summary>
    /// Initialize the card pool.
    /// CardDatabase.GenerateAllCards() automatically checks RuntimeDataLoader for runtime data.
    /// </summary>
    [ContextMenu("Initialize Card Pool")]
    public void Initialize()
    {
        if (!useGeneratedDatabase)
        {
            Debug.Log("[CardPoolInitializer] Using assigned masterCards (not generated database)");
            return;
        }

        var tavern = TavernManager.Instance;
        if (tavern == null)
        {
            Debug.LogError("[CardPoolInitializer] TavernManager not found!");
            return;
        }

        // Apply runtime config overrides (tierCopies, shopSizes) before generating pool
        if (RuntimeDataLoader.Instance != null && RuntimeDataLoader.Instance.IsLoaded && RuntimeDataLoader.Instance.Config != null)
        {
            tavern.ApplyRuntimeConfig(RuntimeDataLoader.Instance.Config);
        }

        // Generate cards from database (auto-uses runtime data if available)
        List<Card> databaseCards = CardDatabase.GenerateAllCards();

        // Assign to TavernManager's masterCards
        tavern.masterCards = databaseCards;

        // Reset the pool to regenerate with new cards
        tavern.ResetPool();

        string source = CardDatabase.IsUsingRuntimeData ? "runtime JSON" : "built-in CardDatabase";
        Debug.Log($"[CardPoolInitializer] Initialized TavernManager with {databaseCards.Count} cards from {source}");

        // Initialize synergies (also checks RuntimeDataLoader)
        if (initializeSynergies)
        {
            InitializeSynergiesFromBestSource();
        }

        // Print summary if requested
        if (printSummary)
        {
            CardDatabase.PrintCardPoolSummary();
        }
    }

    /// <summary>
    /// Initialize synergies from runtime data if available, otherwise from SynergyTestData.
    /// </summary>
    private void InitializeSynergiesFromBestSource()
    {
        // Ensure ThemeManager exists
        ThemeManager.EnsureExists();

        // Auto-create SynergyManager if it doesn't exist
        if (SynergyManager.Instance == null)
        {
            GameObject synergyManagerObj = new GameObject("SynergyManager");
            synergyManagerObj.AddComponent<SynergyManager>();
        }

        // Try runtime data first
        if (RuntimeDataLoader.Instance != null && RuntimeDataLoader.Instance.IsLoaded && RuntimeDataLoader.Instance.Synergies != null)
        {
            var runtimeSynergies = RuntimeDataLoader.Instance.BuildSynergies();
            if (runtimeSynergies != null && runtimeSynergies.Length > 0)
            {
                SynergyManager.Instance.tribeSynergies = runtimeSynergies;
                SynergyManager.Instance.InitializeSynergyCache();
                Debug.Log($"[CardPoolInitializer] Initialized SynergyManager with {runtimeSynergies.Length} runtime synergies");
                return;
            }
        }

        // Fallback to built-in synergy data
        SynergyTestData.InitializeSynergyManager();
    }

    /// <summary>
    /// Add tribe test cards to the pool for synergy testing.
    /// </summary>
    [ContextMenu("Add Tribe Test Cards")]
    public void AddTribeTestCards()
    {
        var tavern = TavernManager.Instance;
        if (tavern == null)
        {
            Debug.LogError("[CardPoolInitializer] TavernManager not found!");
            return;
        }

        SynergyTestCards.AddTestCardsToPool(tavern);
    }

    /// <summary>
    /// Reset pool with fresh cards.
    /// </summary>
    [ContextMenu("Reset Pool")]
    public void ResetPool()
    {
        var tavern = TavernManager.Instance;
        if (tavern != null)
        {
            tavern.ResetPool();
            Debug.Log("[CardPoolInitializer] Pool reset");
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Initializes the card pool with CardDatabase cards.
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
        Initialize();
    }

    /// <summary>
    /// Initialize the card pool.
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

        // Generate cards from database
        List<Card> databaseCards = CardDatabase.GenerateAllCards();

        // Assign to TavernManager's masterCards
        tavern.masterCards = databaseCards;

        // Reset the pool to regenerate with new cards
        tavern.ResetPool();

        Debug.Log($"[CardPoolInitializer] Initialized TavernManager with {databaseCards.Count} cards from CardDatabase");

        // Optionally initialize synergies
        if (initializeSynergies)
        {
            SynergyTestData.InitializeSynergyManager();
        }

        // Print summary if requested
        if (printSummary)
        {
            CardDatabase.PrintCardPoolSummary();
        }
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

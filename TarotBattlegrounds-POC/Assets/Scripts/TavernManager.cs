using UnityEngine;
using System.Collections.Generic;

public class TavernManager : MonoBehaviour
{
    public List<Card> availableCards;  // Assign in Inspector

    void Start()  // Keeps original logic, runs auto
    {
        RefreshShop();  // Call our new method
    }

    public void RefreshShop()  // Public for external calls
    {
        Debug.Log("Tavern refreshed: Available cards - " + availableCards.Count);
        // Later: Randomize cards, etc.
    }
}
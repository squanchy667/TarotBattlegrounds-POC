#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool to generate Card ScriptableObjects from CardDatabase.
/// Use: Tools > Tarot BG > Generate Card Assets
/// </summary>
public class CardDatabaseGenerator : EditorWindow
{
    private string outputFolder = "Assets/Cards/cards/Generated";
    private bool overwriteExisting = false;

    [MenuItem("Tools/Tarot BG/Generate Card Assets")]
    public static void ShowWindow()
    {
        GetWindow<CardDatabaseGenerator>("Card Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Card Database Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing:", overwriteExisting);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate All 30 Cards", GUILayout.Height(40)))
        {
            GenerateAllCards();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clear Generated Cards", GUILayout.Height(30)))
        {
            ClearGeneratedCards();
        }
    }

    private void GenerateAllCards()
    {
        // Create output directory if it doesn't exist
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string parent = Path.GetDirectoryName(outputFolder);
            string folderName = Path.GetFileName(outputFolder);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        List<Card> cards = CardDatabase.GenerateAllCards();
        int created = 0;
        int skipped = 0;

        foreach (Card card in cards)
        {
            string assetPath = $"{outputFolder}/{card.cardName}.asset";

            // Check if already exists
            if (AssetDatabase.LoadAssetAtPath<Card>(assetPath) != null)
            {
                if (!overwriteExisting)
                {
                    skipped++;
                    continue;
                }
                AssetDatabase.DeleteAsset(assetPath);
            }

            // Create a new instance (the CardDatabase ones are runtime-only)
            Card newCard = ScriptableObject.CreateInstance<Card>();
            CopyCardData(card, newCard);

            AssetDatabase.CreateAsset(newCard, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CardDatabaseGenerator] Created {created} cards, skipped {skipped} existing.");
        EditorUtility.DisplayDialog("Generation Complete",
            $"Created: {created} cards\nSkipped: {skipped} existing\n\nCards saved to: {outputFolder}",
            "OK");
    }

    private void CopyCardData(Card source, Card dest)
    {
        dest.cardName = source.cardName;
        dest.tier = source.tier;
        dest.attack = source.attack;
        dest.health = source.health;
        dest.ability = source.ability;
        dest.tribe = source.tribe;

        // Copy tribes array
        if (source.tribes != null)
        {
            dest.tribes = new TribeType[source.tribes.Length];
            System.Array.Copy(source.tribes, dest.tribes, source.tribes.Length);
        }

        // Copy ability system fields
        dest.abilityTrigger = source.abilityTrigger;
        dest.abilityEffect = source.abilityEffect;
        dest.abilityValue = source.abilityValue;

        // Copy legacy effect fields
        dest.effectType = source.effectType;
        dest.effectParameter = source.effectParameter;

        // Copy economy fields
        dest.buyCostModifier = source.buyCostModifier;
        dest.sellValueModifier = source.sellValueModifier;
    }

    private void ClearGeneratedCards()
    {
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Debug.Log("[CardDatabaseGenerator] No generated folder found.");
            return;
        }

        string[] assets = AssetDatabase.FindAssets("t:Card", new[] { outputFolder });
        int deleted = 0;

        foreach (string guid in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
            deleted++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[CardDatabaseGenerator] Deleted {deleted} cards from {outputFolder}");
    }

    /// <summary>
    /// Quick generate without opening window.
    /// </summary>
    [MenuItem("Tools/Tarot BG/Quick Generate Cards")]
    public static void QuickGenerate()
    {
        string folder = "Assets/Cards/cards/Generated";

        // Create folder if needed
        if (!AssetDatabase.IsValidFolder(folder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Cards/cards"))
            {
                AssetDatabase.CreateFolder("Assets/Cards", "cards");
            }
            AssetDatabase.CreateFolder("Assets/Cards/cards", "Generated");
        }

        List<Card> cards = CardDatabase.GenerateAllCards();
        int count = 0;

        foreach (Card card in cards)
        {
            string assetPath = $"{folder}/{card.cardName}.asset";

            // Skip if exists
            if (AssetDatabase.LoadAssetAtPath<Card>(assetPath) != null)
                continue;

            Card newCard = ScriptableObject.CreateInstance<Card>();

            // Copy all fields
            newCard.cardName = card.cardName;
            newCard.tier = card.tier;
            newCard.attack = card.attack;
            newCard.health = card.health;
            newCard.ability = card.ability;
            newCard.tribe = card.tribe;
            if (card.tribes != null)
            {
                newCard.tribes = new TribeType[card.tribes.Length];
                System.Array.Copy(card.tribes, newCard.tribes, card.tribes.Length);
            }
            newCard.abilityTrigger = card.abilityTrigger;
            newCard.abilityEffect = card.abilityEffect;
            newCard.abilityValue = card.abilityValue;
            newCard.effectType = card.effectType;
            newCard.effectParameter = card.effectParameter;

            AssetDatabase.CreateAsset(newCard, assetPath);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CardDatabaseGenerator] Quick generated {count} new cards.");
    }
}
#endif

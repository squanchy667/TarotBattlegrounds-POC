using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CardImporter : Editor
{
    [MenuItem("Tools/Import Tarot Cards")]
    public static void ImportCards()
    {
        string csvPath = "Assets/Data/TarotCards.csv"; // Path to your CSV
        string assetPath = "Assets/Cards/cards/"; // Path to save ScriptableObjects
        string[] validTribes = new string[] { 
            "Wands", "Cups", "Swords", "Pentacles", 
            "Wands/Pentacles", "Cups/Swords", "Swords/Wands", "Pentacles/Cups", 
            "All Suits", "Neutral" 
        }; // Based on your CSV data
        int cardsCreated = 0, errors = 0, skipped = 0;

        // Create output folder if it doesn't exist
        if (!Directory.Exists(assetPath))
            Directory.CreateDirectory(assetPath);

        // Read CSV
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV file not found at: {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            Debug.LogError("CSV is empty or missing header row.");
            return;
        }

        // Parse headers (handle potential misspelling)
        string[] headers = Regex.Split(lines[0], ",(?=([^\"]*\"[^\"]*\")*[^\"]*$)");
        Dictionary<string, int> headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
            headerMap[headers[i].Trim()] = i;

        // Validate required headers (ignore "Tribe Desscription")
        string[] requiredHeaders = { "Name", "Faction", "Tier", "Attack", "Health", "Ability" };
        foreach (string header in requiredHeaders)
        {
            if (!headerMap.ContainsKey(header))
            {
                Debug.LogError($"Missing required CSV column: {header}");
                return;
            }
        }

        // Process rows (skip header)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) { skipped++; continue; }

            // Split with regex to handle quotes/commas
            string[] values = Regex.Split(line, ",(?=([^\"]*\"[^\"]*\")*[^\"]*$)");
            if (values.Length < requiredHeaders.Length)
            {
                Debug.LogWarning($"Skipping row {i}: Insufficient columns ({values.Length}).");
                skipped++;
                continue;
            }

            try
            {
                // Extract and clean data
                string cardName = values[headerMap["Name"]].Trim().Replace("\"", "");
                if (string.IsNullOrEmpty(cardName))
                {
                    Debug.Log($"Skipping row {i}: Empty card name (likely group description).");
                    skipped++;
                    continue;
                }

                // Validate tier
                if (!int.TryParse(values[headerMap["Tier"]].Trim(), out int tier) || tier < 1 || tier > 6)
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid tier ({values[headerMap["Tier"]]}). Must be 1-6.");
                    errors++;
                    continue;
                }

                // Validate tribe (Faction)
                string tribe = values[headerMap["Faction"]].Trim().Replace("\"", "");
                if (string.IsNullOrEmpty(tribe) || !System.Array.Exists(validTribes, t => t.Equals(tribe, System.StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid tribe ({tribe}). Valid tribes: {string.Join(", ", validTribes)}.");
                    errors++;
                    continue;
                }

                // Validate stats
                if (!int.TryParse(values[headerMap["Attack"]].Trim(), out int attack) || attack < 0)
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid attack ({values[headerMap["Attack"]]}). Must be non-negative.");
                    errors++;
                    continue;
                }
                if (!int.TryParse(values[headerMap["Health"]].Trim(), out int health) || health <= 0)
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid health ({values[headerMap["Health"]]}). Must be positive.");
                    errors++;
                    continue;
                }

                string ability = values[headerMap["Ability"]].Trim().Replace("\"", "");

                // Create ScriptableObject
                Card newCard = ScriptableObject.CreateInstance<Card>();
                newCard.name = cardName; // Sets asset name
                newCard.tier = tier;
                newCard.tribe = tribe;
                newCard.attack = attack;
                newCard.health = health;
                newCard.ability = ability;

                // Save asset with unique path
                string assetFilePath = AssetDatabase.GenerateUniqueAssetPath($"{assetPath}{cardName}.asset");
                AssetDatabase.CreateAsset(newCard, assetFilePath);
                cardsCreated++;
                Debug.Log($"Created card: {cardName} (Tier {tier}, {tribe}, {attack}/{health}, Ability: {ability})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error processing row {i}: {ex.Message}");
                errors++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Import complete: {cardsCreated} cards created, {errors} errors, {skipped} rows skipped.");
    }
}
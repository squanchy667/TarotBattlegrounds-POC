using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CardImporter : Editor
{
    [MenuItem("Tools/Game/Import Cards")]
    public static void ImportCards()
    {
        string csvPath = "Assets/Data/TarotCards.csv";
        string assetPath = "Assets/Cards/cards/";

        // Get valid tribes dynamically from ThemeConfig
        ThemeManager.EnsureExists();
        string[] validTribes = ThemeManager.ActiveTheme?.GetValidTribeCombinations()
            ?? new string[] { "Tribe1", "Tribe2", "Tribe3", "Tribe4", "All Suits", "Neutral" };
        string[] validEffectTypes = new string[] { 
            "NoEffect", "Summoning", "LastReading", "Guardian", "Aegis", "Echo" 
        };
        int cardsCreated = 0, errors = 0, skipped = 0;

        if (!Directory.Exists(assetPath))
            Directory.CreateDirectory(assetPath);

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

        string[] headers = lines[0].Split(',');
        Dictionary<string, int> headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i].Trim().Replace("\"", "");
            headerMap[header] = i;
            Debug.Log($"Header {i}: {header}");
        }

        string[] requiredHeaders = { "Tribe Desscription", "Name", "Faction", "Tier", "Attack", "Health" };
        string[] optionalHeaders = { "Ability", "EffectType", "EffectParameter" };
        foreach (string header in requiredHeaders)
        {
            if (!headerMap.ContainsKey(header))
            {
                Debug.LogError($"Missing required CSV column: {header}");
                return;
            }
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) 
            {
                Debug.Log($"Skipping row {i}: Empty row");
                skipped++;
                continue;
            }

            Debug.Log($"Processing row {i}: {line}");
            string[] values = Regex.Split(line, ",(?=([^\"]*\"[^\"]*\")*[^\"]*$)");
            if (values.Length < requiredHeaders.Length)
            {
                Debug.LogWarning($"Skipping row {i}: Insufficient columns ({values.Length}, expected at least {requiredHeaders.Length})");
                skipped++;
                continue;
            }

            Debug.Log($"Row {i} values: {string.Join(" | ", values)}");

            try
            {
                string cardName = values[headerMap["Name"]].Trim().Replace("\"", "");
                if (string.IsNullOrEmpty(cardName))
                {
                    Debug.Log($"Skipping row {i}: Empty card name (likely group description).");
                    skipped++;
                    continue;
                }

                string tierRaw = values[headerMap["Tier"]].Trim();
                if (!int.TryParse(tierRaw, out int tier) || tier < 1 || tier > 6)
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid tier ({tierRaw}). Must be 1-6.");
                    errors++;
                    continue;
                }

                string tribe = values[headerMap["Faction"]].Trim().Replace("\"", "");
                if (string.IsNullOrEmpty(tribe) || !System.Array.Exists(validTribes, t => t.Equals(tribe, System.StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid tribe ({tribe}). Valid tribes: {string.Join(", ", validTribes)}.");
                    errors++;
                    continue;
                }

                string attackRaw = values[headerMap["Attack"]].Trim();
                if (!int.TryParse(attackRaw, out int attack) || attack < 0)
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid attack ({attackRaw}). Must be non-negative.");
                    errors++;
                    continue;
                }

                string healthRaw = values[headerMap["Health"]].Trim();
                if (!int.TryParse(healthRaw, out int health) || health <= 0)
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid health ({healthRaw}). Must be positive.");
                    errors++;
                    continue;
                }

                string ability = headerMap.ContainsKey("Ability") && values.Length > headerMap["Ability"] ? values[headerMap["Ability"]].Trim().Replace("\"", "") : "";
                string effectTypeRaw = headerMap.ContainsKey("EffectType") && values.Length > headerMap["EffectType"] ? values[headerMap["EffectType"]].Trim().Replace("\"", "") : "NoEffect";
                string effectParameter = headerMap.ContainsKey("EffectParameter") && values.Length > headerMap["EffectParameter"] ? values[headerMap["EffectParameter"]].Trim().Replace("\"", "") : "";

                if (!System.Array.Exists(validEffectTypes, t => t.Equals(effectTypeRaw, System.StringComparison.OrdinalIgnoreCase)))
                {
                    Debug.LogWarning($"Skipping row {i}: Invalid effect type ({effectTypeRaw}). Valid types: {string.Join(", ", validEffectTypes)}.");
                    errors++;
                    continue;
                }
                Card.EffectType effectType = (Card.EffectType)System.Enum.Parse(typeof(Card.EffectType), effectTypeRaw, true);

                Card newCard = ScriptableObject.CreateInstance<Card>();
                newCard.name = cardName;
                newCard.tier = tier;
                newCard.tribe = tribe;
                newCard.attack = attack;
                newCard.health = health;
                newCard.ability = ability;
                newCard.effectType = effectType;
                newCard.effectParameter = effectParameter;
                newCard.hasAegis = effectType == Card.EffectType.Aegis;

                string assetFilePath = AssetDatabase.GenerateUniqueAssetPath($"{assetPath}{cardName}.asset");
                AssetDatabase.CreateAsset(newCard, assetFilePath);
                cardsCreated++;
                Debug.Log($"Created card: {cardName} (Tier {tier}, {tribe}, {attack}/{health}, Ability: {ability}, Effect: {effectType}, Param: {effectParameter})");
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
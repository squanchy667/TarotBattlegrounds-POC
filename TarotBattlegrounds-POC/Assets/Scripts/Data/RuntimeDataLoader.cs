using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// Fetches game data JSON from a remote URL on startup.
/// Falls back to built-in CardDatabase/SynergyTestData if loading fails.
/// Must execute before CardPoolInitializer (use Script Execution Order or place in a preload scene).
/// </summary>
public class RuntimeDataLoader : MonoBehaviour
{
    public static RuntimeDataLoader Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Assign a DataConfig ScriptableObject, or leave null to use built-in data")]
    public DataConfig dataConfig;

    /// <summary>Whether runtime data was successfully loaded.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Whether loading has completed (success or failure).</summary>
    public bool IsComplete { get; private set; }

    /// <summary>Error message if loading failed, null otherwise.</summary>
    public string Error { get; private set; }

    // Parsed data
    public List<RuntimeCardData> Cards { get; private set; }
    public List<RuntimeSynergyData> Synergies { get; private set; }
    public RuntimeGameConfig Config { get; private set; }

    /// <summary>Fires when loading completes (success or failure).</summary>
    public event Action OnLoadComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (dataConfig == null || !dataConfig.enableRuntimeLoading)
        {
            Debug.Log("[RuntimeDataLoader] Runtime loading disabled or no DataConfig assigned. Using built-in data.");
            IsComplete = true;
            OnLoadComplete?.Invoke();
            return;
        }

        StartCoroutine(LoadAllData());
    }

    private IEnumerator LoadAllData()
    {
        string baseUrl = dataConfig.dataBaseUrl;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";

        Debug.Log($"[RuntimeDataLoader] Loading data from {baseUrl}");

        string cardsJson = null;
        string synergiesJson = null;
        string configJson = null;
        string cardsError = null;
        string synergiesError = null;
        string configError = null;

        // Fetch all three in parallel using coroutines
        bool cardsDone = false, synergiesDone = false, configDone = false;

        StartCoroutine(FetchJson(baseUrl + "cards.json", (json, err) => { cardsJson = json; cardsError = err; cardsDone = true; }));
        StartCoroutine(FetchJson(baseUrl + "synergies.json", (json, err) => { synergiesJson = json; synergiesError = err; synergiesDone = true; }));
        StartCoroutine(FetchJson(baseUrl + "config.json", (json, err) => { configJson = json; configError = err; configDone = true; }));

        // Wait for all to complete
        while (!cardsDone || !synergiesDone || !configDone)
            yield return null;

        // Parse results
        bool allGood = true;

        if (cardsJson != null)
        {
            try
            {
                Cards = JsonConvert.DeserializeObject<List<RuntimeCardData>>(cardsJson, GetJsonSettings());
                Debug.Log($"[RuntimeDataLoader] Loaded {Cards.Count} cards from remote");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RuntimeDataLoader] Failed to parse cards.json: {e.Message}");
                allGood = false;
            }
        }
        else
        {
            Debug.LogWarning($"[RuntimeDataLoader] Failed to fetch cards.json: {cardsError}");
            allGood = false;
        }

        if (synergiesJson != null)
        {
            try
            {
                Synergies = JsonConvert.DeserializeObject<List<RuntimeSynergyData>>(synergiesJson, GetJsonSettings());
                Debug.Log($"[RuntimeDataLoader] Loaded {Synergies.Count} synergies from remote");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RuntimeDataLoader] Failed to parse synergies.json: {e.Message}");
                allGood = false;
            }
        }
        else
        {
            Debug.LogWarning($"[RuntimeDataLoader] Failed to fetch synergies.json: {synergiesError}");
            allGood = false;
        }

        if (configJson != null)
        {
            try
            {
                Config = JsonConvert.DeserializeObject<RuntimeGameConfig>(configJson, GetJsonSettings());
                Debug.Log("[RuntimeDataLoader] Loaded game config from remote");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RuntimeDataLoader] Failed to parse config.json: {e.Message}");
                allGood = false;
            }
        }
        else
        {
            Debug.LogWarning($"[RuntimeDataLoader] Failed to fetch config.json: {configError}");
            allGood = false;
        }

        IsLoaded = allGood;
        IsComplete = true;

        if (allGood)
            Debug.Log("[RuntimeDataLoader] All runtime data loaded successfully");
        else if (dataConfig.fallbackToBuiltIn)
            Debug.LogWarning("[RuntimeDataLoader] Some data failed to load. CardPoolInitializer will use built-in fallback.");
        else
            Error = "Failed to load one or more data files";

        OnLoadComplete?.Invoke();
    }

    private IEnumerator FetchJson(string url, Action<string, string> callback)
    {
        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = Mathf.RoundToInt(dataConfig.requestTimeout);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback(request.downloadHandler.text, null);
            }
            else
            {
                callback(null, $"{request.responseCode} {request.error}");
            }
        }
    }

    private static JsonSerializerSettings GetJsonSettings()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new StringEnumConverter());
        return settings;
    }

    // ================================================================
    // Methods to convert runtime data into Unity objects
    // ================================================================

    /// <summary>
    /// Convert loaded card data into Card ScriptableObjects.
    /// </summary>
    public List<Card> BuildCards()
    {
        if (Cards == null) return null;

        var result = new List<Card>();
        foreach (var data in Cards)
        {
            Card card = ScriptableObject.CreateInstance<Card>();
            card.cardName = data.cardName;
            card.tier = data.tier;
            card.attack = data.attack;
            card.health = data.health;
            card.ability = data.ability ?? "";
            card.buyCostModifier = data.buyCostModifier;
            card.sellValueModifier = data.sellValueModifier;

            // Parse tribes
            if (data.tribes != null && data.tribes.Length > 0)
            {
                card.tribes = new TribeType[data.tribes.Length];
                for (int i = 0; i < data.tribes.Length; i++)
                {
                    if (Enum.TryParse(data.tribes[i], true, out TribeType tt))
                        card.tribes[i] = tt;
                    else
                        card.tribes[i] = TribeType.None;
                }
            }
            else
            {
                card.tribes = new TribeType[0];
            }

            // Set legacy tribe field
            if (card.tribes.Length > 0 && card.tribes[0] != TribeType.None)
                card.tribe = card.tribes[0].ToString();

            // Parse ability trigger
            if (Enum.TryParse(data.abilityTrigger, true, out AbilityTrigger trigger))
                card.abilityTrigger = trigger;

            // Parse ability effect
            if (Enum.TryParse(data.abilityEffect, true, out Card.AbilityEffectType effect))
                card.abilityEffect = effect;

            card.abilityValue = data.abilityValue;

            // Parse effect type (Guardian, Aegis, etc.)
            if (!string.IsNullOrEmpty(data.effectType))
            {
                if (Enum.TryParse(data.effectType, true, out Card.EffectType et))
                    card.effectType = et;
            }

            result.Add(card);
        }

        Debug.Log($"[RuntimeDataLoader] Built {result.Count} Card objects from runtime data");
        return result;
    }

    /// <summary>
    /// Convert loaded synergy data into TribeSynergy ScriptableObjects.
    /// </summary>
    public TribeSynergy[] BuildSynergies()
    {
        if (Synergies == null) return null;

        var result = new List<TribeSynergy>();
        foreach (var data in Synergies)
        {
            var synergy = ScriptableObject.CreateInstance<TribeSynergy>();

            if (Enum.TryParse(data.tribeType, true, out TribeType tribe))
                synergy.tribe = tribe;

            synergy.tribeName = ThemeManager.GetTribeName(synergy.tribe);
            synergy.description = ThemeManager.GetTribeDescription(synergy.tribe);
            synergy.themeColor = ThemeManager.GetTribeColor(synergy.tribe);

            // Parse tiers
            if (data.tiers != null)
            {
                synergy.tiers = new SynergyTier[data.tiers.Length];
                for (int i = 0; i < data.tiers.Length; i++)
                {
                    var td = data.tiers[i];
                    synergy.tiers[i] = new SynergyTier
                    {
                        threshold = td.threshold,
                        value = td.value,
                        description = td.description ?? ""
                    };

                    if (Enum.TryParse(td.trigger, true, out SynergyTrigger st))
                        synergy.tiers[i].trigger = st;
                    if (Enum.TryParse(td.effect, true, out SynergyEffect se))
                        synergy.tiers[i].effect = se;
                    if (Enum.TryParse(td.target, true, out SynergyTarget sta))
                        synergy.tiers[i].target = sta;
                }
            }

            // Parse combo
            if (data.combo != null)
            {
                if (Enum.TryParse(data.combo.tribe, true, out TribeType comboTribe))
                    synergy.comboTribe = comboTribe;
                synergy.comboThreshold = data.combo.threshold;
                if (Enum.TryParse(data.combo.effect, true, out SynergyEffect comboEffect))
                    synergy.comboEffect = comboEffect;
                synergy.comboValue = data.combo.value;
                synergy.comboDescription = data.combo.description ?? "";
            }

            result.Add(synergy);
        }

        Debug.Log($"[RuntimeDataLoader] Built {result.Count} TribeSynergy objects from runtime data");
        return result.ToArray();
    }
}

// ================================================================
// JSON data classes (match the TypeScript types from tarot-devzone/shared)
// ================================================================

[Serializable]
public class RuntimeCardData
{
    public string id;
    public string cardName;
    public int tier;
    public int attack;
    public int health;
    public string[] tribes;
    public string imageUrl;
    public string ability;
    public string abilityTrigger;
    public string abilityEffect;
    public int abilityValue;
    public string effectType;
    public int buyCostModifier;
    public int sellValueModifier;
}

[Serializable]
public class RuntimeSynergyData
{
    public string tribeType;
    public RuntimeSynergyTierData[] tiers;
    public RuntimeSynergyComboData combo;
}

[Serializable]
public class RuntimeSynergyTierData
{
    public int threshold;
    public string trigger;
    public string effect;
    public string target;
    public int value;
    public string description;
}

[Serializable]
public class RuntimeSynergyComboData
{
    public string tribe;
    public int threshold;
    public string effect;
    public int value;
    public string description;
}

[Serializable]
public class RuntimeGameConfig
{
    public Dictionary<string, int> tierCopies;
    public Dictionary<string, int> shopSizes;
    public int baseBuyCost;
    public float goldenMultiplier;
    public int recruitTimerSeconds;
    public int startingGold;
    public int goldPerTurn;
    public int maxGold;
    public Dictionary<string, int> tavernUpgradeCosts;
    public int startingHealth;
}

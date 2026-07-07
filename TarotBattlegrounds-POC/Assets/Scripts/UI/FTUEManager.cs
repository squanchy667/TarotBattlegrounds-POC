using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// T731: First-time user experience (FTUE) coach tips.
///
/// A self-contained onboarding layer for solo human matches. On the first Game-scene
/// load it auto-instantiates, builds its own toast UI in code, and shows a single
/// contextual tip the first time the player meets each core mechanic (shop, buying,
/// upgrading, combat, triples). Each tip is gated by PlayerPrefs so it fires exactly
/// once, ever; once all are seen the manager stops spawning.
///
/// Deliberately zero-touch: no scene objects, no prefabs, and no changes to existing
/// gameplay classes — it only reads public state (plus one Player event), so it cannot
/// destabilise the core loop. Skipped entirely in AIvsAI mode (e.g. PlayMode tests).
/// </summary>
public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance { get; private set; }

    private const string PrefPrefix = "FTUE_";
    private enum Tip { Shop, Buy, Upgrade, Combat, Triple }
    private static readonly Tip[] AllTips = { Tip.Shop, Tip.Buy, Tip.Upgrade, Tip.Combat, Tip.Triple };

    // --- Bootstrap ------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // guard against duplicate hooks (domain-reload disabled)
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Cover booting directly into the Game scene (sceneLoaded won't fire for it).
        if (SceneManager.GetActiveScene().name == "Game") TrySpawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game") TrySpawn();
    }

    private static void TrySpawn()
    {
        if (Instance != null) return;
        if (GameConfig.CurrentGameMode == GameConfig.GameMode.AIvsAI) return; // onboarding is for human play only
        if (AllSeen()) return;
        new GameObject("FTUEManager").AddComponent<FTUEManager>();
    }

    private static bool AllSeen()
    {
        foreach (var t in AllTips)
            if (!Seen(t)) return false;
        return true;
    }

    private static bool Seen(Tip t) => PlayerPrefs.GetInt(PrefPrefix + t, 0) == 1;
    private static void MarkSeen(Tip t) { PlayerPrefs.SetInt(PrefPrefix + t, 1); PlayerPrefs.Save(); }

    /// <summary>Clear all FTUE flags so every tip shows again (reset-tutorial / testing).</summary>
    public static void ResetAll()
    {
        foreach (var t in AllTips) PlayerPrefs.DeleteKey(PrefPrefix + t);
        PlayerPrefs.Save();
    }

    // --- State ----------------------------------------------------------------

    private Player human;
    private bool tripleWired;
    private readonly Queue<Tip> pending = new Queue<Tip>();
    private bool showing;
    private Tip current;

    private CanvasGroup toastGroup;
    private TMP_Text toastLabel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildToastUI();
        HideToast();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (human != null) human.OnTripleDiscovery -= OnTriple;
    }

    private void Update()
    {
        // Acquire the human player once the match has initialised its players.
        if (human == null)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.players == null) return;
            int idx = GameConfig.HumanPlayerIndex;
            if (idx < 0 || idx >= gm.players.Count) return;
            human = gm.players[idx];
        }
        if (!tripleWired)
        {
            human.OnTripleDiscovery += OnTriple;
            tripleWired = true;
        }

        DetectTriggers();
    }

    private void DetectTriggers()
    {
        var gm = GameManager.Instance;
        if (gm == null || human == null) return;

        // Combat: first time the match enters the combat phase.
        if (Fresh(Tip.Combat) && gm.CurrentPhase == GameManager.GamePhase.Combat)
            Enqueue(Tip.Combat);

        // Shop: shop populated, but the player hasn't acquired a card yet.
        if (Fresh(Tip.Shop) && ShopCount() > 0 && (human.hand.Count + human.board.Count) == 0)
            Enqueue(Tip.Shop);

        // Buy: the player now holds a bought card in hand.
        if (Fresh(Tip.Buy) && human.hand.Count > 0)
            Enqueue(Tip.Buy);

        // Upgrade: the player can afford a tavern upgrade.
        if (Fresh(Tip.Upgrade) && human.currentTavernTier < 6 && human.coins >= human.GetUpgradeCost())
            Enqueue(Tip.Upgrade);

        // Triple: handled via the OnTripleDiscovery event (see OnTriple).
    }

    private int ShopCount()
    {
        var tm = TavernManager.Instance;
        if (tm == null || tm.availableCards == null) return 0;
        return tm.availableCards.TryGetValue(human.playerId, out var list) && list != null ? list.Count : 0;
    }

    private void OnTriple(Player p, List<Card> cards)
    {
        if (Fresh(Tip.Triple)) Enqueue(Tip.Triple);
    }

    /// <summary>A tip is "fresh" if it has never been seen and isn't already shown/queued.</summary>
    private bool Fresh(Tip t)
    {
        if (Seen(t)) return false;
        if (showing && current == t) return false;
        foreach (var q in pending) if (q == t) return false;
        return true;
    }

    private void Enqueue(Tip t)
    {
        pending.Enqueue(t);
        if (!showing) ShowNext();
    }

    private void ShowNext()
    {
        if (pending.Count == 0) { showing = false; HideToast(); return; }
        current = pending.Dequeue();
        showing = true;
        if (toastLabel != null) toastLabel.text = TextFor(current);
        ShowToast();
    }

    /// <summary>Dismiss the current tip ("Got it") and advance to the next queued one.</summary>
    public void Dismiss()
    {
        if (showing) MarkSeen(current);
        showing = false;
        ShowNext();
    }

    private string TextFor(Tip t)
    {
        switch (t)
        {
            case Tip.Shop:    return $"Tap a card in the shop to buy it — you have {human.coins} coins.";
            case Tip.Buy:     return "Nice! Now drag the card from your hand onto your board to play it.";
            case Tip.Upgrade: return "You can afford a Tavern upgrade — it unlocks stronger cards.";
            case Tip.Combat:  return "Combat is automatic — your board fights an opponent's. Watch how you do!";
            case Tip.Triple:  return "Three of a kind! You forged a Golden, upgraded minion.";
            default:          return "";
        }
    }

    // --- UI (code-built; no scene assets or prefabs) --------------------------

    private void BuildToastUI()
    {
        if (EventSystem.current == null)
        {
            var es = new GameObject("FTUE_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }

        var canvasGo = new GameObject("FTUECanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var panel = new GameObject("FTUEToast", typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -80f);
        panelRect.sizeDelta = new Vector2(820f, 156f);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.94f);
        toastGroup = panel.GetComponent<CanvasGroup>();

        var labelGo = new GameObject("Label", typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panel.transform, false);
        toastLabel = labelGo.GetComponent<TextMeshProUGUI>();
        toastLabel.alignment = TextAlignmentOptions.Center;
        toastLabel.enableWordWrapping = true;
        toastLabel.fontSize = 30f;
        toastLabel.color = Color.white;
        var labelRect = toastLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.32f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(24f, 4f);
        labelRect.offsetMax = new Vector2(-24f, -10f);

        var btnGo = new GameObject("GotItButton", typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panel.transform, false);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 12f);
        btnRect.sizeDelta = new Vector2(180f, 44f);
        btnGo.GetComponent<Image>().color = new Color(0.35f, 0.78f, 0.42f, 1f);
        btnGo.GetComponent<Button>().onClick.AddListener(Dismiss);

        var btnLabelGo = new GameObject("Text", typeof(TextMeshProUGUI));
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        var btnLabel = btnLabelGo.GetComponent<TextMeshProUGUI>();
        btnLabel.text = "Got it";
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.fontSize = 24f;
        btnLabel.color = Color.white;
        var btnLabelRect = btnLabel.rectTransform;
        btnLabelRect.anchorMin = Vector2.zero; btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero; btnLabelRect.offsetMax = Vector2.zero;
    }

    private void ShowToast()
    {
        if (toastGroup == null) return;
        toastGroup.gameObject.SetActive(true);
        toastGroup.alpha = 1f;
        toastGroup.blocksRaycasts = true;
    }

    private void HideToast()
    {
        if (toastGroup == null) return;
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastGroup.gameObject.SetActive(false);
    }
}

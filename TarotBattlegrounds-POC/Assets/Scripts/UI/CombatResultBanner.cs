using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TarotBattlegrounds.UI;

/// <summary>
/// Testability HUD: large post-battle result banner so players always know
/// who won, damage to face, and that the shop board is not the combat board.
/// Singleton-friendly; safe if missing from scene.
/// </summary>
public class CombatResultBanner : MonoBehaviour
{
    public static CombatResultBanner Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text hintText;

    [Header("Timing")]
    [SerializeField] private float visibleSeconds = 3.5f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (root != null)
            root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Show a local-player battle result. Call after damage is applied.
    /// </summary>
    public void ShowLocalBattleResult(
        int localIndex, int opponentIndex,
        int winnerIndex, int damage,
        int localHpAfter, int opponentHpAfter,
        int localBoardCount, int opponentBoardCount)
    {
        bool localWon = winnerIndex == localIndex;
        bool tie = winnerIndex < 0;
        string title;
        Color bg;
        if (tie)
        {
            title = "TIE — both took damage";
            bg = Tokens.WithAlpha(Tokens.CharredWood, 0.92f);
        }
        else if (localWon)
        {
            title = "YOU WON THIS FIGHT";
            bg = Tokens.WithAlpha(Tokens.Bronze, 0.92f);
        }
        else
        {
            title = "YOU LOST THIS FIGHT";
            bg = Tokens.WithAlpha(Tokens.BloodDeep, 0.92f);
        }

        string detail =
            $"You (P{localIndex + 1}) vs P{opponentIndex + 1}\n" +
            $"Face damage: {damage}  ·  Your HP now: {localHpAfter}  ·  Their HP: {opponentHpAfter}\n" +
            $"Combat boards (clones): you had {localBoardCount} · they had {opponentBoardCount}";

        string hint =
            "Shop board stays as you built it — combat uses copies. " +
            "Living minions after a loss is normal here; only hero HP drops.";

        Show(title, detail, hint, bg);
    }

    /// <summary>Brief notice when local player is not in a pairing this round.</summary>
    public void ShowSpectating(string line)
    {
        Show("OTHER BATTLES", line, "Your fight plays when you are paired.", Tokens.WithAlpha(Tokens.Umber, 0.9f));
    }

    public void Show(string title, string detail, string hint, Color bgColor)
    {
        EnsureBuilt();
        if (background != null) background.color = bgColor;
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = Tokens.BoneBright;
        }
        if (detailText != null)
        {
            detailText.text = detail ?? "";
            detailText.color = Tokens.Bone;
        }
        if (hintText != null)
        {
            hintText.text = hint ?? "";
            hintText.color = Tokens.BoneDim;
        }
        if (root != null) root.SetActive(true);
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(visibleSeconds);
        Hide();
    }

    /// <summary>
    /// Immediately hide the banner (T840: game-over teardown must clear combat result chrome).
    /// </summary>
    public void Hide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
        if (root != null)
            root.SetActive(false);
    }

    /// <summary>Runtime-build minimal hierarchy if editor setup was not run.</summary>
    public void EnsureBuilt()
    {
        if (root != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        root = new GameObject("CombatResultBanner");
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.08f, 0.72f);
        rt.anchorMax = new Vector2(0.92f, 0.96f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        background = root.AddComponent<Image>();
        background.color = Tokens.WithAlpha(Tokens.CharredWood, 0.92f);
        background.raycastTarget = false;

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 16, 16);
        vlg.spacing = 6f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        titleText = CreateLine(root.transform, "Title", Tokens.TextH2, true);
        detailText = CreateLine(root.transform, "Detail", Tokens.TextBody, false);
        hintText = CreateLine(root.transform, "Hint", Tokens.TextCaption, false);
        root.SetActive(false);
    }

    private static TMP_Text CreateLine(Transform parent, string name, float size, bool bold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        if (FontRefs.Instance != null)
        {
            if (bold && FontRefs.Instance.Display != null) tmp.font = FontRefs.Instance.Display;
            else if (FontRefs.Instance.Body != null) tmp.font = FontRefs.Instance.Body;
        }
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = size + 8f;
        le.preferredHeight = size * (bold ? 1.4f : 2.2f);
        return tmp;
    }

    /// <summary>Find or create on canvas for runtime safety.</summary>
    public static CombatResultBanner EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.EnsureBuilt();
            return Instance;
        }
        var existing = FindObjectOfType<CombatResultBanner>(true);
        if (existing != null)
        {
            existing.EnsureBuilt();
            return existing;
        }
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return null;
        GameObject go = new GameObject("CombatResultBannerHost");
        go.transform.SetParent(canvas.transform, false);
        var banner = go.AddComponent<CombatResultBanner>();
        banner.EnsureBuilt();
        return banner;
    }
}

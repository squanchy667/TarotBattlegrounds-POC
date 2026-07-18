using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

/// <summary>
/// Compact side panel showing tribe synergy progress with filled/unfilled pip indicators
/// for the 2/4/6 thresholds. Active synergies glow, inactive ones are dim.
/// Theme-aware via IThemeable.
/// </summary>
public class SynergyDisplayPanel : MonoBehaviour, IThemeable
{
    [Header("Panel")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image panelBackground;
    [SerializeField] private Transform rowContainer;

    [Header("Row Prefab References")]
    [SerializeField] private GameObject synergyRowPrefab;

    [Header("Colors")]
    [SerializeField] private Color activeGlow = Tokens.WithAlpha(Tokens.Ember, 0.15f);
    [SerializeField] private Color inactiveDim = Tokens.WithAlpha(Tokens.BoneDim, 0.5f);
    [SerializeField] private Color pipFilled = Tokens.BoneBright;
    [SerializeField] private Color pipEmpty = Tokens.BoneDim;

    [Header("Settings")]
    [SerializeField] private float rowHeight = 28f;
    [SerializeField] private float pipSize = 10f;
    [SerializeField] private float iconSize = 20f;

    private Dictionary<TribeType, SynergyRow> rows = new Dictionary<TribeType, SynergyRow>();

    // Cached procedural textures (shared across all instances)
    private static Texture2D _cachedPipTexture;
    private static Dictionary<TribeType, Texture2D> _cachedTribeIcons = new Dictionary<TribeType, Texture2D>();

    private class SynergyRow
    {
        public Image tribeIcon;
        public Image[] pips = new Image[3]; // 2, 4, 6 thresholds
        public TMP_Text countText;
        public Image rowBackground;
        public Image glowOverlay;
        public TribeType tribe;
        public int currentCount;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        EnsureInitialized();
        // Pull board counts if a match is already running
        if (GameUIManager.Instance != null)
        {
            var p = GameUIManager.Instance.GetActivePlayer();
            if (p != null) UpdateSynergies(p);
        }
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += ApplyTheme;
        if (ThemeManager.ActiveTheme != null)
            ApplyTheme(ThemeManager.ActiveTheme);
        EnsureInitialized();
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    /// <summary>
    /// Apply theme colors to this panel.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        // Update panel background from theme
        if (panelBackground != null)
        {
            panelBackground.color = new Color(
                theme.secondaryColor.r,
                theme.secondaryColor.g,
                theme.secondaryColor.b,
                0.75f
            );
        }

        // Clear tribe icon cache so they regenerate with new theme colors
        _cachedTribeIcons.Clear();

        // Refresh all rows with updated tribe colors
        foreach (var kvp in rows)
        {
            // Update tribe icon with new theme color
            if (kvp.Value.tribeIcon != null)
            {
                Texture2D iconTex = GenerateTribeIcon(kvp.Key, Mathf.RoundToInt(iconSize));
                kvp.Value.tribeIcon.sprite = TextureToSprite(iconTex);
            }

            // Re-render pips with new colors
            UpdateRow(kvp.Value, kvp.Value.currentCount);
        }
    }

    /// <summary>
    /// Rebuild rows if the runtime dictionary is empty.
    /// Editor setup creates children in the scene, but <see cref="rows"/> is not serialized —
    /// without this, UpdateSynergies is a no-op and the panel looks broken.
    /// </summary>
    public void EnsureInitialized()
    {
        if (rowContainer == null)
        {
            // Common child name from SynergyDisplaySetup
            var t = transform.Find("RowContainer");
            if (t != null) rowContainer = t;
        }
        if (rows.Count == 0)
            Initialize();
    }

    /// <summary>
    /// Initialize the panel with 6 tribe rows. Call once during setup.
    /// </summary>
    public void Initialize()
    {
        if (rowContainer == null)
        {
            Debug.LogWarning("[SynergyDisplayPanel] rowContainer is null, cannot initialize.");
            return;
        }

        // Clear existing rows (editor setup used Destroy which does nothing in edit mode)
        var toDestroy = new List<GameObject>();
        foreach (Transform child in rowContainer)
            toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        rows.Clear();

        // Create 6 rows (one per TribeType)
        TribeType[] tribeOrder = {
            TribeType.Pentacles, TribeType.Cups, TribeType.Swords,
            TribeType.Wands, TribeType.Stars, TribeType.Coins
        };

        foreach (TribeType tribe in tribeOrder)
        {
            CreateRow(tribe);
        }
    }

    /// <summary>
    /// Create a single synergy row for a tribe.
    /// </summary>
    private void CreateRow(TribeType tribe)
    {
        SynergyRow row = new SynergyRow();
        row.tribe = tribe;
        row.currentCount = 0;

        Color tribeColor = ThemeManager.GetTribeColor(tribe);

        // Row root
        GameObject rowObj = new GameObject($"SynergyRow_{tribe}");
        rowObj.transform.SetParent(rowContainer, false);
        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, rowHeight);

        // Row background
        row.rowBackground = rowObj.AddComponent<Image>();
        row.rowBackground.color = Color.clear;
        row.rowBackground.raycastTarget = false;

        // Horizontal layout
        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Glow overlay (behind content, stretches to fill row)
        GameObject glowObj = new GameObject("GlowOverlay");
        glowObj.transform.SetParent(rowObj.transform, false);
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;
        row.glowOverlay = glowObj.AddComponent<Image>();
        row.glowOverlay.color = new Color(tribeColor.r, tribeColor.g, tribeColor.b, 0.15f);
        row.glowOverlay.raycastTarget = false;
        row.glowOverlay.gameObject.SetActive(false);

        // Exclude glow from layout
        LayoutElement glowLE = glowObj.AddComponent<LayoutElement>();
        glowLE.ignoreLayout = true;

        // Tribe icon
        GameObject iconObj = new GameObject("TribeIcon");
        iconObj.transform.SetParent(rowObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        row.tribeIcon = iconObj.AddComponent<Image>();
        Texture2D iconTex = GenerateTribeIcon(tribe, Mathf.RoundToInt(iconSize));
        row.tribeIcon.sprite = TextureToSprite(iconTex);
        row.tribeIcon.raycastTarget = false;
        LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.preferredWidth = iconSize;
        iconLE.preferredHeight = iconSize;

        // 3 pips (thresholds 2, 4, 6)
        Texture2D pipTex = GeneratePipTexture(Mathf.RoundToInt(pipSize));
        for (int i = 0; i < 3; i++)
        {
            GameObject pipObj = new GameObject($"Pip_{i}");
            pipObj.transform.SetParent(rowObj.transform, false);
            RectTransform pipRect = pipObj.AddComponent<RectTransform>();
            pipRect.sizeDelta = new Vector2(pipSize, pipSize);
            row.pips[i] = pipObj.AddComponent<Image>();
            row.pips[i].sprite = TextureToSprite(pipTex);
            row.pips[i].color = pipEmpty;
            row.pips[i].raycastTarget = false;
            LayoutElement pipLE = pipObj.AddComponent<LayoutElement>();
            pipLE.preferredWidth = pipSize;
            pipLE.preferredHeight = pipSize;
        }

        // Count text
        GameObject textObj = new GameObject("CountText");
        textObj.transform.SetParent(rowObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(36f, rowHeight);
        row.countText = textObj.AddComponent<TextMeshProUGUI>();
        row.countText.text = "0";
        row.countText.fontSize = Tokens.TextCaption;
        row.countText.color = Tokens.BoneDim;
        row.countText.alignment = TextAlignmentOptions.MidlineLeft;
        row.countText.enableWordWrapping = false;
        row.countText.raycastTarget = false;
        LayoutElement textLE = textObj.AddComponent<LayoutElement>();
        textLE.preferredWidth = 36f;
        textLE.preferredHeight = rowHeight;

        rows[tribe] = row;

        // Initialize as inactive
        UpdateRow(row, 0);
    }

    /// <summary>
    /// Update synergies based on a player's board state.
    /// </summary>
    public void UpdateSynergies(Player player)
    {
        if (player == null) return;
        EnsureInitialized();
        if (rows.Count == 0) return;

        // Count tribes on player's board
        var tribeCounts = new Dictionary<TribeType, int>();
        foreach (var card in player.board)
        {
            if (card == null) continue;

            // Prefer GetPrimaryTribe / GetTribes; also fall back to legacy string tribe
            TribeType[] cardTribes = card.GetTribes();
            bool any = false;
            if (cardTribes != null)
            {
                foreach (var tribe in cardTribes)
                {
                    if (tribe == TribeType.None) continue;
                    any = true;
                    if (!tribeCounts.ContainsKey(tribe)) tribeCounts[tribe] = 0;
                    tribeCounts[tribe]++;
                }
            }
            if (!any)
            {
                TribeType primary = card.GetPrimaryTribe();
                if (primary != TribeType.None)
                {
                    if (!tribeCounts.ContainsKey(primary)) tribeCounts[primary] = 0;
                    tribeCounts[primary]++;
                }
            }
        }

        // Update each row
        foreach (var kvp in rows)
        {
            int count = tribeCounts.ContainsKey(kvp.Key) ? tribeCounts[kvp.Key] : 0;
            UpdateRow(kvp.Value, count);
        }
    }

    /// <summary>
    /// Update a single row's visual state based on tribe count.
    /// </summary>
    private void UpdateRow(SynergyRow row, int count)
    {
        row.currentCount = count;

        if (row.countText != null)
            row.countText.text = $"{count}";

        // Update pips: threshold 2, 4, 6
        int[] thresholds = { 2, 4, 6 };
        for (int i = 0; i < 3; i++)
        {
            if (row.pips[i] == null) continue;
            bool filled = count >= thresholds[i];
            Color tribeColor = ThemeManager.GetTribeColor(row.tribe);
            row.pips[i].color = filled ? tribeColor : pipEmpty;
        }

        // Active glow if any threshold met
        bool isActive = count >= 2;
        if (row.glowOverlay != null)
        {
            row.glowOverlay.gameObject.SetActive(isActive);
            if (isActive)
            {
                Color tribeColor = ThemeManager.GetTribeColor(row.tribe);
                row.glowOverlay.color = new Color(tribeColor.r, tribeColor.g, tribeColor.b, 0.15f);
            }
        }

        if (row.rowBackground != null)
        {
            if (isActive)
            {
                Color tribeColor = ThemeManager.GetTribeColor(row.tribe);
                row.rowBackground.color = new Color(tribeColor.r, tribeColor.g, tribeColor.b, 0.1f);
            }
            else
            {
                row.rowBackground.color = Color.clear;
            }
        }

        // Dim inactive rows
        float alpha = isActive ? 1f : 0.5f;
        if (row.tribeIcon != null)
            row.tribeIcon.color = new Color(row.tribeIcon.color.r, row.tribeIcon.color.g, row.tribeIcon.color.b, alpha);
        if (row.countText != null)
            row.countText.alpha = alpha;
    }

    // ===================== PROCEDURAL TEXTURES =====================

    /// <summary>
    /// Generate a small circle texture for pips with soft edges.
    /// Cached as static to share across all rows.
    /// </summary>
    private static Texture2D GeneratePipTexture(int size)
    {
        if (_cachedPipTexture != null) return _cachedPipTexture;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // Soft edge using smoothstep
                float alpha = 1f - Mathf.Clamp01((dist - radius + 1f) / 1.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedPipTexture = tex;
        return tex;
    }

    /// <summary>
    /// Generate a tribe icon texture procedurally.
    /// Each tribe has a distinct shape.
    /// </summary>
    private static Texture2D GenerateTribeIcon(TribeType tribe, int size)
    {
        if (_cachedTribeIcons.TryGetValue(tribe, out Texture2D cached) && cached != null)
            return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color tribeColor = ThemeManager.GetTribeColor(tribe);
        Color[] pixels = new Color[size * size];
        float center = size * 0.5f;

        switch (tribe)
        {
            case TribeType.Pentacles:
                // Circle (coin-like)
                DrawCircle(pixels, size, center, center, center - 2f, tribeColor);
                break;

            case TribeType.Cups:
                // Inverted triangle (chalice)
                DrawInvertedTriangle(pixels, size, tribeColor);
                break;

            case TribeType.Swords:
                // Vertical line with cross (sword)
                DrawSword(pixels, size, tribeColor);
                break;

            case TribeType.Wands:
                // Vertical line (staff)
                DrawStaff(pixels, size, tribeColor);
                break;

            case TribeType.Stars:
                // Star shape (5 points)
                DrawStar(pixels, size, tribeColor);
                break;

            case TribeType.Coins:
                // Circle with inner ring
                DrawCoinRing(pixels, size, center, tribeColor);
                break;

            default:
                // Fallback: filled circle
                DrawCircle(pixels, size, center, center, center - 2f, tribeColor);
                break;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _cachedTribeIcons[tribe] = tex;
        return tex;
    }

    // ===================== SHAPE DRAWING HELPERS =====================

    private static void DrawCircle(Color[] pixels, int size, float cx, float cy, float radius, Color color)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx + 0.5f;
                float dy = y - cy + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.Clamp01((dist - radius + 1f) / 1.5f);
                if (alpha > 0f)
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha * color.a);
            }
        }
    }

    private static void DrawInvertedTriangle(Color[] pixels, int size, Color color)
    {
        float margin = size * 0.15f;
        // Triangle: top-left, top-right, bottom-center
        Vector2 a = new Vector2(margin, size - margin);
        Vector2 b = new Vector2(size - margin, size - margin);
        Vector2 c = new Vector2(size * 0.5f, margin);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                if (PointInTriangle(p, a, b, c))
                {
                    // Soft edge
                    float edgeDist = DistToTriangleEdge(p, a, b, c);
                    float alpha = Mathf.Clamp01(edgeDist / 1.5f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha * color.a);
                }
            }
        }
    }

    private static void DrawSword(Color[] pixels, int size, Color color)
    {
        float cx = size * 0.5f;
        float bladeWidth = size * 0.12f;
        float crossWidth = size * 0.35f;
        float crossHeight = size * 0.1f;
        float crossY = size * 0.65f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                bool isBlade = Mathf.Abs(px - cx) < bladeWidth && py > size * 0.1f && py < size * 0.9f;
                bool isCross = Mathf.Abs(px - cx) < crossWidth && Mathf.Abs(py - crossY) < crossHeight;

                if (isBlade || isCross)
                    pixels[y * size + x] = color;
            }
        }
    }

    private static void DrawStaff(Color[] pixels, int size, Color color)
    {
        float cx = size * 0.5f;
        float staffWidth = size * 0.12f;
        // Small orb at top
        float orbRadius = size * 0.15f;
        float orbY = size * 0.85f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                bool isStaff = Mathf.Abs(px - cx) < staffWidth && py > size * 0.1f && py < size * 0.8f;

                float dx = px - cx;
                float dy = py - orbY;
                bool isOrb = Mathf.Sqrt(dx * dx + dy * dy) < orbRadius;

                if (isStaff || isOrb)
                    pixels[y * size + x] = color;
            }
        }
    }

    private static void DrawStar(Color[] pixels, int size, Color color)
    {
        float cx = size * 0.5f;
        float cy = size * 0.5f;
        float outerR = size * 0.45f;
        float innerR = size * 0.18f;
        int points = 5;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x - cx + 0.5f;
                float py = y - cy + 0.5f;
                if (IsInStar(px, py, outerR, innerR, points))
                {
                    float dist = Mathf.Sqrt(px * px + py * py);
                    float alpha = Mathf.Clamp01(1f - (dist - outerR + 1.5f) / 1.5f);
                    alpha = Mathf.Max(alpha, IsInStar(px, py, outerR, innerR, points) ? 1f : 0f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a);
                }
            }
        }
    }

    private static bool IsInStar(float px, float py, float outerR, float innerR, int points)
    {
        float angle = Mathf.Atan2(py, px);
        float segAngle = Mathf.PI * 2f / points;
        float halfSeg = segAngle * 0.5f;

        // Normalize angle to segment
        float localAngle = Mathf.Repeat(angle + Mathf.PI * 0.5f, segAngle);
        float dist = Mathf.Sqrt(px * px + py * py);

        // Interpolate between inner and outer radius based on position in segment
        float t = Mathf.Abs(localAngle - halfSeg) / halfSeg;
        float threshold = Mathf.Lerp(outerR, innerR, t);

        return dist <= threshold;
    }

    private static void DrawCoinRing(Color[] pixels, int size, float center, Color color)
    {
        float outerR = center - 2f;
        float innerR = outerR * 0.55f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Outer circle
                float outerAlpha = 1f - Mathf.Clamp01((dist - outerR + 1f) / 1.5f);
                // Inner hole
                float innerAlpha = Mathf.Clamp01((dist - innerR + 1f) / 1.5f);

                float alpha = outerAlpha * innerAlpha;
                if (alpha > 0f)
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha * color.a);
            }
        }
    }

    // ===================== GEOMETRY HELPERS =====================

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static float DistToTriangleEdge(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = DistToSegment(p, a, b);
        float d2 = DistToSegment(p, b, c);
        float d3 = DistToSegment(p, c, a);
        return Mathf.Min(d1, Mathf.Min(d2, d3));
    }

    private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }

    /// <summary>
    /// Convert a Texture2D to a Sprite.
    /// </summary>
    private static Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}

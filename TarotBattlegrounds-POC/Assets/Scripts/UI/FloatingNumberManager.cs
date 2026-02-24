using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UX14: Singleton manager for floating combat numbers.
/// Uses an object pool of FloatingNumber instances to display
/// damage, heal, buff, aegis, and death text during combat.
/// </summary>
public class FloatingNumberManager : MonoBehaviour
{
    public static FloatingNumberManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private int poolSize = 20;

    [Header("Spawn Offset")]
    [Tooltip("Random horizontal offset range for overlapping numbers")]
    [SerializeField] private float horizontalJitter = 20f;
    [Tooltip("Vertical offset above card position")]
    [SerializeField] private float verticalOffset = 30f;

    private readonly Queue<FloatingNumber> pool = new Queue<FloatingNumber>();
    private RectTransform canvasRectTransform;
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePool();
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Pre-instantiate the object pool.
    /// </summary>
    private void InitializePool()
    {
        // Find or create parent canvas
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        if (parentCanvas == null)
        {
            // Create an overlay canvas for floating numbers
            GameObject canvasObj = new GameObject("FloatingNumberCanvas");
            canvasObj.transform.SetParent(transform);
            parentCanvas = canvasObj.AddComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parentCanvas.sortingOrder = 100; // Above other UI
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        }

        canvasRectTransform = parentCanvas.GetComponent<RectTransform>();

        for (int i = 0; i < poolSize; i++)
        {
            FloatingNumber fn = FloatingNumber.CreateFromCode(parentCanvas.transform);
            fn.OnReturnToPool = ReturnToPool;
            pool.Enqueue(fn);
        }
    }

    /// <summary>
    /// Get a FloatingNumber from the pool. If empty, create a new one.
    /// </summary>
    private FloatingNumber GetFromPool()
    {
        FloatingNumber fn;

        if (pool.Count > 0)
        {
            fn = pool.Dequeue();
        }
        else
        {
            // Pool exhausted — create a new instance
            fn = FloatingNumber.CreateFromCode(parentCanvas.transform);
            fn.OnReturnToPool = ReturnToPool;
        }

        return fn;
    }

    /// <summary>
    /// Return a FloatingNumber to the pool.
    /// </summary>
    private void ReturnToPool(FloatingNumber fn)
    {
        if (fn == null) return;
        fn.gameObject.SetActive(false);
        pool.Enqueue(fn);
    }

    /// <summary>
    /// Convert a world position to screen-space anchored position on the canvas.
    /// </summary>
    private Vector2 WorldToCanvasPosition(Vector3 worldPos)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector2 screenPos;

        if (mainCamera != null)
        {
            screenPos = mainCamera.WorldToScreenPoint(worldPos);
        }
        else
        {
            // Fallback: treat world pos as screen pos (overlay UI)
            screenPos = new Vector2(worldPos.x, worldPos.y);
        }

        // Convert screen position to canvas local position
        if (canvasRectTransform != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform, screenPos, parentCanvas.worldCamera, out Vector2 localPoint))
        {
            return localPoint;
        }

        return screenPos;
    }

    /// <summary>
    /// Apply jitter and offset to prevent overlapping numbers.
    /// </summary>
    private Vector2 ApplyOffset(Vector2 basePos)
    {
        float jitterX = Random.Range(-horizontalJitter, horizontalJitter);
        return new Vector2(basePos.x + jitterX, basePos.y + verticalOffset);
    }

    /// <summary>
    /// Show floating damage number. Red, "-N" format.
    /// </summary>
    /// <param name="worldPos">World position of the damaged card</param>
    /// <param name="amount">Damage amount (positive integer)</param>
    public void ShowDamage(Vector3 worldPos, int amount)
    {
        if (amount <= 0) return;

        FloatingNumber fn = GetFromPool();
        Vector2 canvasPos = ApplyOffset(WorldToCanvasPosition(worldPos));
        fn.Show($"-{amount}", FloatingNumber.DamageColor, canvasPos);
    }

    /// <summary>
    /// Show floating heal number. Green, "+N" format.
    /// </summary>
    /// <param name="worldPos">World position of the healed card</param>
    /// <param name="amount">Heal amount (positive integer)</param>
    public void ShowHeal(Vector3 worldPos, int amount)
    {
        if (amount <= 0) return;

        FloatingNumber fn = GetFromPool();
        Vector2 canvasPos = ApplyOffset(WorldToCanvasPosition(worldPos));
        fn.Show($"+{amount}", FloatingNumber.HealColor, canvasPos);
    }

    /// <summary>
    /// Show floating buff text. Gold, custom text format (e.g., "+2 ATK", "+1/+1").
    /// </summary>
    /// <param name="worldPos">World position of the buffed card</param>
    /// <param name="text">Buff description text</param>
    public void ShowBuff(Vector3 worldPos, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        FloatingNumber fn = GetFromPool();
        Vector2 canvasPos = ApplyOffset(WorldToCanvasPosition(worldPos));
        fn.Show(text, FloatingNumber.BuffColor, canvasPos);
    }

    /// <summary>
    /// Show "AEGIS!" floating text when aegis shield pops. Cyan colored.
    /// </summary>
    /// <param name="worldPos">World position of the card whose aegis popped</param>
    public void ShowAegisPop(Vector3 worldPos)
    {
        FloatingNumber fn = GetFromPool();
        Vector2 canvasPos = ApplyOffset(WorldToCanvasPosition(worldPos));
        fn.Show("AEGIS!", FloatingNumber.AegisColor, canvasPos);
    }

    /// <summary>
    /// Show "DEAD" floating text when a card dies. Dark red colored.
    /// </summary>
    /// <param name="worldPos">World position of the dead card</param>
    public void ShowDeath(Vector3 worldPos)
    {
        FloatingNumber fn = GetFromPool();
        Vector2 canvasPos = ApplyOffset(WorldToCanvasPosition(worldPos));
        fn.Show("DEAD", FloatingNumber.DeathColor, canvasPos);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

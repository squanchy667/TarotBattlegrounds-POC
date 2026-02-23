using UnityEngine;

/// <summary>
/// ScriptableObject holding the base URL for runtime game data.
/// Create via Assets > Create > Game > Data Config.
/// </summary>
[CreateAssetMenu(fileName = "DataConfig", menuName = "Game/Data Config")]
public class DataConfig : ScriptableObject
{
    [Header("Data Source")]
    [Tooltip("Base URL for runtime JSON data (must end with /)")]
    public string dataBaseUrl = "https://tarot-battlegrounds-data-prod.s3.amazonaws.com/live/";

    [Tooltip("Whether to attempt loading runtime data on startup")]
    public bool enableRuntimeLoading = true;

    [Tooltip("Timeout in seconds for each data fetch request")]
    public float requestTimeout = 10f;

    [Header("API")]
    [Tooltip("Base URL for the DevZone API (API Gateway endpoint, must end with /api/)")]
    public string apiBaseUrl = "";

    [Header("Fallback")]
    [Tooltip("If true, use built-in CardDatabase data when runtime loading fails")]
    public bool fallbackToBuiltIn = true;
}

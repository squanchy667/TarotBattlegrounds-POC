using UnityEngine;
using System.Collections.Generic;

namespace TarotBattlegrounds.Combat.Audio
{
    /// <summary>
    /// T312: SFX Manager with AudioSource pooling.
    /// Handles one-shot sound effects for combat events.
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 8;

        [Header("Config")]
        [SerializeField] private SFXConfig sfxConfig;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

        private Queue<AudioSource> availableSources = new Queue<AudioSource>();
        private List<AudioSource> activeSources = new List<AudioSource>();
        private Transform poolRoot;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // T701: SFXConfig isn't scene/prefab-assigned when SFXManager is created by
            // AudioBootstrap at runtime — fall back to Resources so PlaySFX has clips to
            // look up once audio assets are added (T723).
            if (sfxConfig == null)
                sfxConfig = Resources.Load<SFXConfig>("SFXConfig");

            poolRoot = new GameObject("SFX_Pool").transform;
            poolRoot.SetParent(transform);

            // Pre-create audio sources
            for (int i = 0; i < poolSize; i++)
            {
                var source = CreateAudioSource();
                availableSources.Enqueue(source);
            }
        }

        /// <summary>
        /// T314: Play a combat SFX event.
        /// </summary>
        public void PlaySFX(SFXEvent sfxEvent, float volumeMultiplier = 1f)
        {
            if (sfxConfig == null)
            {
                Debug.Log($"[SFXManager] No SFXConfig assigned, skipping {sfxEvent}");
                return;
            }

            AudioClip clip = sfxConfig.GetClip(sfxEvent);
            if (clip == null)
            {
                Debug.Log($"[SFXManager] No clip for {sfxEvent}");
                return;
            }

            PlayClip(clip, masterVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play a specific audio clip.
        /// </summary>
        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetSource();
            if (source == null)
            {
                Debug.LogWarning("[SFXManager] No available audio sources");
                return;
            }

            source.clip = clip;
            source.volume = volume;
            source.pitch = 1f + Random.Range(-0.05f, 0.05f); // Slight variation
            source.Play();
            activeSources.Add(source);
        }

        /// <summary>
        /// Play SFX with positional pitch shift (for variety).
        /// </summary>
        public void PlaySFXWithPitch(SFXEvent sfxEvent, float pitchShift)
        {
            if (sfxConfig == null) return;
            AudioClip clip = sfxConfig.GetClip(sfxEvent);
            if (clip == null) return;

            AudioSource source = GetSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = masterVolume;
            source.pitch = 1f + pitchShift;
            source.Play();
            activeSources.Add(source);
        }

        private AudioSource GetSource()
        {
            // Return available source from pool
            while (availableSources.Count > 0)
            {
                var source = availableSources.Dequeue();
                if (source != null)
                {
                    source.gameObject.SetActive(true);
                    return source;
                }
            }

            // Create new if under limit
            if (activeSources.Count < poolSize * 2)
            {
                return CreateAudioSource();
            }

            return null;
        }

        private AudioSource CreateAudioSource()
        {
            GameObject obj = new GameObject("SFX_Source");
            obj.transform.SetParent(poolRoot);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound
            return source;
        }

        private void Update()
        {
            // Return completed sources to pool
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                var source = activeSources[i];
                if (source == null)
                {
                    activeSources.RemoveAt(i);
                    continue;
                }
                if (!source.isPlaying)
                {
                    activeSources.RemoveAt(i);
                    source.gameObject.SetActive(false);
                    availableSources.Enqueue(source);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    /// <summary>
    /// T314: SFX event types for combat.
    /// </summary>
    public enum SFXEvent
    {
        // Combat
        AttackSwing,
        AttackImpact,
        CounterattackHit,
        CardDeath,
        AegisPop,
        // Abilities
        Battlecry,
        Deathrattle,
        Venomous,
        Windfury,
        Reborn,
        BuffApplied,
        SummonToken,
        // UI
        ButtonClick,
        CardDraw,
        CardPlay,
        CoinGain,
        TierUp,
        // Music transitions
        CombatStart,
        CombatEnd,
        Victory,
        Defeat
    }
}

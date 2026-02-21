using UnityEngine;
using System.Collections;

namespace TarotBattlegrounds.Combat.Audio
{
    /// <summary>
    /// T315: Music Manager with crossfade between tracks.
    /// Supports menu, recruit, and combat music phases.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Music Tracks")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip recruitMusic;
        [SerializeField] private AudioClip combatMusic;
        [SerializeField] private AudioClip victoryStinger;
        [SerializeField] private AudioClip defeatStinger;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField] private float crossfadeDuration = 1.5f;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private Coroutine crossfadeCoroutine;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (activeSource != null)
                    activeSource.volume = musicVolume;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            sourceA = gameObject.AddComponent<AudioSource>();
            sourceA.playOnAwake = false;
            sourceA.loop = true;
            sourceA.spatialBlend = 0f;

            sourceB = gameObject.AddComponent<AudioSource>();
            sourceB.playOnAwake = false;
            sourceB.loop = true;
            sourceB.spatialBlend = 0f;

            activeSource = sourceA;
        }

        /// <summary>
        /// Start playing menu music.
        /// </summary>
        public void PlayMenuMusic()
        {
            CrossfadeTo(menuMusic);
        }

        /// <summary>
        /// Start playing recruit phase music.
        /// </summary>
        public void PlayRecruitMusic()
        {
            CrossfadeTo(recruitMusic);
        }

        /// <summary>
        /// Start playing combat music.
        /// </summary>
        public void PlayCombatMusic()
        {
            CrossfadeTo(combatMusic);
        }

        /// <summary>
        /// Play a one-shot stinger (victory/defeat) then resume previous track.
        /// </summary>
        public void PlayVictoryStinger()
        {
            if (victoryStinger != null)
                PlayOneShot(victoryStinger);
        }

        /// <summary>
        /// Play a one-shot stinger (victory/defeat) then resume previous track.
        /// </summary>
        public void PlayDefeatStinger()
        {
            if (defeatStinger != null)
                PlayOneShot(defeatStinger);
        }

        /// <summary>
        /// Stop all music with fade out.
        /// </summary>
        public void StopMusic()
        {
            if (crossfadeCoroutine != null)
                StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = StartCoroutine(FadeOut(activeSource, crossfadeDuration));
        }

        /// <summary>
        /// Crossfade from current track to a new track.
        /// </summary>
        private void CrossfadeTo(AudioClip newClip)
        {
            if (newClip == null) return;

            // Skip if already playing this clip
            if (activeSource != null && activeSource.clip == newClip && activeSource.isPlaying)
                return;

            if (crossfadeCoroutine != null)
                StopCoroutine(crossfadeCoroutine);

            AudioSource incoming = activeSource == sourceA ? sourceB : sourceA;
            AudioSource outgoing = activeSource;

            incoming.clip = newClip;
            incoming.volume = 0f;
            incoming.Play();

            crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(outgoing, incoming));
            activeSource = incoming;
        }

        private IEnumerator CrossfadeCoroutine(AudioSource outgoing, AudioSource incoming)
        {
            float elapsed = 0f;
            float startVolume = outgoing.isPlaying ? outgoing.volume : 0f;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;

                if (outgoing.isPlaying)
                    outgoing.volume = Mathf.Lerp(startVolume, 0f, t);
                incoming.volume = Mathf.Lerp(0f, musicVolume, t);

                yield return null;
            }

            if (outgoing.isPlaying)
            {
                outgoing.Stop();
                outgoing.volume = 0f;
            }
            incoming.volume = musicVolume;

            crossfadeCoroutine = null;
        }

        private IEnumerator FadeOut(AudioSource source, float duration)
        {
            if (source == null || !source.isPlaying) yield break;

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
        }

        private void PlayOneShot(AudioClip clip)
        {
            // Duck the current music
            if (activeSource != null && activeSource.isPlaying)
                activeSource.volume = musicVolume * 0.3f;

            // Play stinger on a temporary source
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, musicVolume);

            // Restore music volume after stinger
            StartCoroutine(RestoreVolumeAfterDelay(clip.length));
        }

        private IEnumerator RestoreVolumeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (activeSource != null && activeSource.isPlaying)
                activeSource.volume = musicVolume;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

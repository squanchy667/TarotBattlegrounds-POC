using UnityEngine;
using System.Collections.Generic;

namespace TarotBattlegrounds.Combat.VFX
{
    /// <summary>
    /// T308: VFX Manager with particle system pooling.
    /// Manages creation, pooling, and playback of particle effects for combat.
    /// Creates procedural particle systems at runtime (no prefabs needed).
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 30;

        [Header("VFX Config")]
        [SerializeField] private VFXConfig config;

        private Dictionary<VFXType, Queue<ParticleSystem>> pools = new Dictionary<VFXType, Queue<ParticleSystem>>();
        private List<ParticleSystem> activeEffects = new List<ParticleSystem>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            poolRoot = new GameObject("VFX_Pool").transform;
            poolRoot.SetParent(transform);

            // Pre-warm pools for common effect types
            PrewarmPool(VFXType.AttackSwoosh, initialPoolSize / 2);
            PrewarmPool(VFXType.ImpactBurst, initialPoolSize / 2);
            PrewarmPool(VFXType.DeathSoulRelease, initialPoolSize / 4);
        }

        /// <summary>
        /// T309: Play attack swoosh VFX from source to target.
        /// </summary>
        public void PlayAttackVFX(Vector3 sourcePos, Vector3 targetPos)
        {
            var swoosh = GetFromPool(VFXType.AttackSwoosh);
            if (swoosh != null)
            {
                swoosh.transform.position = sourcePos;
                swoosh.transform.LookAt(targetPos);
                var cfg = GetConfig(VFXType.AttackSwoosh);
                ApplyConfig(swoosh, cfg);
                swoosh.Play();
            }

            var impact = GetFromPool(VFXType.ImpactBurst);
            if (impact != null)
            {
                impact.transform.position = targetPos;
                var cfg = GetConfig(VFXType.ImpactBurst);
                ApplyConfig(impact, cfg);
                impact.Play();
            }
        }

        /// <summary>
        /// T310: Play ability trigger VFX at a position.
        /// </summary>
        public void PlayAbilityVFX(Vector3 position, string abilityName)
        {
            VFXType type = MapAbilityToVFX(abilityName);
            var ps = GetFromPool(type);
            if (ps != null)
            {
                ps.transform.position = position;
                var cfg = GetConfig(type);
                ApplyConfig(ps, cfg);
                ps.Play();
            }
        }

        /// <summary>
        /// T311: Play death VFX at a position.
        /// </summary>
        public void PlayDeathVFX(Vector3 position)
        {
            var soul = GetFromPool(VFXType.DeathSoulRelease);
            if (soul != null)
            {
                soul.transform.position = position;
                var cfg = GetConfig(VFXType.DeathSoulRelease);
                ApplyConfig(soul, cfg);
                soul.Play();
            }

            var shatter = GetFromPool(VFXType.DeathCardShatter);
            if (shatter != null)
            {
                shatter.transform.position = position;
                var cfg = GetConfig(VFXType.DeathCardShatter);
                ApplyConfig(shatter, cfg);
                shatter.Play();
            }
        }

        /// <summary>
        /// Play buff VFX at a position.
        /// </summary>
        public void PlayBuffVFX(Vector3 position)
        {
            var ps = GetFromPool(VFXType.BuffGlow);
            if (ps != null)
            {
                ps.transform.position = position;
                var cfg = GetConfig(VFXType.BuffGlow);
                ApplyConfig(ps, cfg);
                ps.Play();
            }
        }

        /// <summary>
        /// Play aegis pop VFX at a position.
        /// </summary>
        public void PlayAegisPopVFX(Vector3 position)
        {
            var ps = GetFromPool(VFXType.AegisPop);
            if (ps != null)
            {
                ps.transform.position = position;
                var cfg = GetConfig(VFXType.AegisPop);
                ApplyConfig(ps, cfg);
                ps.Play();
            }
        }

        /// <summary>
        /// Play reborn VFX at a position.
        /// </summary>
        public void PlayRebornVFX(Vector3 position)
        {
            var ps = GetFromPool(VFXType.RebornRevive);
            if (ps != null)
            {
                ps.transform.position = position;
                var cfg = GetConfig(VFXType.RebornRevive);
                ApplyConfig(ps, cfg);
                ps.Play();
            }
        }

        // ====== POOL MANAGEMENT ======

        private ParticleSystem GetFromPool(VFXType type)
        {
            if (!pools.ContainsKey(type))
                pools[type] = new Queue<ParticleSystem>();

            var pool = pools[type];

            // Return from pool if available
            while (pool.Count > 0)
            {
                var ps = pool.Dequeue();
                if (ps != null)
                {
                    ps.gameObject.SetActive(true);
                    activeEffects.Add(ps);
                    return ps;
                }
            }

            // Create new if under max
            if (GetTotalPoolCount() < maxPoolSize)
            {
                var ps = CreateParticleSystem(type);
                activeEffects.Add(ps);
                return ps;
            }

            Debug.LogWarning($"[VFXManager] Pool exhausted for {type}, max={maxPoolSize}");
            return null;
        }

        private void ReturnToPool(ParticleSystem ps, VFXType type)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            activeEffects.Remove(ps);

            if (!pools.ContainsKey(type))
                pools[type] = new Queue<ParticleSystem>();
            pools[type].Enqueue(ps);
        }

        private void PrewarmPool(VFXType type, int count)
        {
            if (!pools.ContainsKey(type))
                pools[type] = new Queue<ParticleSystem>();

            for (int i = 0; i < count; i++)
            {
                var ps = CreateParticleSystem(type);
                ps.gameObject.SetActive(false);
                pools[type].Enqueue(ps);
            }
        }

        private int GetTotalPoolCount()
        {
            int total = activeEffects.Count;
            foreach (var pool in pools.Values)
                total += pool.Count;
            return total;
        }

        /// <summary>
        /// Create a procedural particle system for the given VFX type.
        /// </summary>
        private ParticleSystem CreateParticleSystem(VFXType type)
        {
            GameObject obj = new GameObject($"VFX_{type}");
            obj.transform.SetParent(poolRoot);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;

            var cfg = GetConfig(type);
            ApplyConfig(ps, cfg);

            // Tag with type for pool return
            var tag = obj.AddComponent<VFXPoolTag>();
            tag.vfxType = type;

            ps.Stop();
            return ps;
        }

        private VFXTypeConfig GetConfig(VFXType type)
        {
            if (config != null)
                return config.GetConfig(type);

            // Fallback defaults
            return VFXTypeConfig.GetDefault(type);
        }

        private void ApplyConfig(ParticleSystem ps, VFXTypeConfig cfg)
        {
            if (ps == null || cfg == null) return;

            var main = ps.main;
            main.duration = cfg.duration;
            main.startLifetime = cfg.lifetime;
            main.startSpeed = cfg.speed;
            main.startSize = cfg.size;
            main.startColor = cfg.color;
            main.maxParticles = cfg.maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cfg.burstCount) });

            var shape = ps.shape;
            shape.shapeType = cfg.shapeType;
            shape.radius = cfg.shapeRadius;

            // Color over lifetime
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(cfg.color, 0f), new GradientColorKey(cfg.color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = gradient;

            // Size over lifetime (shrink)
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        }

        private VFXType MapAbilityToVFX(string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName)) return VFXType.AbilityGeneric;

            string lower = abilityName.ToLower();
            if (lower.Contains("reborn")) return VFXType.RebornRevive;
            if (lower.Contains("venomous")) return VFXType.AbilityVenomous;
            if (lower.Contains("windfury")) return VFXType.AbilityWindfury;
            if (lower.Contains("buff")) return VFXType.BuffGlow;
            if (lower.Contains("deathrattle")) return VFXType.AbilityDeathrattle;
            if (lower.Contains("battlecry")) return VFXType.AbilityBattlecry;
            return VFXType.AbilityGeneric;
        }

        private void Update()
        {
            // Return completed effects to pool
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var ps = activeEffects[i];
                if (ps == null)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }
                if (!ps.isPlaying && !ps.isEmitting)
                {
                    var tag = ps.GetComponent<VFXPoolTag>();
                    if (tag != null)
                        ReturnToPool(ps, tag.vfxType);
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
    /// Types of VFX effects.
    /// </summary>
    public enum VFXType
    {
        // T309: Attack
        AttackSwoosh,
        ImpactBurst,
        // T310: Abilities
        AbilityGeneric,
        AbilityBattlecry,
        AbilityDeathrattle,
        AbilityVenomous,
        AbilityWindfury,
        BuffGlow,
        AegisPop,
        RebornRevive,
        // T311: Death
        DeathSoulRelease,
        DeathCardShatter
    }

    /// <summary>
    /// Tag component for pool tracking.
    /// </summary>
    public class VFXPoolTag : MonoBehaviour
    {
        public VFXType vfxType;
    }
}

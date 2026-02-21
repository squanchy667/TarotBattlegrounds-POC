using UnityEngine;

namespace TarotBattlegrounds.Combat.VFX
{
    /// <summary>
    /// T308: ScriptableObject configuration for VFX particle parameters.
    /// Can be edited in Unity Inspector for per-project tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "VFXConfig", menuName = "Tarot Battlegrounds/VFX Config")]
    public class VFXConfig : ScriptableObject
    {
        [Header("Attack VFX (T309)")]
        public VFXTypeConfig attackSwoosh = VFXTypeConfig.GetDefault(VFXType.AttackSwoosh);
        public VFXTypeConfig impactBurst = VFXTypeConfig.GetDefault(VFXType.ImpactBurst);

        [Header("Ability VFX (T310)")]
        public VFXTypeConfig abilityGeneric = VFXTypeConfig.GetDefault(VFXType.AbilityGeneric);
        public VFXTypeConfig abilityBattlecry = VFXTypeConfig.GetDefault(VFXType.AbilityBattlecry);
        public VFXTypeConfig abilityDeathrattle = VFXTypeConfig.GetDefault(VFXType.AbilityDeathrattle);
        public VFXTypeConfig abilityVenomous = VFXTypeConfig.GetDefault(VFXType.AbilityVenomous);
        public VFXTypeConfig abilityWindfury = VFXTypeConfig.GetDefault(VFXType.AbilityWindfury);
        public VFXTypeConfig buffGlow = VFXTypeConfig.GetDefault(VFXType.BuffGlow);
        public VFXTypeConfig aegisPop = VFXTypeConfig.GetDefault(VFXType.AegisPop);
        public VFXTypeConfig rebornRevive = VFXTypeConfig.GetDefault(VFXType.RebornRevive);

        [Header("Death VFX (T311)")]
        public VFXTypeConfig deathSoulRelease = VFXTypeConfig.GetDefault(VFXType.DeathSoulRelease);
        public VFXTypeConfig deathCardShatter = VFXTypeConfig.GetDefault(VFXType.DeathCardShatter);

        public VFXTypeConfig GetConfig(VFXType type)
        {
            switch (type)
            {
                case VFXType.AttackSwoosh: return attackSwoosh;
                case VFXType.ImpactBurst: return impactBurst;
                case VFXType.AbilityGeneric: return abilityGeneric;
                case VFXType.AbilityBattlecry: return abilityBattlecry;
                case VFXType.AbilityDeathrattle: return abilityDeathrattle;
                case VFXType.AbilityVenomous: return abilityVenomous;
                case VFXType.AbilityWindfury: return abilityWindfury;
                case VFXType.BuffGlow: return buffGlow;
                case VFXType.AegisPop: return aegisPop;
                case VFXType.RebornRevive: return rebornRevive;
                case VFXType.DeathSoulRelease: return deathSoulRelease;
                case VFXType.DeathCardShatter: return deathCardShatter;
                default: return abilityGeneric;
            }
        }
    }

    /// <summary>
    /// Configuration for a single VFX type.
    /// </summary>
    [System.Serializable]
    public class VFXTypeConfig
    {
        public float duration = 0.5f;
        public float lifetime = 0.5f;
        public float speed = 5f;
        public float size = 0.3f;
        public Color color = Color.white;
        public int maxParticles = 20;
        public int burstCount = 10;
        public ParticleSystemShapeType shapeType = ParticleSystemShapeType.Sphere;
        public float shapeRadius = 0.3f;

        public static VFXTypeConfig GetDefault(VFXType type)
        {
            switch (type)
            {
                case VFXType.AttackSwoosh:
                    return new VFXTypeConfig
                    {
                        duration = 0.3f, lifetime = 0.3f, speed = 15f, size = 0.2f,
                        color = new Color(1f, 0.9f, 0.4f), maxParticles = 15, burstCount = 8,
                        shapeType = ParticleSystemShapeType.Cone, shapeRadius = 0.1f
                    };
                case VFXType.ImpactBurst:
                    return new VFXTypeConfig
                    {
                        duration = 0.2f, lifetime = 0.4f, speed = 8f, size = 0.4f,
                        color = new Color(1f, 0.5f, 0.1f), maxParticles = 20, burstCount = 15,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.2f
                    };
                case VFXType.AbilityBattlecry:
                    return new VFXTypeConfig
                    {
                        duration = 0.5f, lifetime = 0.8f, speed = 3f, size = 0.3f,
                        color = new Color(0.3f, 0.7f, 1f), maxParticles = 25, burstCount = 12,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.5f
                    };
                case VFXType.AbilityDeathrattle:
                    return new VFXTypeConfig
                    {
                        duration = 0.6f, lifetime = 1f, speed = 2f, size = 0.35f,
                        color = new Color(0.6f, 0.2f, 0.8f), maxParticles = 20, burstCount = 10,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.4f
                    };
                case VFXType.AbilityVenomous:
                    return new VFXTypeConfig
                    {
                        duration = 0.3f, lifetime = 0.5f, speed = 5f, size = 0.25f,
                        color = new Color(0.1f, 0.9f, 0.1f), maxParticles = 15, burstCount = 8,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.3f
                    };
                case VFXType.AbilityWindfury:
                    return new VFXTypeConfig
                    {
                        duration = 0.3f, lifetime = 0.4f, speed = 12f, size = 0.2f,
                        color = new Color(0.6f, 0.9f, 1f), maxParticles = 20, burstCount = 10,
                        shapeType = ParticleSystemShapeType.Cone, shapeRadius = 0.15f
                    };
                case VFXType.BuffGlow:
                    return new VFXTypeConfig
                    {
                        duration = 0.4f, lifetime = 0.6f, speed = 2f, size = 0.25f,
                        color = new Color(0.2f, 1f, 0.4f), maxParticles = 15, burstCount = 8,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.3f
                    };
                case VFXType.AegisPop:
                    return new VFXTypeConfig
                    {
                        duration = 0.3f, lifetime = 0.5f, speed = 6f, size = 0.3f,
                        color = new Color(1f, 0.9f, 0.3f), maxParticles = 25, burstCount = 15,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.4f
                    };
                case VFXType.RebornRevive:
                    return new VFXTypeConfig
                    {
                        duration = 0.5f, lifetime = 0.8f, speed = 3f, size = 0.3f,
                        color = new Color(1f, 1f, 0.6f), maxParticles = 20, burstCount = 12,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.4f
                    };
                case VFXType.DeathSoulRelease:
                    return new VFXTypeConfig
                    {
                        duration = 0.5f, lifetime = 1f, speed = 3f, size = 0.35f,
                        color = new Color(0.5f, 0.5f, 1f, 0.8f), maxParticles = 15, burstCount = 8,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.3f
                    };
                case VFXType.DeathCardShatter:
                    return new VFXTypeConfig
                    {
                        duration = 0.3f, lifetime = 0.6f, speed = 8f, size = 0.2f,
                        color = new Color(0.8f, 0.3f, 0.3f), maxParticles = 25, burstCount = 15,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.2f
                    };
                default:
                    return new VFXTypeConfig
                    {
                        duration = 0.5f, lifetime = 0.5f, speed = 5f, size = 0.3f,
                        color = Color.white, maxParticles = 20, burstCount = 10,
                        shapeType = ParticleSystemShapeType.Sphere, shapeRadius = 0.3f
                    };
            }
        }
    }
}

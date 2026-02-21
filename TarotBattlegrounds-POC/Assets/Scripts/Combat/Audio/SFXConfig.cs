using UnityEngine;

namespace TarotBattlegrounds.Combat.Audio
{
    /// <summary>
    /// T313: ScriptableObject mapping SFX events to AudioClips.
    /// Assign clips in the Unity Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "SFXConfig", menuName = "Tarot Battlegrounds/SFX Config")]
    public class SFXConfig : ScriptableObject
    {
        [Header("Combat Sounds")]
        public AudioClip attackSwing;
        public AudioClip attackImpact;
        public AudioClip counterattackHit;
        public AudioClip cardDeath;
        public AudioClip aegisPop;

        [Header("Ability Sounds")]
        public AudioClip battlecry;
        public AudioClip deathrattle;
        public AudioClip venomous;
        public AudioClip windfury;
        public AudioClip reborn;
        public AudioClip buffApplied;
        public AudioClip summonToken;

        [Header("UI Sounds")]
        public AudioClip buttonClick;
        public AudioClip cardDraw;
        public AudioClip cardPlay;
        public AudioClip coinGain;
        public AudioClip tierUp;

        [Header("Phase Transitions")]
        public AudioClip combatStart;
        public AudioClip combatEnd;
        public AudioClip victory;
        public AudioClip defeat;

        /// <summary>
        /// Get the AudioClip for a given SFX event.
        /// Returns null if no clip is assigned.
        /// </summary>
        public AudioClip GetClip(SFXEvent sfxEvent)
        {
            switch (sfxEvent)
            {
                case SFXEvent.AttackSwing: return attackSwing;
                case SFXEvent.AttackImpact: return attackImpact;
                case SFXEvent.CounterattackHit: return counterattackHit;
                case SFXEvent.CardDeath: return cardDeath;
                case SFXEvent.AegisPop: return aegisPop;
                case SFXEvent.Battlecry: return battlecry;
                case SFXEvent.Deathrattle: return deathrattle;
                case SFXEvent.Venomous: return venomous;
                case SFXEvent.Windfury: return windfury;
                case SFXEvent.Reborn: return reborn;
                case SFXEvent.BuffApplied: return buffApplied;
                case SFXEvent.SummonToken: return summonToken;
                case SFXEvent.ButtonClick: return buttonClick;
                case SFXEvent.CardDraw: return cardDraw;
                case SFXEvent.CardPlay: return cardPlay;
                case SFXEvent.CoinGain: return coinGain;
                case SFXEvent.TierUp: return tierUp;
                case SFXEvent.CombatStart: return combatStart;
                case SFXEvent.CombatEnd: return combatEnd;
                case SFXEvent.Victory: return victory;
                case SFXEvent.Defeat: return defeat;
                default: return null;
            }
        }
    }
}

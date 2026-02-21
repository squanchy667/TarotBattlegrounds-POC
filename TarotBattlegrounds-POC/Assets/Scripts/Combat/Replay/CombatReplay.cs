using System.Collections.Generic;
using UnityEngine;

namespace TarotBattlegrounds.Combat.Replay
{
    public enum CombatActionType
    {
        CombatStart,
        Attack,
        TakeDamage,
        Die,
        AbilityTrigger,
        SummonToken,
        BuffApplied,
        AegisPopped,
        Counterattack,
        EchoTrigger,
        CombatEnd,
        // Phase II T105-T107
        Reborn,
        VenomousKill,
        WindfuryAttack
    }

    [System.Serializable]
    public class CombatReplayAction
    {
        public CombatActionType type;
        public int sourceCardIndex;
        public int sourceOwnerSide;
        public int targetCardIndex;
        public int targetOwnerSide;
        public int value;
        public int targetHealthAfter;
        public string abilityName;
        public float timestamp;

        public static CombatReplayAction CombatStart() =>
            new CombatReplayAction { type = CombatActionType.CombatStart, sourceCardIndex = -1, sourceOwnerSide = -1, targetCardIndex = -1, targetOwnerSide = -1 };

        public static CombatReplayAction MakeAttack(int srcIndex, int srcSide, int tgtIndex, int tgtSide) =>
            new CombatReplayAction { type = CombatActionType.Attack, sourceCardIndex = srcIndex, sourceOwnerSide = srcSide, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide };

        public static CombatReplayAction MakeTakeDamage(int tgtIndex, int tgtSide, int dmg, int healthAfter) =>
            new CombatReplayAction { type = CombatActionType.TakeDamage, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide, value = dmg, targetHealthAfter = healthAfter };

        public static CombatReplayAction MakeCounterattack(int srcIndex, int srcSide, int tgtIndex, int tgtSide, int dmg, int healthAfter) =>
            new CombatReplayAction { type = CombatActionType.Counterattack, sourceCardIndex = srcIndex, sourceOwnerSide = srcSide, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide, value = dmg, targetHealthAfter = healthAfter };

        public static CombatReplayAction MakeDie(int tgtIndex, int tgtSide) =>
            new CombatReplayAction { type = CombatActionType.Die, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide };

        public static CombatReplayAction MakeAbilityTrigger(int srcIndex, int srcSide, string name) =>
            new CombatReplayAction { type = CombatActionType.AbilityTrigger, sourceCardIndex = srcIndex, sourceOwnerSide = srcSide, abilityName = name };

        public static CombatReplayAction MakeBuffApplied(int tgtIndex, int tgtSide, int buffAmt) =>
            new CombatReplayAction { type = CombatActionType.BuffApplied, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide, value = buffAmt };

        public static CombatReplayAction MakeAegisPopped(int tgtIndex, int tgtSide) =>
            new CombatReplayAction { type = CombatActionType.AegisPopped, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide };

        public static CombatReplayAction MakeEchoTrigger(int srcIndex, int srcSide, int tgtIndex, int tgtSide, int buffAmt) =>
            new CombatReplayAction { type = CombatActionType.EchoTrigger, sourceCardIndex = srcIndex, sourceOwnerSide = srcSide, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide, value = buffAmt, abilityName = "Echo" };

        public static CombatReplayAction MakeCombatEnd() =>
            new CombatReplayAction { type = CombatActionType.CombatEnd };

        public static CombatReplayAction MakeReborn(int tgtIndex, int tgtSide) =>
            new CombatReplayAction { type = CombatActionType.Reborn, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide, abilityName = "Reborn" };

        public static CombatReplayAction MakeVenomousKill(int srcIndex, int srcSide, int tgtIndex, int tgtSide) =>
            new CombatReplayAction { type = CombatActionType.VenomousKill, sourceCardIndex = srcIndex, sourceOwnerSide = srcSide, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide, abilityName = "Venomous" };

        public static CombatReplayAction MakeWindfuryAttack(int srcIndex, int srcSide, int tgtIndex, int tgtSide) =>
            new CombatReplayAction { type = CombatActionType.WindfuryAttack, sourceCardIndex = srcIndex, sourceOwnerSide = srcSide, targetCardIndex = tgtIndex, targetOwnerSide = tgtSide, abilityName = "Windfury" };
    }

    [System.Serializable]
    public class CombatCardSnapshot
    {
        public int cardInstanceId;
        public string cardName;
        public int attack;
        public int health;
        public int maxHealth;
        public bool hasTaunt;
        public bool hasAegis;
        public bool isGolden;
        public bool hasReborn;
        public bool hasWindfury;
        public bool hasVenomous;
        public int armor;
        public string[] abilityNames;
        public string[] tribeNames;
        public int boardPosition;

        public static CombatCardSnapshot FromCard(Card card, int position)
        {
            if (card == null) return null;

            string[] tribes;
            if (card.tribes != null && card.tribes.Length > 0)
            {
                tribes = new string[card.tribes.Length];
                for (int i = 0; i < card.tribes.Length; i++)
                    tribes[i] = card.tribes[i].ToString();
            }
            else if (!string.IsNullOrEmpty(card.tribe))
                tribes = new[] { card.tribe };
            else
                tribes = new string[0];

            string[] abilities;
            if (card.abilityTrigger != AbilityTrigger.None && !string.IsNullOrEmpty(card.ability))
                abilities = new[] { card.abilityTrigger.ToString() + ": " + card.ability };
            else if (!string.IsNullOrEmpty(card.ability))
                abilities = new[] { card.ability };
            else
                abilities = new string[0];

            return new CombatCardSnapshot
            {
                cardInstanceId = card.GetInstanceID(),
                cardName = card.cardName,
                attack = card.attack,
                health = card.health,
                maxHealth = card.health,
                hasTaunt = card.effectType == Card.EffectType.Guardian || card.abilityEffect == Card.AbilityEffectType.Taunt,
                hasAegis = card.hasAegis,
                isGolden = card.isGolden,
                hasReborn = card.hasReborn,
                hasWindfury = card.hasWindfury,
                hasVenomous = card.hasVenomous,
                armor = card.armor,
                abilityNames = abilities,
                tribeNames = tribes,
                boardPosition = position
            };
        }
    }

    [System.Serializable]
    public class CombatReplayState
    {
        public List<CombatCardSnapshot> attackerBoard;
        public List<CombatCardSnapshot> defenderBoard;
        public string attackerName;
        public string defenderName;
        public int attackerHealth;
        public int defenderHealth;

        public static CombatReplayState FromBoards(List<Card> attackerClones, List<Card> defenderClones,
            string attackerName, string defenderName, int attackerHealth, int defenderHealth)
        {
            var state = new CombatReplayState
            {
                attackerName = attackerName,
                defenderName = defenderName,
                attackerHealth = attackerHealth,
                defenderHealth = defenderHealth,
                attackerBoard = new List<CombatCardSnapshot>(attackerClones.Count),
                defenderBoard = new List<CombatCardSnapshot>(defenderClones.Count)
            };
            for (int i = 0; i < attackerClones.Count; i++)
                state.attackerBoard.Add(CombatCardSnapshot.FromCard(attackerClones[i], i));
            for (int i = 0; i < defenderClones.Count; i++)
                state.defenderBoard.Add(CombatCardSnapshot.FromCard(defenderClones[i], i));
            return state;
        }
    }

    [System.Serializable]
    public class CombatReplayResult
    {
        public string winnerSide;
        public string winnerName;
        public int damageDealt;
        public List<CombatCardSnapshot> survivingCards;
        public int turnCount;
    }

    [System.Serializable]
    public class CombatReplay
    {
        public const int CURRENT_VERSION = 1;
        public int version = CURRENT_VERSION;

        public CombatReplayState initialState;
        public List<CombatReplayAction> actions;
        public CombatReplayResult result;
        public float estimatedDuration;

        public const float DURATION_ATTACK = 0.5f;
        public const float DURATION_TAKE_DAMAGE = 0.3f;
        public const float DURATION_COUNTERATTACK = 0.3f;
        public const float DURATION_DIE = 0.5f;
        public const float DURATION_ABILITY = 0.8f;
        public const float DURATION_BUFF = 0.3f;
        public const float DURATION_AEGIS_POP = 0.3f;
        public const float DURATION_ECHO = 0.5f;
        public const float DURATION_SUMMON_TOKEN = 0.4f;
        public const float DURATION_COMBAT_END = 0.2f;
        public const float DURATION_REBORN = 0.6f;
        public const float DURATION_VENOMOUS_KILL = 0.3f;
        public const float DURATION_WINDFURY_ATTACK = 0.4f;

        public static float DurationOf(CombatActionType type)
        {
            switch (type)
            {
                case CombatActionType.Attack: return DURATION_ATTACK;
                case CombatActionType.TakeDamage: return DURATION_TAKE_DAMAGE;
                case CombatActionType.Counterattack: return DURATION_COUNTERATTACK;
                case CombatActionType.Die: return DURATION_DIE;
                case CombatActionType.AbilityTrigger: return DURATION_ABILITY;
                case CombatActionType.BuffApplied: return DURATION_BUFF;
                case CombatActionType.AegisPopped: return DURATION_AEGIS_POP;
                case CombatActionType.EchoTrigger: return DURATION_ECHO;
                case CombatActionType.SummonToken: return DURATION_SUMMON_TOKEN;
                case CombatActionType.Reborn: return DURATION_REBORN;
                case CombatActionType.VenomousKill: return DURATION_VENOMOUS_KILL;
                case CombatActionType.WindfuryAttack: return DURATION_WINDFURY_ATTACK;
                default: return DURATION_COMBAT_END;
            }
        }

        public static CombatReplay CreateEmpty() =>
            new CombatReplay { version = CURRENT_VERSION, actions = new List<CombatReplayAction>(), estimatedDuration = 0f };

        /// <summary>
        /// Record an action, auto-advancing the timestamp clock.
        /// </summary>
        public float Record(CombatReplayAction action)
        {
            action.timestamp = estimatedDuration;
            actions.Add(action);
            estimatedDuration += DurationOf(action.type);
            return action.timestamp;
        }

        public float RecordCombatStart() => Record(CombatReplayAction.CombatStart());
        public float RecordAttack(int si, int ss, int ti, int ts) => Record(CombatReplayAction.MakeAttack(si, ss, ti, ts));
        public float RecordTakeDamage(int ti, int ts, int dmg, int ha) => Record(CombatReplayAction.MakeTakeDamage(ti, ts, dmg, ha));
        public float RecordCounterattack(int si, int ss, int ti, int ts, int dmg, int ha) => Record(CombatReplayAction.MakeCounterattack(si, ss, ti, ts, dmg, ha));
        public float RecordDie(int ti, int ts) => Record(CombatReplayAction.MakeDie(ti, ts));
        public float RecordAbilityTrigger(int si, int ss, string n) => Record(CombatReplayAction.MakeAbilityTrigger(si, ss, n));
        public float RecordBuffApplied(int ti, int ts, int b) => Record(CombatReplayAction.MakeBuffApplied(ti, ts, b));
        public float RecordAegisPopped(int ti, int ts) => Record(CombatReplayAction.MakeAegisPopped(ti, ts));
        public float RecordEchoTrigger(int si, int ss, int ti, int ts, int b) => Record(CombatReplayAction.MakeEchoTrigger(si, ss, ti, ts, b));
        public float RecordCombatEnd() => Record(CombatReplayAction.MakeCombatEnd());
        public float RecordReborn(int ti, int ts) => Record(CombatReplayAction.MakeReborn(ti, ts));
        public float RecordVenomousKill(int si, int ss, int ti, int ts) => Record(CombatReplayAction.MakeVenomousKill(si, ss, ti, ts));
        public float RecordWindfuryAttack(int si, int ss, int ti, int ts) => Record(CombatReplayAction.MakeWindfuryAttack(si, ss, ti, ts));
    }
}

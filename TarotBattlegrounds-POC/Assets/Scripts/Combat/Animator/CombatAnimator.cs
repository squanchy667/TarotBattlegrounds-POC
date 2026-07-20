using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using TarotBattlegrounds.Combat.Replay;
using TarotBattlegrounds.Combat.VFX;
using TarotBattlegrounds.Combat.Audio;
using TarotBattlegrounds.UI;

namespace TarotBattlegrounds.Combat.Animator
{
    /// <summary>
    /// T304: Coroutine-based combat replay playback system.
    /// Reads a CombatReplay and animates each action step-by-step using
    /// CombatCardVisual instances, VFXManager, and SFXManager.
    ///
    /// T316: UI integration — shows animated replay during combat phase.
    /// T317: Skip button + speed controls.
    /// </summary>
    public class CombatAnimator : MonoBehaviour
    {
        public static CombatAnimator Instance { get; private set; }

        [Header("Combat Arena")]
        [SerializeField] private RectTransform combatPanel;
        [SerializeField] private RectTransform attackerBoardContainer;
        [SerializeField] private RectTransform defenderBoardContainer;

        [Header("Info Display")]
        [SerializeField] private TMP_Text attackerNameText;
        [SerializeField] private TMP_Text defenderNameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text resultText;

        [Header("Controls (T317)")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedButtonText;

        [Header("Arena Visual (UX16)")]
        [SerializeField] private CombatArenaVisual arenaVisual;

        [Header("Card Visual Prefab")]
        [SerializeField] private GameObject cardVisualPrefab;

        [Header("Speed Settings")]
        [SerializeField] private float[] speedMultipliers = { 1f, 2f, 4f };
        private int currentSpeedIndex = 0;

        // Runtime state
        private CombatReplay currentReplay;
        private List<CombatCardVisual> attackerCards = new List<CombatCardVisual>();
        private List<CombatCardVisual> defenderCards = new List<CombatCardVisual>();
        private Coroutine playbackCoroutine;
        private bool skipRequested;
        private bool isPlaying;

        /// <summary>
        /// True while a replay is being animated.
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Fired when replay playback completes (skipped or naturally).
        /// </summary>
        public event System.Action OnPlaybackComplete;

        /// <summary>
        /// Current playback speed multiplier.
        /// </summary>
        public float CurrentSpeed => speedMultipliers[currentSpeedIndex];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);
            if (speedButton != null)
                speedButton.onClick.AddListener(OnSpeedClicked);

            SetPanelVisible(false);
        }

        /// <summary>
        /// T316: Play a combat replay with animated visuals.
        /// Call this from GameManager after SimulateBattle produces a replay.
        /// </summary>
        public void PlayReplay(CombatReplay replay)
        {
            if (replay == null || replay.actions == null || replay.actions.Count == 0)
            {
                Debug.Log("[CombatAnimator] No replay data to play");
                OnPlaybackComplete?.Invoke();
                return;
            }

            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                CleanupCards();
            }

            currentReplay = replay;
            skipRequested = false;
            isPlaying = true;
            SetPanelVisible(true);
            playbackCoroutine = StartCoroutine(PlaybackCoroutine());
        }

        /// <summary>
        /// T317: Skip remaining replay and show result immediately.
        /// Force-stops nested animate coroutines so IsPlaying cannot stick true forever
        /// (T839/T841: mid-action WaitForSeconds ignores skipRequested).
        /// </summary>
        public void SkipReplay()
        {
            skipRequested = true;
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }
            StopAllCoroutines();
            // Finish presentation state so callers waiting on IsPlaying unblock
            if (isPlaying)
            {
                ApplyFinalSurvivorVisuals();
                LastFinalActiveVisualNames = GetActiveVisualCardNames();
                ShowResult();
                isPlaying = false;
                arenaVisual?.HideArena();
                SetPanelVisible(false);
                CleanupCards();
                if (MusicManager.Instance != null)
                    MusicManager.Instance.PlayRecruitMusic();
                OnPlaybackComplete?.Invoke();
            }
        }

        /// <summary>Index of the action currently being animated (T847 look-ahead for Reborn).</summary>
        private int _playbackActionIndex = -1;

        /// <summary>
        /// T847: names of active visuals after the last ApplyFinalSurvivorVisuals (for strict T843 assert).
        /// </summary>
        public List<string> LastFinalActiveVisualNames { get; private set; } = new List<string>();

        /// <summary>True if ApplyFinalSurvivorVisuals had to hide anything (desync alarm).</summary>
        public bool LastFinalSurvivorHadDesync { get; private set; }

        private IEnumerator PlaybackCoroutine()
        {
            // Setup arena
            SetupArena();

            // Play combat start music
            if (MusicManager.Instance != null)
                MusicManager.Instance.PlayCombatMusic();
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXEvent.CombatStart);

            // Status
            if (statusText != null)
                statusText.text = "Combat!";

            // Process each action
            for (int i = 0; i < currentReplay.actions.Count; i++)
            {
                if (skipRequested) break;

                _playbackActionIndex = i;
                var action = currentReplay.actions[i];
                yield return StartCoroutine(AnimateAction(action));
            }

            // T843 safety net — after T847 live lists, this should almost never hide anything
            ApplyFinalSurvivorVisuals();
            LastFinalActiveVisualNames = GetActiveVisualCardNames();

            // Show result (in-panel + keep readable longer for testing)
            ShowResult();

            // Wait a moment for result to be visible
            yield return new WaitForSeconds(skipRequested ? 0.85f : 2.8f);

            // Cleanup
            isPlaying = false;
            arenaVisual?.HideArena(); // UX16: Hide arena visuals
            SetPanelVisible(false);
            CleanupCards();

            // Switch back to recruit music
            if (MusicManager.Instance != null)
                MusicManager.Instance.PlayRecruitMusic();

            OnPlaybackComplete?.Invoke();
            playbackCoroutine = null;
        }

        /// <summary>
        /// Build the visual board from the replay's initial state.
        /// </summary>
        private void SetupArena()
        {
            CleanupCards();

            if (currentReplay.initialState == null)
            {
                Debug.LogWarning("[CombatAnimator] No initial state in replay");
                return;
            }

            var state = currentReplay.initialState;

            // Attacker name
            if (attackerNameText != null)
                attackerNameText.text = state.attackerName ?? "Attacker";
            if (defenderNameText != null)
                defenderNameText.text = state.defenderName ?? "Defender";

            // UX16: Show arena visuals
            arenaVisual?.ShowArena(
                state.attackerName ?? "Attacker",
                state.defenderName ?? "Defender");

            // Create attacker cards
            if (state.attackerBoard != null)
            {
                for (int i = 0; i < state.attackerBoard.Count; i++)
                {
                    var snap = state.attackerBoard[i];
                    if (snap == null) continue;
                    var visual = CreateCardVisual(attackerBoardContainer, snap, 0, i);
                    attackerCards.Add(visual);
                }
            }

            // Create defender cards
            if (state.defenderBoard != null)
            {
                for (int i = 0; i < state.defenderBoard.Count; i++)
                {
                    var snap = state.defenderBoard[i];
                    if (snap == null) continue;
                    var visual = CreateCardVisual(defenderBoardContainer, snap, 1, i);
                    defenderCards.Add(visual);
                }
            }
        }

        private CombatCardVisual CreateCardVisual(RectTransform container, CombatCardSnapshot snap, int side, int index)
        {
            if (container == null)
            {
                // Create container dynamically if not assigned
                Debug.LogWarning("[CombatAnimator] Board container not assigned, creating dynamically");
                return CombatCardVisual.CreateFromCode(transform, snap, side, index);
            }

            if (cardVisualPrefab != null)
            {
                GameObject obj = Instantiate(cardVisualPrefab, container);
                CombatCardVisual visual = obj.GetComponent<CombatCardVisual>();
                if (visual != null)
                {
                    visual.Setup(snap, side, index);
                    return visual;
                }
                Destroy(obj);
            }

            // Fallback: create from code
            return CombatCardVisual.CreateFromCode(container, snap, side, index);
        }

        /// <summary>
        /// Animate a single replay action.
        /// </summary>
        private IEnumerator AnimateAction(CombatReplayAction action)
        {
            float duration = CombatReplay.DurationOf(action.type) / CurrentSpeed;

            switch (action.type)
            {
                case CombatActionType.CombatStart:
                    if (statusText != null) statusText.text = "Fight!";
                    yield return new WaitForSeconds(duration);
                    break;

                case CombatActionType.Attack:
                    yield return AnimateAttack(action, duration);
                    break;

                case CombatActionType.WindfuryAttack:
                    if (statusText != null) statusText.text = "Windfury!";
                    yield return AnimateAttack(action, duration);
                    break;

                case CombatActionType.TakeDamage:
                    yield return AnimateDamage(action, duration);
                    break;

                case CombatActionType.Counterattack:
                    yield return AnimateCounterattack(action, duration);
                    break;

                case CombatActionType.Die:
                    yield return AnimateDeath(action, duration);
                    break;

                case CombatActionType.Reborn:
                    yield return AnimateReborn(action, duration);
                    break;

                case CombatActionType.VenomousKill:
                    yield return AnimateVenomousKill(action, duration);
                    break;

                case CombatActionType.AbilityTrigger:
                    yield return AnimateAbility(action, duration);
                    break;

                case CombatActionType.BuffApplied:
                    yield return AnimateBuff(action, duration);
                    break;

                case CombatActionType.AegisPopped:
                    yield return AnimateAegisPop(action, duration);
                    break;

                case CombatActionType.SummonToken:
                    yield return AnimateSummon(action, duration);
                    break;

                case CombatActionType.EchoTrigger:
                    yield return AnimateEcho(action, duration);
                    break;

                case CombatActionType.CombatEnd:
                    yield return new WaitForSeconds(duration);
                    break;
            }
        }

        // ====== ACTION ANIMATIONS ======

        private IEnumerator AnimateAttack(CombatReplayAction action, float duration)
        {
            var attacker = GetCardVisual(action.sourceCardIndex, action.sourceOwnerSide);
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            if (attacker != null && target != null)
            {
                // SFX
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.AttackSwing);

                // VFX
                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayAttackVFX(
                        attacker.transform.position,
                        target.transform.position);

                // UX16: Attack trail line
                arenaVisual?.ShowAttackTrail(
                    attacker.transform.position,
                    target.transform.position);

                // T306: Lunge animation
                yield return attacker.PlayAttackAnimation(
                    target.transform.position, duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private IEnumerator AnimateDamage(CombatReplayAction action, float duration)
        {
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            // T841: skip flash on null / already-dead (inactive) visuals — death animation
            // deactivates the GO; later TakeDamage actions in the stream must not StartCoroutine.
            if (target != null && target.gameObject.activeInHierarchy)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.AttackImpact);

                target.FlashDamage(action.targetHealthAfter);
                yield return target.PlayHitReaction(duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private IEnumerator AnimateCounterattack(CombatReplayAction action, float duration)
        {
            var attacker = GetCardVisual(action.sourceCardIndex, action.sourceOwnerSide);
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            // T841: counter flash lands on the original attacker (target of the counter action).
            // Skip if that visual is already inactive (e.g. died earlier in the stream).
            if (target != null && target.gameObject.activeInHierarchy)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.CounterattackHit);

                target.FlashDamage(action.targetHealthAfter);
                yield return target.PlayHitReaction(duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private IEnumerator AnimateDeath(CombatReplayAction action, float duration)
        {
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            // T847: sim keeps reborn cards in place — only remove from live list when
            // the next action is NOT Reborn for the same side+index (mirrors CombatTranscript).
            bool rebornNext = IsRebornNext(action);

            if (target != null)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.CardDeath);

                // T311: Death VFX
                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayDeathVFX(target.transform.position);

                // UX14: Show floating death text
                if (FloatingNumberManager.Instance != null)
                    FloatingNumberManager.Instance.ShowDeath(target.GetWorldPosition());

                // T307: Death animation
                yield return target.PlayDeathAnimation(duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }

            // Shrink live visual list to match sim board (T847) so subsequent indices resolve correctly
            if (!rebornNext)
                RemoveLiveVisualAt(action.targetOwnerSide, action.targetCardIndex, destroyVisual: true);
        }

        /// <summary>T847: next action is Reborn for the same dying slot (sim keeps card in list).</summary>
        private bool IsRebornNext(CombatReplayAction dieAction)
        {
            if (currentReplay?.actions == null || dieAction == null) return false;
            int i = _playbackActionIndex;
            if (i < 0 || i + 1 >= currentReplay.actions.Count) return false;
            var next = currentReplay.actions[i + 1];
            return next != null
                && next.type == CombatActionType.Reborn
                && next.targetOwnerSide == dieAction.targetOwnerSide
                && next.targetCardIndex == dieAction.targetCardIndex;
        }

        /// <summary>
        /// T847: remove a visual at the sim index from the live list (board shrink).
        /// </summary>
        private void RemoveLiveVisualAt(int side, int index, bool destroyVisual)
        {
            var list = side == 0 ? attackerCards : defenderCards;
            if (index < 0 || index >= list.Count) return;
            var v = list[index];
            list.RemoveAt(index);
            if (destroyVisual && v != null)
                Destroy(v.gameObject);
        }

        private IEnumerator AnimateReborn(CombatReplayAction action, float duration)
        {
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            if (target != null)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.Reborn);

                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayRebornVFX(target.transform.position);

                yield return target.PlayRebornAnimation(duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private IEnumerator AnimateVenomousKill(CombatReplayAction action, float duration)
        {
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            if (target != null)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.Venomous);

                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayAbilityVFX(target.transform.position, "Venomous");
            }

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator AnimateAbility(CombatReplayAction action, float duration)
        {
            var source = GetCardVisual(action.sourceCardIndex, action.sourceOwnerSide);

            if (source != null)
            {
                string abilityName = action.abilityName ?? "Ability";
                if (statusText != null)
                    statusText.text = abilityName;

                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayAbilityVFX(source.transform.position, abilityName);
            }

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator AnimateBuff(CombatReplayAction action, float duration)
        {
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            if (target != null)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.BuffApplied);

                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayBuffVFX(target.transform.position);

                target.FlashBuff(0, action.value);
            }

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator AnimateAegisPop(CombatReplayAction action, float duration)
        {
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            if (target != null)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXEvent.AegisPop);

                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayAegisPopVFX(target.transform.position);

                target.FlashAegisPop();
            }

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator AnimateSummon(CombatReplayAction action, float duration)
        {
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXEvent.SummonToken);

            // T847: insert at recorded index (sim Insert) — do not append.
            var tokenSnap = new CombatCardSnapshot
            {
                cardName = action.abilityName ?? "Token",
                attack = action.value > 0 ? action.value : 1,
                health = action.value > 0 ? action.value : 1,
                maxHealth = action.value > 0 ? action.value : 1,
                boardPosition = action.targetCardIndex
            };
            int side = action.targetOwnerSide;
            var list = side == 0 ? attackerCards : defenderCards;
            var container = side == 0 ? attackerBoardContainer : defenderBoardContainer;
            int at = Mathf.Clamp(action.targetCardIndex, 0, list.Count);
            var visual = CreateCardVisual(container, tokenSnap, side, at);
            if (visual != null)
            {
                list.Insert(at, visual);
                if (container != null)
                    visual.transform.SetSiblingIndex(Mathf.Min(at, container.childCount - 1));
                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayBuffVFX(visual.transform.position);
                yield return visual.PlaySummonAnimation(duration);
                yield break;
            }

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator AnimateEcho(CombatReplayAction action, float duration)
        {
            var source = GetCardVisual(action.sourceCardIndex, action.sourceOwnerSide);
            var target = GetCardVisual(action.targetCardIndex, action.targetOwnerSide);

            if (target != null)
            {
                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayBuffVFX(target.transform.position);

                target.FlashBuff(action.value, 0);
            }

            yield return new WaitForSeconds(duration);
        }

        // ====== HELPERS ======

        /// <summary>
        /// T847: resolve through LIVE lists that shrink on Die / grow on SummonToken,
        /// matching CombatManager board indices after each death removal.
        /// </summary>
        private CombatCardVisual GetCardVisual(int index, int side)
        {
            if (index < 0) return null;

            var list = side == 0 ? attackerCards : defenderCards;
            if (index >= list.Count) return null;
            return list[index];
        }

        private void ShowResult()
        {
            if (currentReplay?.result == null) return;

            var result = currentReplay.result;
            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                resultText.fontSize = Tokens.TextH1;
                if (result.winnerName == "Tie")
                {
                    resultText.text = "TIE!\n" + result.damageDealt + " dmg each";
                    resultText.color = Tokens.BoneBright;
                }
                else
                {
                    resultText.text = result.winnerName + " WINS!\n" + result.damageDealt + " face damage";
                    resultText.color = Tokens.BronzeBright;
                }
            }

            // T701: Play victory/defeat stinger based on whether the LOCAL player won —
            // previously this always fired PlayVictoryStinger() for any non-tie result,
            // including local player losses, and PlayDefeatStinger() was never called.
            if (MusicManager.Instance != null && result.winnerName != "Tie")
            {
                if (IsLocalPlayerWin(result.winnerName))
                    MusicManager.Instance.PlayVictoryStinger();
                else
                    MusicManager.Instance.PlayDefeatStinger();
            }

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXEvent.CombatEnd);
        }

        /// <summary>
        /// T701: Determine whether a replay result's winner string refers to the local/human
        /// player. Mirrors GameOverUI.ShowGameOver's local-player identification
        /// (GameConfig.HumanPlayerIndex offline, NetworkGameBridge.LocalPlayerSlot online) and
        /// relies on BattleExecutor's naming convention where winnerName is exactly
        /// "Player {index + 1}" for a human-controlled slot (no " (AI)" suffix).
        /// PlayReplay is only ever invoked for battles that include the local player
        /// (see BattleExecutor.IsLocalPlayerBattle), so winnerName here is always either the
        /// local player or their opponent.
        /// </summary>
        private bool IsLocalPlayerWin(string winnerName)
        {
#if PHOTON_UNITY_NETWORKING
            int localIndex = GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer && NetworkGameBridge.Instance != null
                ? NetworkGameBridge.Instance.LocalPlayerSlot
                : GameConfig.HumanPlayerIndex;
#else
            int localIndex = GameConfig.HumanPlayerIndex;
#endif
            return winnerName == $"Player {localIndex + 1}";
        }

        private void SetPanelVisible(bool visible)
        {
            if (combatPanel != null)
            {
                combatPanel.gameObject.SetActive(visible);
                // T845: darker arena dim for stronger separation from recruit UI underneath
                var bg = combatPanel.GetComponent<Image>();
                if (bg != null && visible)
                {
                    bg.color = Tokens.WithAlpha(Tokens.Ash, 0.72f);
                    bg.raycastTarget = true; // block clicks to shop underneath (T844)
                }
                if (visible)
                    combatPanel.transform.SetAsLastSibling();
            }

            if (skipButton != null)
                skipButton.gameObject.SetActive(visible);
            if (speedButton != null)
                speedButton.gameObject.SetActive(visible);

            if (resultText != null && !visible)
                resultText.gameObject.SetActive(false);

            // T844: exclusive combat stage — hide ALL recruit chrome while arena plays
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.SetCombatPresentationMode(visible);
        }

        /// <summary>
        /// T843 safety net: after T847 live lists, hide anything that still disagrees with
        /// result.survivingCards. Any actual hide is a desync alarm (should be zero after T847).
        /// </summary>
        private void ApplyFinalSurvivorVisuals()
        {
            LastFinalSurvivorHadDesync = false;
            if (currentReplay?.result == null) return;

            string winnerSide = currentReplay.result.winnerSide; // "attacker" | "defender" | "Tie"
            var survivors = currentReplay.result.survivingCards ?? new List<CombatCardSnapshot>();
            var survivorNames = new HashSet<string>();
            foreach (var s in survivors)
            {
                if (s != null && !string.IsNullOrEmpty(s.cardName))
                    survivorNames.Add(s.cardName);
            }

            void FilterSide(List<CombatCardVisual> list, bool isWinnerSide)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i];
                    if (v == null) continue;
                    bool keep = isWinnerSide && survivorNames.Contains(v.CardName);
                    if (!keep && v.gameObject != null && v.gameObject.activeSelf)
                    {
                        LastFinalSurvivorHadDesync = true;
                        Debug.LogWarning(
                            $"[CombatAnimator/T843-desync] Hiding leftover visual '{v.CardName}' " +
                            $"(winnerSide={winnerSide}, isWinnerSide={isWinnerSide}) — live list drifted from sim.");
                        v.gameObject.SetActive(false);
                    }
                }
            }

            if (winnerSide == "Tie")
            {
                FilterSide(attackerCards, false);
                FilterSide(defenderCards, false);
            }
            else if (winnerSide == "attacker")
            {
                FilterSide(attackerCards, true);
                FilterSide(defenderCards, false);
            }
            else if (winnerSide == "defender")
            {
                FilterSide(attackerCards, false);
                FilterSide(defenderCards, true);
            }
        }

        /// <summary>T843 / AutoPlaytest v2: active combat-card visuals still showing after playback.</summary>
        public List<string> GetActiveVisualCardNames()
        {
            var names = new List<string>();
            foreach (var c in attackerCards)
                if (c != null && c.gameObject != null && c.gameObject.activeInHierarchy)
                    names.Add("A:" + c.CardName);
            foreach (var c in defenderCards)
                if (c != null && c.gameObject != null && c.gameObject.activeInHierarchy)
                    names.Add("D:" + c.CardName);
            return names;
        }

        /// <summary>
        /// T847 strict T843: winner-side active visual names must match result.survivingCards names
        /// (multiset). Losing side must be empty of active visuals.
        /// </summary>
        public string CompareActiveVisualsToSurvivors()
        {
            // Prefer snapshot taken at end of playback (before CleanupCards)
            var active = LastFinalActiveVisualNames != null
                ? LastFinalActiveVisualNames
                : GetActiveVisualCardNames();

            var result = currentReplay?.result ?? CombatManager.lastReplay?.result;
            if (result == null)
                return "no result on currentReplay/lastReplay";

            var expected = new List<string>();
            if (result.survivingCards != null)
            {
                foreach (var s in result.survivingCards)
                    if (s != null && !string.IsNullOrEmpty(s.cardName))
                        expected.Add(s.cardName);
            }
            expected.Sort();

            string sidePrefix = result.winnerSide == "attacker" ? "A:"
                : result.winnerSide == "defender" ? "D:" : null;

            if (result.winnerSide == "Tie")
            {
                if (active.Count > 0)
                    return $"tie but {active.Count} active visual(s): {string.Join(", ", active)}";
                if (LastFinalSurvivorHadDesync)
                    return "ApplyFinalSurvivorVisuals had to correct a desync on tie";
                return null;
            }

            if (sidePrefix == null)
                return $"unknown winnerSide '{result.winnerSide}'";

            var winnerActive = new List<string>();
            var loserActive = new List<string>();
            foreach (var n in active)
            {
                if (n.StartsWith(sidePrefix))
                    winnerActive.Add(n.Substring(2));
                else
                    loserActive.Add(n);
            }
            winnerActive.Sort();

            if (loserActive.Count > 0)
                return $"losing side still has visuals: {string.Join(", ", loserActive)}";

            if (winnerActive.Count != expected.Count)
                return $"winner visuals [{string.Join(", ", winnerActive)}] count {winnerActive.Count} != survivors [{string.Join(", ", expected)}] count {expected.Count}";

            for (int i = 0; i < expected.Count; i++)
            {
                if (winnerActive[i] != expected[i])
                    return $"winner visuals [{string.Join(", ", winnerActive)}] != survivors [{string.Join(", ", expected)}]";
            }

            // Desync alarm is logged separately; name multiset match is the hard T847 bar.
            // (SkipReplay mid-playback may need the safety net even with correct live lists.)
            return null; // match
        }

        private void CleanupCards()
        {
            foreach (var card in attackerCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            foreach (var card in defenderCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            attackerCards.Clear();
            defenderCards.Clear();
        }

        // T317: Skip button handler
        private void OnSkipClicked()
        {
            skipRequested = true;
        }

        // T317: Speed button handler — cycle through speed multipliers
        private void OnSpeedClicked()
        {
            currentSpeedIndex = (currentSpeedIndex + 1) % speedMultipliers.Length;
            if (speedButtonText != null)
                speedButtonText.text = $"{CurrentSpeed}x";
            Debug.Log($"[CombatAnimator] Speed set to {CurrentSpeed}x");
        }

        private void OnDestroy()
        {
            if (skipButton != null)
                skipButton.onClick.RemoveAllListeners();
            if (speedButton != null)
                speedButton.onClick.RemoveAllListeners();

            CleanupCards();

            if (Instance == this)
                Instance = null;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TarotBattlegrounds.Combat.Replay
{
    /// <summary>
    /// T846: renders a CombatReplay as a human-readable play-by-play Markdown file so a
    /// combat can be analyzed after the fact (AutoPlaytest/transcripts/, gitignored).
    ///
    /// Index semantics: the sim REMOVES dead cards from its board lists mid-combat
    /// (CombatManager death pipeline), so action indices recorded after a death refer to
    /// the SHRUNK board. This walker mirrors those removals, which is the only correct way
    /// to resolve indices back to card names. NOTE the animator does NOT do this — it
    /// indexes fixed-size visual lists (T847) — so this transcript shows what the sim
    /// actually did, which may differ from what the player saw on screen.
    /// </summary>
    public static class CombatTranscript
    {
        private class Entry
        {
            public string name;
            public int atk;
            public int hp;
            public bool aegis;
            public bool reborn;
            public bool golden;
        }

        /// <summary>
        /// Generate the transcript text. Never throws — a malformed replay produces a
        /// transcript with anomaly lines instead.
        /// </summary>
        public static string Generate(CombatReplay replay, string header = null)
        {
            var sb = new StringBuilder();
            var anomalies = new List<string>();

            if (replay == null)
            {
                return "# Combat Transcript\n\n(no replay recorded)\n";
            }

            string atkName = replay.initialState != null ? replay.initialState.attackerName : "Attacker";
            string defName = replay.initialState != null ? replay.initialState.defenderName : "Defender";

            sb.AppendLine("# Combat Transcript");
            if (!string.IsNullOrEmpty(header)) sb.AppendLine("\n" + header);
            sb.AppendLine();

            // --- Initial boards ---
            var side0 = BuildEntries(replay.initialState != null ? replay.initialState.attackerBoard : null);
            var side1 = BuildEntries(replay.initialState != null ? replay.initialState.defenderBoard : null);

            sb.AppendLine($"## Boards at combat start");
            sb.AppendLine();
            AppendBoard(sb, atkName + " (side 0, attacks first)", replay.initialState != null ? replay.initialState.attackerBoard : null);
            AppendBoard(sb, defName + " (side 1)", replay.initialState != null ? replay.initialState.defenderBoard : null);

            // --- Actions ---
            sb.AppendLine("## Play-by-play");
            sb.AppendLine();

            var actions = replay.actions ?? new List<CombatReplayAction>();
            int step = 0;
            bool boardsShrunk = false;
            int actionsAfterFirstDeath = 0;

            for (int i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a == null) continue;
                if (boardsShrunk &&
                    a.type != CombatActionType.CombatStart && a.type != CombatActionType.CombatEnd)
                    actionsAfterFirstDeath++;

                string t = $"`t={a.timestamp,5:0.0}s`";
                switch (a.type)
                {
                    case CombatActionType.CombatStart:
                        sb.AppendLine($"{t} — **Combat begins**: {atkName} vs {defName}");
                        break;

                    case CombatActionType.Attack:
                    {
                        var src = Resolve(side0, side1, a.sourceCardIndex, a.sourceOwnerSide, anomalies, step, "attacker");
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "attack target");
                        sb.AppendLine($"{t} {++step}. ATTACK — {Label(src, a.sourceOwnerSide)} hits {Label(tgt, a.targetOwnerSide)}");
                        break;
                    }

                    case CombatActionType.WindfuryAttack:
                    {
                        var src = Resolve(side0, side1, a.sourceCardIndex, a.sourceOwnerSide, anomalies, step, "windfury attacker");
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "windfury target");
                        sb.AppendLine($"{t} {++step}. WINDFURY — {Label(src, a.sourceOwnerSide)} strikes again at {Label(tgt, a.targetOwnerSide)}");
                        break;
                    }

                    case CombatActionType.TakeDamage:
                    {
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "damage target");
                        if (tgt != null && tgt.hp <= 0)
                            anomalies.Add($"step {step + 1}: damage dealt to {tgt.name} which was already at {tgt.hp} HP");
                        sb.AppendLine($"{t} {++step}. DAMAGE — {Label(tgt, a.targetOwnerSide)} takes {a.value} -> {a.targetHealthAfter} HP");
                        if (tgt != null) tgt.hp = a.targetHealthAfter;
                        break;
                    }

                    case CombatActionType.Counterattack:
                    {
                        var src = Resolve(side0, side1, a.sourceCardIndex, a.sourceOwnerSide, anomalies, step, "counter source");
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "counter target");
                        if (tgt != null && tgt.hp <= 0)
                            anomalies.Add($"step {step + 1}: counterattack against {tgt.name} which was already at {tgt.hp} HP");
                        sb.AppendLine($"{t} {++step}. COUNTER — {Label(src, a.sourceOwnerSide)} counters {Label(tgt, a.targetOwnerSide)} for {a.value} -> {a.targetHealthAfter} HP");
                        if (tgt != null) tgt.hp = a.targetHealthAfter;
                        break;
                    }

                    case CombatActionType.AegisPopped:
                    {
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "aegis target");
                        sb.AppendLine($"{t} {++step}. AEGIS — {Label(tgt, a.targetOwnerSide)}'s shield breaks (0 damage taken)");
                        if (tgt != null) tgt.aegis = false;
                        break;
                    }

                    case CombatActionType.Die:
                    {
                        var list = a.targetOwnerSide == 0 ? side0 : side1;
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "dying card");

                        // Sim keeps a reborn card on the board (revived in place); only
                        // remove the entry when no immediate Reborn follows for it.
                        bool rebornNext = i + 1 < actions.Count
                            && actions[i + 1] != null
                            && actions[i + 1].type == CombatActionType.Reborn
                            && actions[i + 1].targetOwnerSide == a.targetOwnerSide
                            && actions[i + 1].targetCardIndex == a.targetCardIndex;

                        if (rebornNext)
                        {
                            sb.AppendLine($"{t} {++step}. DEATH — {Label(tgt, a.targetOwnerSide)} dies... (Reborn incoming)");
                        }
                        else
                        {
                            sb.AppendLine($"{t} {++step}. DEATH — {Label(tgt, a.targetOwnerSide)} dies and leaves the board");
                            if (tgt != null && a.targetCardIndex >= 0 && a.targetCardIndex < list.Count)
                                list.RemoveAt(a.targetCardIndex);
                            boardsShrunk = true;
                        }
                        break;
                    }

                    case CombatActionType.Reborn:
                    {
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "reborn card");
                        sb.AppendLine($"{t} {++step}. REBORN — {Label(tgt, a.targetOwnerSide)} revives with 1 HP (Reborn spent, Aegis stripped)");
                        if (tgt != null) { tgt.hp = 1; tgt.aegis = false; tgt.reborn = false; }
                        break;
                    }

                    case CombatActionType.VenomousKill:
                    {
                        var src = Resolve(side0, side1, a.sourceCardIndex, a.sourceOwnerSide, anomalies, step, "venomous source");
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "venomous target");
                        sb.AppendLine($"{t} {++step}. VENOM — {Label(src, a.sourceOwnerSide)} poison-kills {Label(tgt, a.targetOwnerSide)}");
                        if (tgt != null) tgt.hp = 0;
                        break;
                    }

                    case CombatActionType.AbilityTrigger:
                    {
                        var src = Resolve(side0, side1, a.sourceCardIndex, a.sourceOwnerSide, anomalies, step, "ability source");
                        sb.AppendLine($"{t} {++step}. ABILITY — {Label(src, a.sourceOwnerSide)} triggers {a.abilityName}");
                        break;
                    }

                    case CombatActionType.BuffApplied:
                    {
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "buff target");
                        sb.AppendLine($"{t} {++step}. BUFF — {Label(tgt, a.targetOwnerSide)} gains +{a.value}");
                        break;
                    }

                    case CombatActionType.EchoTrigger:
                    {
                        var src = Resolve(side0, side1, a.sourceCardIndex, a.sourceOwnerSide, anomalies, step, "echo source");
                        var tgt = Resolve(side0, side1, a.targetCardIndex, a.targetOwnerSide, anomalies, step, "echo target");
                        sb.AppendLine($"{t} {++step}. ECHO — {Label(src, a.sourceOwnerSide)} echoes +{a.value} to {Label(tgt, a.targetOwnerSide)}");
                        break;
                    }

                    case CombatActionType.SummonToken:
                    {
                        var list = a.targetOwnerSide == 0 ? side0 : side1;
                        var tok = new Entry { name = string.IsNullOrEmpty(a.abilityName) ? "Token" : a.abilityName, atk = a.value, hp = a.value };
                        int at = Mathf.Clamp(a.targetCardIndex, 0, list.Count);
                        list.Insert(at, tok);
                        sb.AppendLine($"{t} {++step}. SUMMON — {tok.name} ({a.value}/{a.value}) appears at position {at} ({SideName(a.targetOwnerSide, atkName, defName)}; position approximate)");
                        break;
                    }

                    case CombatActionType.CombatEnd:
                        sb.AppendLine($"{t} — **Combat ends**");
                        break;

                    default:
                        sb.AppendLine($"{t} {++step}. {a.type} (src {a.sourceOwnerSide}/{a.sourceCardIndex}, tgt {a.targetOwnerSide}/{a.targetCardIndex}, value {a.value})");
                        break;
                }
            }

            // --- Result + expected final boards ---
            sb.AppendLine();
            sb.AppendLine("## Result (per the sim)");
            sb.AppendLine();
            if (replay.result != null)
            {
                sb.AppendLine($"- **Winner:** {replay.result.winnerName} ({replay.result.winnerSide})");
                sb.AppendLine($"- **Damage dealt:** {replay.result.damageDealt} (current formula: surviving-minion count + winner tavern tier — T835)");
                int survCount = replay.result.survivingCards != null ? replay.result.survivingCards.Count : 0;
                sb.AppendLine($"- **Recorded survivors:** {survCount}");
                if (replay.result.survivingCards != null)
                    foreach (var s in replay.result.survivingCards)
                        if (s != null) sb.AppendLine($"  - {s.cardName} {s.attack}/{s.health}{(s.isGolden ? " (golden)" : "")}");
            }
            else
            {
                sb.AppendLine("- (no result recorded)");
            }

            sb.AppendLine();
            sb.AppendLine("## What the screen SHOULD show at the end");
            sb.AppendLine();
            sb.AppendLine("Walked from the action list; compare with what you actually saw (T843).");
            sb.AppendLine();
            AppendEntries(sb, atkName + " (side 0)", side0);
            AppendEntries(sb, defName + " (side 1)", side1);

            // --- Anomalies ---
            sb.AppendLine("## Anomalies");
            sb.AppendLine();
            if (anomalies.Count == 0 && actionsAfterFirstDeath == 0)
            {
                sb.AppendLine("None detected.");
            }
            else
            {
                foreach (var an in anomalies) sb.AppendLine($"- WARNING {an}");
                if (actionsAfterFirstDeath > 0)
                    sb.AppendLine($"- NOTE {actionsAfterFirstDeath} action(s) happen after the first board-shrinking death. " +
                        "The sim records indices into the shrunk board, but the on-screen animator indexes fixed visual lists (T847) — " +
                        "these actions may have animated on the WRONG cards even though this transcript (and the sim) are correct.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Write the transcript under AutoPlaytest/transcripts/ (outside Assets, gitignored).
        /// Never throws — combat must not break because a disk write failed.
        /// </summary>
        public static string WriteToDisk(CombatReplay replay, string label, string header = null)
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", "..", "AutoPlaytest", "transcripts"));
                Directory.CreateDirectory(dir);

                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string safeLabel = Sanitize(string.IsNullOrEmpty(label) ? "combat" : label);
                string path = Path.Combine(dir, $"{stamp}_{safeLabel}.md");

                File.WriteAllText(path, Generate(replay, header));
                Debug.Log($"[CombatTranscript] {safeLabel} -> {path}");
                return path;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CombatTranscript] Failed to write transcript: {ex.Message}");
                return null;
            }
        }

        // ----------------------------------------------------------------

        private static List<Entry> BuildEntries(List<CombatCardSnapshot> board)
        {
            var list = new List<Entry>();
            if (board == null) return list;
            foreach (var s in board)
            {
                if (s == null) continue;
                list.Add(new Entry
                {
                    name = s.cardName,
                    atk = s.attack,
                    hp = s.health,
                    aegis = s.hasAegis,
                    reborn = s.hasReborn,
                    golden = s.isGolden
                });
            }
            return list;
        }

        private static Entry Resolve(List<Entry> side0, List<Entry> side1, int index, int side,
            List<string> anomalies, int step, string role)
        {
            if (side != 0 && side != 1)
            {
                if (index >= 0) anomalies.Add($"step {step + 1}: {role} has invalid side {side}");
                return null;
            }
            var list = side == 0 ? side0 : side1;
            if (index < 0 || index >= list.Count)
            {
                anomalies.Add($"step {step + 1}: {role} index {index} out of range (side {side} has {list.Count} cards) — recorder/board desync");
                return null;
            }
            return list[index];
        }

        private static string Label(Entry e, int side)
        {
            if (e == null) return $"<unknown card, side {side}>";
            string keywords = "";
            if (e.aegis) keywords += " [Aegis]";
            if (e.reborn) keywords += " [Reborn]";
            if (e.golden) keywords += " [Golden]";
            return $"**{e.name}** ({e.atk}/{e.hp}){keywords}";
        }

        private static string SideName(int side, string atkName, string defName) =>
            side == 0 ? atkName : side == 1 ? defName : $"side {side}";

        private static void AppendBoard(StringBuilder sb, string title, List<CombatCardSnapshot> board)
        {
            sb.AppendLine($"**{title}**");
            sb.AppendLine();
            if (board == null || board.Count == 0)
            {
                sb.AppendLine("- (empty board)");
            }
            else
            {
                for (int i = 0; i < board.Count; i++)
                {
                    var s = board[i];
                    if (s == null) continue;
                    string kw = "";
                    if (s.hasTaunt) kw += " Taunt";
                    if (s.hasAegis) kw += " Aegis";
                    if (s.hasReborn) kw += " Reborn";
                    if (s.hasWindfury) kw += " Windfury";
                    if (s.hasVenomous) kw += " Venomous";
                    if (s.armor > 0) kw += $" Armor:{s.armor}";
                    if (s.isGolden) kw += " GOLDEN";
                    string tribes = s.tribeNames != null && s.tribeNames.Length > 0
                        ? " — " + string.Join("/", s.tribeNames) : "";
                    sb.AppendLine($"- [{i}] {s.cardName} {s.attack}/{s.health}{(kw.Length > 0 ? " (" + kw.Trim() + ")" : "")}{tribes}");
                }
            }
            sb.AppendLine();
        }

        private static void AppendEntries(StringBuilder sb, string title, List<Entry> list)
        {
            sb.AppendLine($"**{title}**");
            sb.AppendLine();
            bool any = false;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.hp <= 0) continue;
                any = true;
                sb.AppendLine($"- {e.name} {e.atk}/{e.hp}{(e.golden ? " (golden)" : "")}");
            }
            if (!any) sb.AppendLine("- (board empty)");
            sb.AppendLine();
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            return sb.ToString();
        }
    }
}

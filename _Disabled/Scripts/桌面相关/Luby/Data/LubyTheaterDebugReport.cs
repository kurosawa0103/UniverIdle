using System.Collections.Generic;
using System.Text;
using DesktopPet.AI;
using DesktopPet.Decor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DesktopPet.Luby
{
    /// <summary>小剧场调试报告（AI 行为日志 · 小剧场页签）。</summary>
    public static class LubyTheaterDebugReport
    {
        [System.Flags]
        public enum Section
        {
            None = 0,
            EventConfig = 1 << 0,
            SceneHosts = 1 << 1,
            Props = 1 << 2,
            CastSlots = 1 << 3,
            LubiesOnDesk = 1 << 4,
            Gates = 1 << 5,
            LiveSession = 1 << 6,
            All = ~0
        }

        private static readonly List<LubyTheaterDirector.TheaterSessionSnapshot> SnapshotsScratch =
            new List<LubyTheaterDirector.TheaterSessionSnapshot>(4);

        private static readonly Dictionary<LubyInstanceComponent, string> LubyLabelScratch =
            new Dictionary<LubyInstanceComponent, string>(8);

        private struct ReportCache
        {
            public bool SlotsFull;
            public bool HasSnapshots;
            public bool EvalReady;
            public bool CanStart;
            public string EvalSummary;
        }

        public static string Build(
            LubyTheaterEventDefinition evt,
            Section sections = Section.All,
            LubyTheaterDirector director = null,
            bool ignoreGates = true,
            bool ignoreCooldown = true)
        {
            if (evt == null)
                return "（未指定事件）";

            var sb = new StringBuilder(640);
            LubyWorld world = ResolveWorld();
            DecorWorld decor = ResolveDecor();
            var lubies = CollectLubies(world);
            var placed = CollectPlaced(decor);

            var cache = new ReportCache
            {
                SlotsFull = Application.isPlaying && director != null &&
                            director.ActiveSessionCount >= director.MaxConcurrentSessions
            };

            if (Application.isPlaying && director != null && director.HasActiveSession)
            {
                CopySnapshots(director);
                cache.HasSnapshots = true;
            }

            bool needEval = Application.isPlaying && director != null &&
                            (sections.HasFlag(Section.CastSlots) ||
                             sections.HasFlag(Section.Gates) ||
                             !director.HasActiveSession);
            if (needEval)
            {
                cache.EvalReady = true;
                cache.CanStart = director.TryEvaluateEvent(evt, ignoreGates, ignoreCooldown, out cache.EvalSummary);
            }

            AppendHeadline(sb, evt, director, cache);
            if (Application.isPlaying && sections.HasFlag(Section.LiveSession))
                AppendLiveSession(sb, director, evt, cache);
            if (Application.isPlaying && (sections.HasFlag(Section.CastSlots) || sections.HasFlag(Section.Gates)))
                AppendStartCheck(sb, director, cache, ignoreGates);

            if (sections.HasFlag(Section.Props))
                AppendProps(sb, evt, placed);

            if (sections.HasFlag(Section.LubiesOnDesk))
                AppendLubies(sb, lubies, director);

            if (sections.HasFlag(Section.SceneHosts))
                AppendSceneHosts(sb, world, decor, director);

            if (sections.HasFlag(Section.EventConfig))
                AppendEventConfig(sb, evt);

            return sb.ToString().TrimEnd();
        }

        private static void CopySnapshots(LubyTheaterDirector director)
        {
            SnapshotsScratch.Clear();
            if (director != null)
                director.CopyActiveSessionSummaries(SnapshotsScratch);
        }

        private static void AppendHeadline(
            StringBuilder sb,
            LubyTheaterEventDefinition evt,
            LubyTheaterDirector director,
            ReportCache cache)
        {
            AppendSectionTitle(sb, "状态");
            if (!Application.isPlaying)
            {
                AppendMuted(sb, "Edit 模式 → 进 Play 再看");
                return;
            }

            if (director == null)
            {
                AppendBad(sb, "× 无 Director");
                return;
            }

            if (director.HasActiveSession)
            {
                if (!cache.HasSnapshots)
                    CopySnapshots(director);

                int max = director.MaxConcurrentSessions;
                int count = SnapshotsScratch.Count;
                int locked = 0;
                for (int i = 0; i < SnapshotsScratch.Count; i++)
                    locked += SnapshotsScratch[i].CastCount;

                sb.AppendLine(C(Accent, $"{count}/{max} 场") + C(Muted, $" · 已锁 {locked} 只 Luby"));
                if (count < max)
                    AppendHint(sb, "↓ 还可新开 · 各场演员见「场次详情」");
                else
                    AppendHint(sb, "↓ 已满员 · 「能否新开一场」× 为正常");
                return;
            }

            if (cache.EvalReady && cache.CanStart)
                AppendOk(sb, "空闲 · 可开演（强制 / 等扫描）");
            else if (cache.EvalReady)
                AppendWarn(sb, "空闲 · 暂不能开演 → 看「能否新开一场」");
            else
                AppendMuted(sb, "空闲");
        }

        private static void AppendStartCheck(
            StringBuilder sb,
            LubyTheaterDirector director,
            ReportCache cache,
            bool ignoreGates)
        {
            sb.AppendLine();
            AppendSectionTitle(sb, "能否新开一场");
            if (director != null)
            {
                sb.AppendLine(C(Muted, "槽位 ") + C(Accent, $"{director.ActiveSessionCount}/{director.MaxConcurrentSessions}"));
                if (director.HasActiveSession)
                {
                    if (!cache.HasSnapshots)
                        CopySnapshots(director);

                    sb.Append(C(Muted, "已占用："));
                    for (int i = 0; i < SnapshotsScratch.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(C(Muted, " · "));
                        var snap = SnapshotsScratch[i];
                        sb.Append(C(SessionAccent(snap.DisplayIndex), $"场{snap.DisplayIndex}") +
                                  C(Muted, $"({snap.CastCount}人)"));
                    }
                    sb.AppendLine();
                }
            }
            if (cache.SlotsFull)
                AppendWarn(sb, "已满员 → 不能再开（正常）· 重测请先「结束全部」");

            if (cache.EvalReady && cache.CanStart)
                AppendOk(sb, "结论：✓ 可新开");
            else if (cache.EvalReady)
                AppendBad(sb, "结论：× 不可新开");
            else
                AppendMuted(sb, "结论：（未评估）");

            AppendGates(sb, ignoreGates, cache.SlotsFull, director);
            if (cache.EvalReady && !string.IsNullOrEmpty(cache.EvalSummary))
            {
                if (!cache.SlotsFull)
                    AppendSubTitle(sb, "演员评估");
                AppendColoredEvalSummary(sb, cache.EvalSummary);
            }
        }

        private static void AppendLiveSession(StringBuilder sb, LubyTheaterDirector director, LubyTheaterEventDefinition selected, ReportCache cache)
        {
            sb.AppendLine();
            AppendSectionTitle(sb, "场次详情");
            if (director == null || !director.HasActiveSession)
            {
                AppendMuted(sb, "（无进行中场次）");
                return;
            }

            if (!cache.HasSnapshots)
                CopySnapshots(director);

            int total = SnapshotsScratch.Count;
            sb.AppendLine(C(Muted, "共 ") + C(Accent, $"{total}/{director.MaxConcurrentSessions}") + C(Muted, " 场"));
            sb.AppendLine();

            for (int si = 0; si < SnapshotsScratch.Count; si++)
            {
                if (si > 0)
                    sb.AppendLine();

                AppendSessionDetailBlock(sb, SnapshotsScratch[si], total, selected?.eventId);
            }
        }

        private static void AppendSessionDetailBlock(
            StringBuilder sb,
            LubyTheaterDirector.TheaterSessionSnapshot snap,
            int total,
            string selectedEventId)
        {
            string accent = SessionAccent(snap.DisplayIndex);
            sb.AppendLine(B(C(accent, $"── 场 {snap.DisplayIndex}/{total} · #{snap.SessionId} · {snap.EventId} ──")));

            if (!string.IsNullOrEmpty(selectedEventId) && snap.EventId != selectedEventId)
                AppendWarn(sb, $"（下拉选中的是 [{selectedEventId}]，与本场无关）");

            string phaseHint = snap.PhaseLabel == "走位中" ? "全员到点 → 切表演" : "到时结束 → 写冷却";
            sb.AppendLine(
                C(Muted, "阶段：") +
                C(PhaseColor(snap.PhaseLabel), snap.PhaseLabel) +
                C(Muted, $" · 剩余 {snap.RemainingSeconds:0.0}s · {phaseHint}"));
            sb.AppendLine(C(Muted, $"演员（{snap.CastCount}）："));

            if (snap.CastLines == null || snap.CastLines.Count == 0)
            {
                AppendMuted(sb, "  （无）");
                return;
            }

            for (int i = 0; i < snap.CastLines.Count; i++)
                sb.AppendLine(FormatCastLine(snap.CastLines[i], accent));
        }

        private static string FormatCastLine(string pipeLine, string sessionAccent)
        {
            if (string.IsNullOrEmpty(pipeLine))
                return C(Muted, "  ?");

            string[] p = pipeLine.Split('|');
            if (p.Length < 4)
                return C(Muted, "  " + pipeLine);

            string role = p[0].PadRight(8);
            string name = p[1].PadRight(12);
            string stateColor = p[2] == "已到" ? Ok : Warn;
            string state = C(stateColor, p[2].PadRight(4));
            string facing = p.Length >= 5 ? C(Muted, $"  朝向{p[4]}") : string.Empty;
            return C(Muted, "  ") +
                   C(sessionAccent, role) +
                   C(Muted, name) +
                   state +
                   C(Muted, $"  目标X {p[3]}") +
                   facing;
        }

        private static void AppendProps(StringBuilder sb, LubyTheaterEventDefinition evt, List<PlacedDecor> placed)
        {
            sb.AppendLine();
            AppendSectionTitle(sb, "道具");
            if (evt.requiredProps == null || evt.requiredProps.Count == 0)
            {
                AppendMuted(sb, "（无要求）");
                return;
            }

            for (int i = 0; i < evt.requiredProps.Count; i++)
            {
                LubyTheaterPropRequirement req = evt.requiredProps[i];
                string itemId = req?.ResolveItemId();
                if (string.IsNullOrEmpty(itemId))
                    continue;

                int need = Mathf.Max(1, req.minCount);
                int count = 0;
                for (int p = 0; p < placed.Count; p++)
                {
                    if (placed[p] != null && placed[p].ItemId == itemId)
                        count++;
                }

                if (count >= need)
                    sb.AppendLine(C(Ok, "✓ ") + C(Muted, $"{itemId} {count}/{need}"));
                else
                    sb.AppendLine(C(Bad, "× ") + C(Warn, $"{itemId} {count}/{need}"));

                for (int p = 0; p < placed.Count; p++)
                {
                    PlacedDecor d = placed[p];
                    if (d == null || d.ItemId != itemId)
                        continue;
                    float ax = LubyTheaterStaging.GetPropAnchorWorld(d).x;
                    sb.AppendLine(C(Muted, $"    {d.name} anchorX={ax:0.##}"));
                }
            }
        }

        private static void AppendLubies(StringBuilder sb, List<LubyInstanceComponent> lubies, LubyTheaterDirector director)
        {
            sb.AppendLine();
            int free = 0;
            int locked = 0;
            int busy = 0;
            LubyLabelScratch.Clear();
            if (director != null && Application.isPlaying)
                director.FillLubyTheaterLabels(LubyLabelScratch);

            for (int i = 0; i < lubies.Count; i++)
            {
                LubyInstanceComponent l = lubies[i];
                if (director != null && director.IsLubyInTheater(l))
                    locked++;
                else if (Application.isPlaying && DesktopPetServices.IsLubyExternallyBusy(l))
                    busy++;
                else
                    free++;
            }

            sb.AppendLine(B(C(TitleColor, $"■ Luby（{lubies.Count}") + C(Muted, $" · ") +
                          C(Ok, $"空闲{free}") + C(Muted, " / ") +
                          C(Accent, $"锁定{locked}") + C(Muted, " / ") +
                          C(Warn, $"交互{busy}") + C(TitleColor, "）")));
            if (lubies.Count == 0)
            {
                AppendMuted(sb, Application.isPlaying ? "（无）" : "（Edit 通常为空）");
                return;
            }

            for (int i = 0; i < lubies.Count; i++)
            {
                LubyInstanceComponent l = lubies[i];
                string pid = l.Personality != null ? l.Personality.personalityId : l.Data?.personalityId ?? "?";
                string tid = LubyTraitDisplay.FormatIds(l.Data, empty: "—");
                PetBrain brain = l.Agent?.Brain ?? l.GetComponent<PetBrain>();

                string stateColored;
                if (LubyLabelScratch.TryGetValue(l, out string theaterLabel))
                {
                    int fieldIdx = theaterLabel.IndexOf('·');
                    string sessionPart = fieldIdx > 0 ? theaterLabel.Substring(0, fieldIdx) : theaterLabel;
                    string rolePart = fieldIdx > 0 ? theaterLabel.Substring(fieldIdx) : "";
                    int sessionNum = 0;
                    if (sessionPart.StartsWith("场") && sessionPart.Length > 1)
                        int.TryParse(sessionPart.Substring(1), out sessionNum);
                    stateColored = C(SessionAccent(sessionNum > 0 ? sessionNum : 1), "锁定") +
                                   C(Muted, rolePart);
                }
                else if (Application.isPlaying && DesktopPetServices.IsLubyExternallyBusy(l))
                {
                    stateColored = C(Warn, "交互/捡币");
                }
                else
                {
                    stateColored = C(Ok, "空闲");
                }

                sb.AppendLine(C(Muted, $"{LubyTheaterStaging.ShortLubyName(l)} · {pid} · {tid} · X={l.transform.position.x:0.##} · ") + stateColored);
                if (brain != null)
                {
                    string cur = string.IsNullOrEmpty(brain.CurrentBehaviorId) ? "—" : brain.CurrentBehaviorId;
                    sb.AppendLine(C(Muted, $"  行为 ") + C(Accent, cur));
                }
            }
        }

        private static void AppendSceneHosts(StringBuilder sb, LubyWorld world, DecorWorld decor, LubyTheaterDirector director)
        {
            sb.AppendLine();
            AppendSectionTitle(sb, "宿主");
            sb.AppendLine(
                C(Muted, "LubyWorld ") + GateYn(world != null) +
                C(Muted, " · DecorWorld ") + GateYn(decor != null) +
                C(Muted, " · Director ") + GateYn(director != null));
        }

        private static string GateYn(bool ok) => ok ? C(Ok, "✓") : C(Bad, "×");

        private static void AppendEventConfig(StringBuilder sb, LubyTheaterEventDefinition evt)
        {
            sb.AppendLine();
            AppendSectionTitle(sb, "配置");
            sb.AppendLine(C(Muted, $"{evt.eventId} w{evt.weight} 命中{evt.scanChance:P0} 冷却{evt.cooldownSeconds}s 演{evt.durationSeconds}s"));
            string stage = evt.ResolveStagePropItemId();
            string stageLabel;
            if (!string.IsNullOrEmpty(stage))
                stageLabel = stage;
            else if (evt.seekPartnerFirst)
                stageLabel = "寻访→中点";
            else
                stageLabel = "中点站位";
            string span = evt.maxCastSpanX > 0f ? $"跨度≤{evt.maxCastSpanX:0.##}" : "跨度不限";
            sb.AppendLine(C(Muted, $"走位 v{evt.stageMoveSpeed} 到点{evt.stageArriveDistance} 超时{evt.stageApproachTimeout}s · {stageLabel} · {span}"));
        }

        private static void AppendGates(StringBuilder sb, bool ignoreGates, bool slotsFull, LubyTheaterDirector director)
        {
            AppendSubTitle(sb, "门闸");
            if (slotsFull && director != null)
                AppendWarn(sb, $"已满员 {director.ActiveSessionCount}/{director.MaxConcurrentSessions}");
            if (!ignoreGates)
            {
                AppendGateLine(sb, !DesktopPetServices.IsAnyPlacementHolding(), "无手持", "手持放置");
                AppendGateLine(sb, !DesktopPetServices.IsHubOpen(), "Hub关", "Hub开");
            }
            else if (!slotsFull)
            {
                AppendMuted(sb, "Hub/手持已忽略");
            }
        }

        private static void AppendGateLine(StringBuilder sb, bool ok, string okText, string badText)
        {
            if (ok)
                AppendOk(sb, $"✓ {okText}");
            else
                AppendBad(sb, $"× {badText}");
        }

        /// <summary>给纯文本评估报告加色（开演失败时）。</summary>
        public static string ColorizePlainReport(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var sb = new StringBuilder(text.Length + 64);
            AppendColoredEvalSummary(sb, text);
            return sb.ToString().TrimEnd();
        }

        private static bool EditorDark
        {
            get
            {
#if UNITY_EDITOR
                return EditorGUIUtility.isProSkin;
#else
                return true;
#endif
            }
        }

        private static string TitleColor => EditorDark ? "#5ECFFF" : "#0066AA";
        private static string Accent => EditorDark ? "#67E8F9" : "#0088AA";
        private static string Ok => EditorDark ? "#7DDE8A" : "#1A7A1A";
        private static string Bad => EditorDark ? "#FF7070" : "#CC2222";
        private static string Warn => EditorDark ? "#FFD56B" : "#996600";
        private static string Muted => EditorDark ? "#909090" : "#666666";

        private static string C(string hex, string text) => $"<color={hex}>{text}</color>";
        private static string B(string text) => $"<b>{text}</b>";

        private static string SessionAccent(int index)
        {
            switch (index)
            {
                case 1: return EditorDark ? "#C4A1FF" : "#6633AA";
                case 2: return EditorDark ? "#67E8F9" : "#007799";
                case 3: return EditorDark ? "#F9A8D4" : "#AA3366";
                default: return Accent;
            }
        }

        private static string PhaseColor(string phase)
        {
            if (phase == "寻访中" || phase == "走位中")
                return Warn;
            if (phase == "对方走开")
                return Bad;
            return Ok;
        }

        private static void AppendSectionTitle(StringBuilder sb, string title) =>
            sb.AppendLine(B(C(TitleColor, "■ " + title)));

        private static void AppendSubTitle(StringBuilder sb, string title) =>
            sb.AppendLine(C(Muted, "— ") + C(TitleColor, title) + C(Muted, " —"));

        private static void AppendOk(StringBuilder sb, string text) => sb.AppendLine(C(Ok, text));
        private static void AppendBad(StringBuilder sb, string text) => sb.AppendLine(C(Bad, text));
        private static void AppendWarn(StringBuilder sb, string text) => sb.AppendLine(C(Warn, text));
        private static void AppendMuted(StringBuilder sb, string text) => sb.AppendLine(C(Muted, text));
        private static void AppendHint(StringBuilder sb, string text) => sb.AppendLine(C(Muted, text));

        private static void AppendColoredEvalSummary(StringBuilder sb, string evalSummary)
        {
            string[] lines = evalSummary.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                sb.AppendLine(ColorizeMarkerLine(lines[i]));
        }

        private static string ColorizeMarkerLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return line;

            string t = line.TrimStart();
            if (t.StartsWith("✓"))
                return C(Ok, line);
            if (t.StartsWith("×"))
                return C(Bad, line);
            if (t.StartsWith("·") || line.StartsWith("  ·") || (line.StartsWith("  ") && t.Length > 0 && t[0] != '×' && t[0] != '✓'))
                return C(Muted, line);
            if (t.StartsWith("可以开演"))
                return B(C(Ok, line));
            if (t.StartsWith("暂不能开演"))
                return B(C(Bad, line));
            return line;
        }

        private static LubyWorld ResolveWorld() => DesktopPetServices.LubyWorld;

        private static DecorWorld ResolveDecor() => DesktopPetServices.DecorWorld;

        private static List<LubyInstanceComponent> CollectLubies(LubyWorld world)
        {
            var list = new List<LubyInstanceComponent>(8);
            if (world == null)
                return list;

            var instances = world.Instances;
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null && instances[i].isActiveAndEnabled)
                    list.Add(instances[i]);
            }

            return list;
        }

        private static List<PlacedDecor> CollectPlaced(DecorWorld decor)
        {
            var list = new List<PlacedDecor>(16);
            if (decor == null)
                return list;

            var placed = decor.Placed;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i] != null)
                    list.Add(placed[i]);
            }

            return list;
        }
    }
}

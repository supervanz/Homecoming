using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Homecoming.Bot
{
    /// <summary>
    /// 시뮬레이션 결과 저장: 판별 CSV(runs.csv), 요약(summary.md), 비교용 수치(summary.json).
    /// 같은 프리셋의 직전 리포트가 있으면 변경 전후 비교표를 함께 쓴다 (11-6).
    /// </summary>
    public static class SimulationReport
    {
        [Serializable]
        public class SkillSummary
        {
            public string skill;
            public int runs;
            public int clears;
            public float clearRate;
            public float avgClearTime = -1f;
            public float avgHits;
            public float avgBombs;
            public float avgScore;
            public float avgLv2Time = -1f;
            public float avgLv3Time = -1f;
            public float avgPhase1 = -1f;
            public float avgPhase2 = -1f;
            public float avgBossFight = -1f;
            public int timeouts;
            public float targetMin;
            public float targetMax;
        }

        [Serializable]
        public class Summary
        {
            public string createdAt;
            public string preset;
            public string label;
            public int baseSeed;
            public float realSeconds;
            /// <summary>실행 당시 프리셋 값 (JSON). 리포트끼리 어떤 값을 바꿨는지 비교할 때 쓴다 (11-6)</summary>
            public string presetJson;
            public List<SkillSummary> skills = new List<SkillSummary>();
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Write(SimulationConfig cfg, List<RunResult> results, float realSeconds)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", cfg.outputFolder));
            string preset = cfg.preset != null ? cfg.preset.name : "none";
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", Inv);
            string name = $"{stamp}_{preset}" + (string.IsNullOrWhiteSpace(cfg.label) ? "" : "_" + Sanitize(cfg.label));
            string dir = Path.Combine(root, name);
            Directory.CreateDirectory(dir);

            Summary previous = FindPrevious(root, preset, dir);
            Summary summary = BuildSummary(cfg, results, preset);
            summary.realSeconds = realSeconds;
            summary.presetJson = cfg.preset != null ? JsonUtility.ToJson(cfg.preset) : "";

            File.WriteAllText(Path.Combine(dir, "runs.csv"), BuildCsv(results), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(dir, "summary.json"), JsonUtility.ToJson(summary, true), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(dir, "summary.md"), BuildMarkdown(cfg, results, summary, previous, realSeconds), new UTF8Encoding(false));
            return dir;
        }

        private static string Sanitize(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }

        private static Summary FindPrevious(string root, string preset, string current)
        {
            if (!Directory.Exists(root)) return null;
            var dirs = Directory.GetDirectories(root)
                .Where(d => d != current && Path.GetFileName(d).Contains("_" + preset) && File.Exists(Path.Combine(d, "summary.json")))
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToList();
            if (dirs.Count == 0) return null;
            try { return JsonUtility.FromJson<Summary>(File.ReadAllText(Path.Combine(dirs[dirs.Count - 1], "summary.json"))); }
            catch { return null; }
        }

        private static float AvgOf(IEnumerable<float> values)
        {
            var list = values.Where(v => v >= 0f).ToList();
            return list.Count > 0 ? list.Average() : -1f;
        }

        private static Summary BuildSummary(SimulationConfig cfg, List<RunResult> results, string preset)
        {
            var s = new Summary { createdAt = DateTime.Now.ToString("s", Inv), preset = preset, label = cfg.label, baseSeed = cfg.baseSeed };
            foreach (BotSkill skill in cfg.skills)
            {
                var rs = results.Where(r => r.skill == skill.name).ToList();
                if (rs.Count == 0) continue;
                int clears = rs.Count(r => r.cleared);
                s.skills.Add(new SkillSummary
                {
                    skill = skill.name,
                    runs = rs.Count,
                    clears = clears,
                    clearRate = clears / (float)rs.Count,
                    avgClearTime = AvgOf(rs.Where(r => r.cleared).Select(r => r.endTime)),
                    avgHits = (float)rs.Average(r => r.hitSources.Count),
                    avgBombs = (float)rs.Average(r => r.bombTimes.Count),
                    avgScore = (float)rs.Average(r => r.score),
                    avgLv2Time = AvgOf(rs.Select(r => r.lv2Time)),
                    avgLv3Time = AvgOf(rs.Select(r => r.lv3Time)),
                    avgPhase1 = AvgOf(rs.Select(r => r.Phase1Duration)),
                    avgPhase2 = AvgOf(rs.Select(r => r.Phase2Duration)),
                    avgBossFight = AvgOf(rs.Select(r => r.BossFightDuration)),
                    timeouts = rs.Count(r => r.timedOut),
                    targetMin = skill.targetClearRateMin,
                    targetMax = skill.targetClearRateMax,
                });
            }
            return s;
        }

        private static string F(float v, string fmt = "0.0") => v < 0f ? "-" : v.ToString(fmt, Inv);
        private static string Pct(float v) => (v * 100f).ToString("0", Inv) + "%";
        private static string Mmss(float s) => s < 0f ? "-" : $"{(int)s / 60}:{(int)s % 60:00}";
        private static string Csv(string s) => s == null ? "" : (s.Contains(",") || s.Contains("\"") || s.Contains(";") ? "\"" + s.Replace("\"", "\"\"") + "\"" : s);

        private static string BuildCsv(List<RunResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("run,skill,preset,seed,cleared,timed_out,end_time,end_section,death_cause,score,hits,hit_sources,hit_times,bombs,bomb_times,lv2_time,lv3_time,boss_spawn,boss_phase1,boss_transition,boss_phase2,boss_desperation,boss_defeated,phase1_duration,phase2_duration");
            foreach (var r in results)
            {
                sb.Append(r.run).Append(',').Append(Csv(r.skill)).Append(',').Append(Csv(r.preset)).Append(',').Append(r.seed).Append(',')
                  .Append(r.cleared ? 1 : 0).Append(',').Append(r.timedOut ? 1 : 0).Append(',').Append(F(r.endTime, "0.00")).Append(',')
                  .Append(Csv(r.endSection)).Append(',').Append(Csv(r.deathCause)).Append(',').Append(r.score).Append(',')
                  .Append(r.hitSources.Count).Append(',').Append(Csv(string.Join(";", r.hitSources))).Append(',')
                  .Append(Csv(string.Join(";", r.hitTimes.Select(t => F(t, "0.0"))))).Append(',')
                  .Append(r.bombTimes.Count).Append(',').Append(Csv(string.Join(";", r.bombTimes.Select(t => F(t, "0.0"))))).Append(',')
                  .Append(F(r.lv2Time)).Append(',').Append(F(r.lv3Time)).Append(',')
                  .Append(F(r.bossSpawn)).Append(',').Append(F(r.bossPhase1)).Append(',').Append(F(r.bossTransition)).Append(',')
                  .Append(F(r.bossPhase2)).Append(',').Append(F(r.bossDesperation)).Append(',').Append(F(r.bossDefeated)).Append(',')
                  .Append(F(r.Phase1Duration)).Append(',').Append(F(r.Phase2Duration)).AppendLine();
            }
            return sb.ToString();
        }

        private static string Judge(float value, float min, float max)
        {
            if (value < 0f) return "-";
            if (value < min) return "below";
            if (value > max) return "above";
            return "in range";
        }

        private static string BuildMarkdown(SimulationConfig cfg, List<RunResult> results, Summary s, Summary previous, float realSeconds)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Balance report: {s.preset}{(string.IsNullOrWhiteSpace(s.label) ? "" : " (" + s.label + ")")}");
            sb.AppendLine();
            sb.AppendLine($"- Created: {s.createdAt}");
            sb.AppendLine($"- Runs: {results.Count} ({cfg.runsPerSkill} per skill), seeds {cfg.baseSeed}-{cfg.baseSeed + cfg.runsPerSkill - 1}");
            sb.AppendLine($"- Real time: {realSeconds:0}s");
            sb.AppendLine();

            sb.AppendLine("## Clear rate and time (target: CLAUDE.md 11-5)");
            sb.AppendLine();
            sb.AppendLine("| Skill | Runs | Clear rate | Target | Judge | Avg clear time | Time judge | Avg hits | Avg bombs | Avg score | Timeouts |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var k in s.skills)
                sb.AppendLine($"| {k.skill} | {k.runs} | {Pct(k.clearRate)} | {Pct(k.targetMin)}-{Pct(k.targetMax)} | {Judge(k.clearRate, k.targetMin, k.targetMax)} | {Mmss(k.avgClearTime)} | {Judge(k.avgClearTime, cfg.targetClearTimeMin, cfg.targetClearTimeMax)} | {F(k.avgHits)} | {F(k.avgBombs)} | {k.avgScore:0} | {k.timeouts} |");
            sb.AppendLine();

            sb.AppendLine("## Growth and boss phases (avg, runs that reached it)");
            sb.AppendLine();
            sb.AppendLine("| Skill | Weapon Lv2 at | Weapon Lv3 at | Boss phase 1 | Boss phase 2 (incl. desperation) | Boss fight total |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var k in s.skills)
                sb.AppendLine($"| {k.skill} | {Mmss(k.avgLv2Time)} | {Mmss(k.avgLv3Time)} | {F(k.avgPhase1)}s | {F(k.avgPhase2)}s | {F(k.avgBossFight)}s |");
            sb.AppendLine();

            var sections = results.Where(r => !r.cleared).Select(r => r.endSection).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            sb.AppendLine("## Deaths by section (CLAUDE.md 12-5)");
            sb.AppendLine();
            if (sections.Count == 0) sb.AppendLine("No deaths.");
            else
            {
                sb.AppendLine("| Section | " + string.Join(" | ", s.skills.Select(k => k.skill)) + " |");
                sb.AppendLine("|---|" + string.Concat(s.skills.Select(_ => "---|")));
                foreach (var sec in sections)
                    sb.AppendLine($"| {sec} | " + string.Join(" | ", s.skills.Select(k => results.Count(r => r.skill == k.skill && !r.cleared && r.endSection == sec))) + " |");
                sb.AppendLine();
                foreach (var k in s.skills)
                {
                    var deaths = results.Where(r => r.skill == k.skill && !r.cleared).ToList();
                    if (deaths.Count < 3) continue;
                    var top = deaths.GroupBy(r => r.endSection).OrderByDescending(g => g.Count()).First();
                    float share = top.Count() / (float)deaths.Count;
                    if (share >= cfg.deathConcentrationWarn)
                        sb.AppendLine($"- **Warning:** {k.skill} deaths concentrate in \"{top.Key}\" ({Pct(share)} of {deaths.Count}).");
                }
            }
            sb.AppendLine();

            sb.AppendLine("## Deaths by cause (pattern)");
            sb.AppendLine();
            var causes = results.Where(r => !r.cleared).GroupBy(r => r.deathCause).OrderByDescending(g => g.Count()).ToList();
            if (causes.Count == 0) sb.AppendLine("No deaths.");
            else
            {
                sb.AppendLine("| Cause | " + string.Join(" | ", s.skills.Select(k => k.skill)) + " | Total |");
                sb.AppendLine("|---|" + string.Concat(s.skills.Select(_ => "---|")) + "---|");
                foreach (var g in causes)
                    sb.AppendLine($"| {g.Key} | " + string.Join(" | ", s.skills.Select(k => g.Count(r => r.skill == k.skill))) + $" | {g.Count()} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Hits by cause (all hits, top 10)");
            sb.AppendLine();
            var hits = results.SelectMany(r => r.hitSources).GroupBy(x => x).OrderByDescending(g => g.Count()).Take(10).ToList();
            if (hits.Count == 0) sb.AppendLine("No hits.");
            else
            {
                sb.AppendLine("| Cause | Hits |");
                sb.AppendLine("|---|---|");
                foreach (var g in hits) sb.AppendLine($"| {g.Key} | {g.Count()} |");
            }
            sb.AppendLine();

            if (previous != null)
            {
                sb.AppendLine($"## Compared with previous report ({previous.createdAt}{(string.IsNullOrWhiteSpace(previous.label) ? "" : ", " + previous.label)})");
                sb.AppendLine();
                sb.AppendLine("| Skill | Clear rate | Avg clear time | Avg hits | Avg bombs |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var k in s.skills)
                {
                    var p = previous.skills.FirstOrDefault(x => x.skill == k.skill);
                    if (p == null) continue;
                    sb.AppendLine($"| {k.skill} | {Pct(p.clearRate)} → {Pct(k.clearRate)} | {Mmss(p.avgClearTime)} → {Mmss(k.avgClearTime)} | {F(p.avgHits)} → {F(k.avgHits)} | {F(p.avgBombs)} → {F(k.avgBombs)} |");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}

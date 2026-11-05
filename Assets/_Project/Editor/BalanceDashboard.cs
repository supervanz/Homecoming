using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Homecoming.Bot;
using Homecoming.Core;
using UnityEditor;
using UnityEngine;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// 밸런스 대시보드 (CLAUDE.md 11장). 메뉴 Homecoming/Balance Dashboard.
    /// 왼쪽: Normal 난이도 파라미터 조정 + 봇 시뮬레이션 실행. 오른쪽: 리포트 결과와 직전 리포트 비교.
    /// 조정 루프 (11-6): 값 1~2개 변경 → 실행 → 비교 → 기준에 들어오면 커밋.
    /// </summary>
    public class BalanceDashboard : EditorWindow
    {
        private const string PresetPath = "Assets/_Project/Data/Difficulty/Difficulty_Normal.asset";
        private const string ReportsFolder = "BalanceReports";
        private const float DefaultSecondsPerRun = 35f;

        private struct Param
        {
            public string field;
            public float min, max;
            public bool isInt;
            public Param(string field, float min, float max, bool isInt = false)
            {
                this.field = field; this.min = min; this.max = max; this.isInt = isInt;
            }
        }

        private static readonly (string header, Param[] items)[] Groups =
        {
            ("Player", new[]
            {
                new Param("playerMoveSpeedScale", 0.5f, 1.5f),
                new Param("playerFireIntervalScale", 0.5f, 2f),
                new Param("playerHitRadiusScale", 0.5f, 2f),
                new Param("invincibleTimeScale", 0.25f, 2f),
                new Param("playerMaxHpBonus", -3, 5, true),
                new Param("bombCountBonus", -3, 5, true),
            }),
            ("Enemy", new[]
            {
                new Param("enemyHpScale", 0.25f, 3f),
                new Param("enemySpeedScale", 0.5f, 2f),
                new Param("enemyFireIntervalScale", 0.25f, 3f),
                new Param("enemyBulletSpeedScale", 0.5f, 2f),
            }),
            ("Boss", new[]
            {
                new Param("bossHpScale", 0.25f, 3f),
                new Param("bossActionGapScale", 0.25f, 3f),
            }),
            ("Items", new[]
            {
                new Param("dropChanceScale", 0f, 3f),
            }),
        };

        private class Row
        {
            public string skill, endSection, cause;
            public bool cleared;
        }

        private class Report
        {
            public string folder, name;
            public SimulationReport.Summary summary;
            public readonly List<Row> rows = new List<Row>();
            public DifficultyPreset snapshot;
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private DifficultyPreset _preset;
        private SimulationConfig _config;
        private SerializedObject _presetSO, _configSO;
        private BotSkill[] _allSkills = new BotSkill[0];
        private Vector2 _scrollLeft, _scrollRight;
        private readonly List<string> _reports = new List<string>();
        private int _reportIndex = -1, _compareIndex = -1;
        private Report _current, _compare;
        private double _lastScan;
        private SimulationReport.Summary _latestSummary;
        private GUIStyle _header, _wrap;

        [MenuItem("Homecoming/Balance Dashboard")]
        public static void Open()
        {
            var w = GetWindow<BalanceDashboard>();
            w.titleContent = new GUIContent("Balance Dashboard");
            w.minSize = new Vector2(960f, 600f);
        }

        private void OnEnable()
        {
            LoadAssets();
            ScanReports(true);
        }

        private void OnDisable() => DestroySnapshots();

        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastScan > 3.0) ScanReports(false);
            Repaint();
        }

        // ---------- data ----------

        private void LoadAssets()
        {
            _preset = AssetDatabase.LoadAssetAtPath<DifficultyPreset>(PresetPath);
            _config = AssetDatabase.LoadAssetAtPath<SimulationConfig>(SimulationLauncher.DefaultConfig);
            _presetSO = _preset != null ? new SerializedObject(_preset) : null;
            _configSO = _config != null ? new SerializedObject(_config) : null;
            _allSkills = AssetDatabase.FindAssets("t:BotSkill")
                .Select(g => AssetDatabase.LoadAssetAtPath<BotSkill>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => s.targetClearRateMin)
                .ToArray();
        }

        private string ReportsRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportsFolder));

        private void ScanReports(bool force)
        {
            _lastScan = EditorApplication.timeSinceStartup;
            string root = ReportsRoot;
            var list = Directory.Exists(root)
                ? Directory.GetDirectories(root).Where(d => File.Exists(Path.Combine(d, "summary.json"))).OrderBy(d => d, StringComparer.Ordinal).ToList()
                : new List<string>();
            if (!force && list.Count == _reports.Count) return;

            bool added = list.Count > _reports.Count;
            _reports.Clear();
            _reports.AddRange(list);
            if (force || added)
            {
                _reportIndex = _reports.Count - 1;
                _compareIndex = _reports.Count - 2;
            }
            _reportIndex = Mathf.Clamp(_reportIndex, -1, _reports.Count - 1);
            _compareIndex = Mathf.Clamp(_compareIndex, -1, _reports.Count - 1);
            _latestSummary = _reports.Count > 0 ? LoadSummaryOnly(_reports[_reports.Count - 1]) : null;
            LoadSelected();
        }

        private void LoadSelected()
        {
            DestroySnapshots();
            _current = _reportIndex >= 0 ? LoadReport(_reports[_reportIndex]) : null;
            _compare = _compareIndex >= 0 && _compareIndex != _reportIndex ? LoadReport(_reports[_compareIndex]) : null;
        }

        private void DestroySnapshots()
        {
            if (_current?.snapshot != null) DestroyImmediate(_current.snapshot);
            if (_compare?.snapshot != null) DestroyImmediate(_compare.snapshot);
        }

        private static Report LoadReport(string dir)
        {
            try
            {
                var r = new Report { folder = dir, name = Path.GetFileName(dir) };
                r.summary = JsonUtility.FromJson<SimulationReport.Summary>(File.ReadAllText(Path.Combine(dir, "summary.json")));
                string csv = Path.Combine(dir, "runs.csv");
                if (File.Exists(csv))
                {
                    string[] lines = File.ReadAllLines(csv);
                    if (lines.Length > 0)
                    {
                        List<string> head = SplitCsv(lines[0]);
                        int iSkill = head.IndexOf("skill"), iCleared = head.IndexOf("cleared"), iSection = head.IndexOf("end_section"), iCause = head.IndexOf("death_cause");
                        foreach (string line in lines.Skip(1))
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            List<string> f = SplitCsv(line);
                            if (f.Count <= Math.Max(Math.Max(iSkill, iCleared), Math.Max(iSection, iCause))) continue;
                            r.rows.Add(new Row { skill = f[iSkill], cleared = f[iCleared] == "1", endSection = f[iSection], cause = f[iCause] });
                        }
                    }
                }
                if (!string.IsNullOrEmpty(r.summary.presetJson))
                {
                    r.snapshot = CreateInstance<DifficultyPreset>();
                    r.snapshot.hideFlags = HideFlags.HideAndDontSave;
                    JsonUtility.FromJsonOverwrite(r.summary.presetJson, r.snapshot);
                }
                return r;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BalanceDashboard] could not read report {dir}: {e.Message}");
                return null;
            }
        }

        private static List<string> SplitCsv(string line)
        {
            var result = new List<string>();
            var cur = new System.Text.StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else cur.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { result.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
            result.Add(cur.ToString());
            return result;
        }

        private static float FieldValue(DifficultyPreset p, string field)
        {
            FieldInfo fi = typeof(DifficultyPreset).GetField(field);
            if (fi == null || p == null) return float.NaN;
            object v = fi.GetValue(p);
            return v is int i ? i : (float)v;
        }

        // ---------- GUI ----------

        private void EnsureStyles()
        {
            _header ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _wrap ??= new GUIStyle(EditorStyles.label) { wordWrap = true };
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (_preset == null || _config == null)
            {
                EditorGUILayout.HelpBox($"Missing assets:\n{PresetPath}\n{SimulationLauncher.DefaultConfig}", MessageType.Error);
                if (GUILayout.Button("Reload")) LoadAssets();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(400f));
            _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft);
            DrawPreset();
            EditorGUILayout.Space(12f);
            DrawSimulation();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            _scrollRight = EditorGUILayout.BeginScrollView(_scrollRight);
            DrawResults();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreset()
        {
            EditorGUILayout.LabelField("Difficulty: Normal", _header);
            EditorGUILayout.LabelField("각 설정 에셋의 기본 수치에 곱하는 배율(1.0)과 더하는 값(0)입니다. 한 번에 1~2개만 바꾸고 실행해 비교하세요 (11-6).", _wrap);

            DifficultyPreset reference = _current?.snapshot;
            if (reference != null)
                EditorGUILayout.LabelField($"노란색: 선택한 리포트({_current.name}) 실행 당시 값과 다른 항목", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                _presetSO.Update();
                foreach (var (header, items) in Groups)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
                    foreach (Param p in items)
                    {
                        SerializedProperty prop = _presetSO.FindProperty(p.field);
                        if (prop == null) continue;
                        float now = p.isInt ? prop.intValue : prop.floatValue;
                        float before = FieldValue(reference, p.field);
                        bool changed = reference != null && !float.IsNaN(before) && !Mathf.Approximately(now, before);

                        Color old = GUI.backgroundColor;
                        if (changed) GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
                        EditorGUILayout.BeginHorizontal();
                        var label = new GUIContent(prop.displayName, prop.tooltip + (changed ? $"\n(리포트 당시 {before.ToString("0.##", Inv)})" : ""));
                        if (p.isInt) EditorGUILayout.IntSlider(prop, (int)p.min, (int)p.max, label);
                        else EditorGUILayout.Slider(prop, p.min, p.max, label);
                        if (GUILayout.Button(new GUIContent("↺", "기본값으로 (1.0 / 0)"), GUILayout.Width(24f)))
                        {
                            if (p.isInt) prop.intValue = 0;
                            else prop.floatValue = 1f;
                        }
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = old;
                    }
                }
                if (_presetSO.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(_preset);

                EditorGUILayout.Space(4f);
                if (GUILayout.Button("Reset all to default (1.0 / 0)"))
                {
                    Undo.RecordObject(_preset, "Reset difficulty");
                    foreach (var (_, items) in Groups)
                        foreach (Param p in items)
                        {
                            FieldInfo fi = typeof(DifficultyPreset).GetField(p.field);
                            fi?.SetValue(_preset, p.isInt ? (object)0 : 1f);
                        }
                    EditorUtility.SetDirty(_preset);
                    AssetDatabase.SaveAssetIfDirty(_preset);
                    _presetSO.Update();
                }
            }
        }

        private void DrawSimulation()
        {
            EditorGUILayout.LabelField("Bot simulation", _header);
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;

            using (new EditorGUI.DisabledScope(playing))
            {
                _configSO.Update();
                SerializedProperty presetProp = _configSO.FindProperty("preset");
                if (presetProp.objectReferenceValue != _preset) presetProp.objectReferenceValue = _preset;

                SerializedProperty skillsProp = _configSO.FindProperty("skills");
                var selected = new List<BotSkill>();
                for (int i = 0; i < skillsProp.arraySize; i++)
                    if (skillsProp.GetArrayElementAtIndex(i).objectReferenceValue is BotSkill s) selected.Add(s);

                EditorGUILayout.LabelField("Bot skills", EditorStyles.boldLabel);
                bool skillsChanged = false;
                var next = new List<BotSkill>();
                foreach (BotSkill s in _allSkills)
                {
                    bool on = selected.Contains(s);
                    bool now = EditorGUILayout.ToggleLeft($"{s.name}   (target clear {s.targetClearRateMin:P0}–{s.targetClearRateMax:P0})", on);
                    if (now != on) skillsChanged = true;
                    if (now) next.Add(s);
                }
                if (skillsChanged)
                {
                    skillsProp.arraySize = next.Count;
                    for (int i = 0; i < next.Count; i++) skillsProp.GetArrayElementAtIndex(i).objectReferenceValue = next[i];
                }

                EditorGUILayout.IntSlider(_configSO.FindProperty("runsPerSkill"), 1, 100, new GUIContent("Runs per skill", "실력 단계마다 몇 판 (정식 측정 100판)"));
                EditorGUILayout.PropertyField(_configSO.FindProperty("baseSeed"), new GUIContent("Base seed", "판 i의 시드 = Base seed + i. 같은 시드면 같은 판이 재현됩니다"));
                EditorGUILayout.PropertyField(_configSO.FindProperty("label"), new GUIContent("Memo", "리포트 이름에 붙는 메모 (예: bulletSpeed-10%)"));
                EditorGUILayout.PropertyField(_configSO.FindProperty("renderDuringRun"), new GUIContent("Render during run", "끄면 화면을 그리지 않아 더 빠릅니다"));
                if (_configSO.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(_config);

                int total = next.Count * _config.runsPerSkill;
                float perRun = DefaultSecondsPerRun;
                var latest = _latestSummary;
                if (latest != null && latest.realSeconds > 0f)
                {
                    int runs = latest.skills.Sum(k => k.runs);
                    if (runs > 0) perRun = latest.realSeconds / runs;
                }
                EditorGUILayout.LabelField($"{total} runs, estimated about {Mathf.CeilToInt(total * perRun / 60f)} min ({perRun:0}s per run)", EditorStyles.miniLabel);

                using (new EditorGUI.DisabledScope(total == 0))
                {
                    if (GUILayout.Button("▶  Run simulation", GUILayout.Height(30f)))
                    {
                        AssetDatabase.SaveAssets();
                        string result = SimulationLauncher.Run(SimulationLauncher.DefaultConfig);
                        if (result.StartsWith("ABORT")) ShowNotification(new GUIContent(result));
                    }
                }
            }

            SimulationSession session = SimulationSession.Active;
            if (EditorApplication.isPlaying && session != null)
            {
                EditorGUILayout.Space(4f);
                Rect r = GUILayoutUtility.GetRect(18f, 20f, GUILayout.ExpandWidth(true));
                float frac = session.Total > 0 ? session.Completed / (float)session.Total : 0f;
                EditorGUI.ProgressBar(r, frac, $"{session.Completed}/{session.Total}   {session.CurrentSkill}   {session.RealElapsed:0}s");
                if (GUILayout.Button(new GUIContent("Stop", "중간에 멈추면 리포트가 저장되지 않습니다")))
                    EditorApplication.isPlaying = false;
            }
        }

        private static SimulationReport.Summary LoadSummaryOnly(string dir)
        {
            try { return JsonUtility.FromJson<SimulationReport.Summary>(File.ReadAllText(Path.Combine(dir, "summary.json"))); }
            catch { return null; }
        }

        private void DrawResults()
        {
            EditorGUILayout.LabelField("Results", _header);
            if (_reports.Count == 0)
            {
                EditorGUILayout.HelpBox("아직 리포트가 없습니다. 왼쪽에서 시뮬레이션을 실행하세요.", MessageType.Info);
                return;
            }

            string[] names = _reports.Select(Path.GetFileName).ToArray();
            string[] compareNames = new[] { "(none)" }.Concat(names).ToArray();
            int ri = EditorGUILayout.Popup("Report", _reportIndex, names);
            int ci = EditorGUILayout.Popup("Compare with", _compareIndex + 1, compareNames) - 1;
            if (ri != _reportIndex || ci != _compareIndex)
            {
                _reportIndex = ri;
                _compareIndex = ci;
                LoadSelected();
            }
            if (_current == null)
            {
                EditorGUILayout.HelpBox("리포트를 읽지 못했습니다.", MessageType.Warning);
                return;
            }

            SimulationReport.Summary s = _current.summary;
            EditorGUILayout.BeginHorizontal();
            string real = s.realSeconds > 0f ? $"{s.realSeconds:0}s" : "-";
            EditorGUILayout.LabelField($"{s.skills.Sum(k => k.runs)} runs   seed {s.baseSeed}+   real {real}   {(string.IsNullOrEmpty(s.label) ? "" : "memo: " + s.label)}", EditorStyles.miniLabel);
            if (GUILayout.Button("Open summary.md", GUILayout.Width(130f))) EditorUtility.OpenWithDefaultApp(Path.Combine(_current.folder, "summary.md"));
            if (GUILayout.Button("Show folder", GUILayout.Width(100f))) EditorUtility.RevealInFinder(Path.Combine(_current.folder, "summary.md"));
            EditorGUILayout.EndHorizontal();

            DrawChangedParams();
            EditorGUILayout.Space(8f);
            DrawClearRates();
            EditorGUILayout.Space(8f);
            DrawGrowth();
            EditorGUILayout.Space(8f);
            DrawDeathsBySection();
            EditorGUILayout.Space(8f);
            DrawDeathCauses();
        }

        private void DrawChangedParams()
        {
            if (_compare == null) return;
            if (_current.snapshot == null || _compare.snapshot == null)
            {
                EditorGUILayout.LabelField("(이전 리포트에는 파라미터 기록이 없어 바뀐 값을 비교할 수 없습니다)", EditorStyles.miniLabel);
                return;
            }
            var diffs = new List<string>();
            foreach (var (_, items) in Groups)
                foreach (Param p in items)
                {
                    float a = FieldValue(_compare.snapshot, p.field), b = FieldValue(_current.snapshot, p.field);
                    if (!Mathf.Approximately(a, b)) diffs.Add($"{p.field}: {a.ToString("0.##", Inv)} → {b.ToString("0.##", Inv)}");
                }
            EditorGUILayout.LabelField(diffs.Count == 0 ? "Changed parameters: none" : "Changed parameters: " + string.Join(",  ", diffs), _wrap);
            if (diffs.Count > 2) EditorGUILayout.HelpBox("한 번에 3개 이상 바뀌었습니다. 원인과 결과를 분명히 보려면 1~2개씩 바꾸세요 (11-6).", MessageType.Warning);
        }

        private static string Judge(float v, float min, float max) => v < 0f ? "-" : v < min ? "below" : v > max ? "above" : "in range";
        private static Color JudgeColor(string j) => j == "in range" ? new Color(0.3f, 0.8f, 0.4f) : j == "-" ? Color.gray : new Color(1f, 0.55f, 0.3f);
        private static string Mmss(float s) => s < 0f ? "-" : $"{(int)s / 60}:{(int)s % 60:00}";
        private static string Num(float v) => v < 0f ? "-" : v.ToString("0.0", Inv);

        private void DrawClearRates()
        {
            EditorGUILayout.LabelField("Clear rate (green band = target, CLAUDE.md 11-5)", EditorStyles.boldLabel);
            float tMin = _config.targetClearTimeMin, tMax = _config.targetClearTimeMax;
            foreach (var k in _current.summary.skills)
            {
                var prev = _compare?.summary.skills.FirstOrDefault(x => x.skill == k.skill);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(k.skill, GUILayout.Width(100f));

                Rect bar = GUILayoutUtility.GetRect(240f, 18f, GUILayout.Width(240f));
                EditorGUI.DrawRect(bar, new Color(0f, 0f, 0f, 0.25f));
                EditorGUI.DrawRect(new Rect(bar.x + bar.width * k.targetMin, bar.y, bar.width * (k.targetMax - k.targetMin), bar.height), new Color(0.2f, 0.75f, 0.3f, 0.35f));
                EditorGUI.DrawRect(new Rect(bar.x, bar.y + 4f, bar.width * k.clearRate, bar.height - 8f), new Color(0.35f, 0.6f, 1f, 0.95f));
                GUI.Label(bar, $"  {k.clearRate:P0}  ({k.clears}/{k.runs})", EditorStyles.whiteMiniLabel);

                string j = Judge(k.clearRate, k.targetMin, k.targetMax);
                Color old = GUI.contentColor;
                GUI.contentColor = JudgeColor(j);
                EditorGUILayout.LabelField(j, GUILayout.Width(70f));
                GUI.contentColor = old;

                string delta = prev != null ? $"{(k.clearRate - prev.clearRate) * 100f:+0;-0;0}%p" : "";
                EditorGUILayout.LabelField(delta, GUILayout.Width(60f));

                string tj = Judge(k.avgClearTime, tMin, tMax);
                EditorGUILayout.LabelField($"clear {Mmss(k.avgClearTime)}", GUILayout.Width(80f));
                GUI.contentColor = JudgeColor(tj);
                EditorGUILayout.LabelField(tj, GUILayout.Width(70f));
                GUI.contentColor = old;
                EditorGUILayout.LabelField($"hits {Num(k.avgHits)}{(prev != null ? $" ({Num(prev.avgHits)})" : "")}   bombs {Num(k.avgBombs)}", GUILayout.Width(200f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.LabelField($"Clear time target {Mmss(tMin)}–{Mmss(tMax)}.  괄호 안은 비교 리포트 값.", EditorStyles.miniLabel);
        }

        private void DrawGrowth()
        {
            EditorGUILayout.LabelField("Growth and boss phases (average of runs that reached it)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (string h in new[] { "Skill", "Weapon Lv2", "Weapon Lv3", "Boss phase 1", "Boss phase 2", "Boss total" })
                EditorGUILayout.LabelField(h, EditorStyles.miniBoldLabel, GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();
            foreach (var k in _current.summary.skills)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(k.skill, GUILayout.Width(100f));
                EditorGUILayout.LabelField(Mmss(k.avgLv2Time), GUILayout.Width(100f));
                EditorGUILayout.LabelField(Mmss(k.avgLv3Time), GUILayout.Width(100f));
                EditorGUILayout.LabelField(k.avgPhase1 < 0f ? "-" : $"{k.avgPhase1:0}s", GUILayout.Width(100f));
                EditorGUILayout.LabelField(k.avgPhase2 < 0f ? "-" : $"{k.avgPhase2:0}s", GUILayout.Width(100f));
                EditorGUILayout.LabelField(k.avgBossFight < 0f ? "-" : $"{k.avgBossFight:0}s", GUILayout.Width(100f));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDeathsBySection()
        {
            EditorGUILayout.LabelField("Deaths by section (CLAUDE.md 12-5)", EditorStyles.boldLabel);
            var deaths = _current.rows.Where(r => !r.cleared).ToList();
            if (deaths.Count == 0)
            {
                EditorGUILayout.LabelField("No deaths.");
                return;
            }
            string[] order = { "0:00-0:30 Zako", "0:30-1:30 Aimer", "1:30-2:30 Carrier", "2:30-3:00 Rest", "Boss Phase1", "Boss Transition", "Boss Phase2", "Boss Desperation" };
            var sections = order.Where(o => deaths.Any(d => d.endSection == o)).Concat(deaths.Select(d => d.endSection).Distinct().Where(x => !order.Contains(x))).ToList();
            var skills = _current.summary.skills.Select(k => k.skill).ToList();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Section", EditorStyles.miniBoldLabel, GUILayout.Width(140f));
            foreach (string sk in skills) EditorGUILayout.LabelField(sk, EditorStyles.miniBoldLabel, GUILayout.Width(80f));
            EditorGUILayout.LabelField("Share of all deaths", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            foreach (string sec in sections)
            {
                int all = deaths.Count(d => d.endSection == sec);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(sec, GUILayout.Width(140f));
                foreach (string sk in skills) EditorGUILayout.LabelField(deaths.Count(d => d.skill == sk && d.endSection == sec).ToString(), GUILayout.Width(80f));
                Rect bar = GUILayoutUtility.GetRect(160f, 14f, GUILayout.Width(200f));
                EditorGUI.DrawRect(bar, new Color(0f, 0f, 0f, 0.2f));
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * all / deaths.Count, bar.height), new Color(1f, 0.4f, 0.35f, 0.85f));
                GUI.Label(bar, $"  {all * 100 / deaths.Count}%", EditorStyles.whiteMiniLabel);
                EditorGUILayout.EndHorizontal();
            }

            foreach (string sk in skills)
            {
                var ds = deaths.Where(d => d.skill == sk).ToList();
                if (ds.Count < 3) continue;
                var top = ds.GroupBy(d => d.endSection).OrderByDescending(g => g.Count()).First();
                float share = top.Count() / (float)ds.Count;
                if (share >= _config.deathConcentrationWarn)
                    EditorGUILayout.HelpBox($"{sk}: 사망의 {share:P0}가 \"{top.Key}\"에 몰려 있습니다 (난이도 급상승 구간 후보).", MessageType.Warning);
            }
        }

        private void DrawDeathCauses()
        {
            EditorGUILayout.LabelField("Death causes (top 8)", EditorStyles.boldLabel);
            var deaths = _current.rows.Where(r => !r.cleared).ToList();
            if (deaths.Count == 0)
            {
                EditorGUILayout.LabelField("No deaths.");
                return;
            }
            foreach (var g in deaths.GroupBy(d => d.cause).OrderByDescending(g => g.Count()).Take(8))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(g.Key, GUILayout.Width(260f));
                Rect bar = GUILayoutUtility.GetRect(160f, 14f, GUILayout.Width(200f));
                EditorGUI.DrawRect(bar, new Color(0f, 0f, 0f, 0.2f));
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * g.Count() / deaths.Count, bar.height), new Color(1f, 0.55f, 0.3f, 0.85f));
                GUI.Label(bar, $"  {g.Count()}", EditorStyles.whiteMiniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}

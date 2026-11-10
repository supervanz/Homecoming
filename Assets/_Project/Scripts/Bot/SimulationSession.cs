using System.Collections.Generic;
using Homecoming.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Homecoming.Bot
{
    /// <summary>
    /// 배치 시뮬레이션 (11-3): 실력 단계별로 N판을 연속 실행하고 결과를 리포트로 저장한다.
    /// 판마다 시드를 고정하고, 1프레임 = 고정 게임 시간으로 진행해 같은 시드면 같은 판이 재현된다.
    /// </summary>
    public class SimulationSession : MonoBehaviour
    {
        public static SimulationSession Active { get; private set; }

        /// <summary>끝난 판 수 / 전체 판 수 / 지금 실력 단계 / 실제 경과 시간 (대시보드 진행률 표시용)</summary>
        public int Completed => _results.Count;
        public int Total => _total;
        public string CurrentSkill => _cfg != null && _skillIndex < _cfg.skills.Length ? _cfg.skills[_skillIndex].name : "";
        public float RealElapsed => Time.realtimeSinceStartup - _startRealtime;

        private SimulationConfig _cfg;
        private int _skillIndex;
        private int _runIndex;
        private int _total;
        private readonly List<RunResult> _results = new List<RunResult>();
        private RunRecorder _recorder;
        private GameFlow _flow;
        private bool _runFinished;
        private float _startRealtime;

        public static void Begin(SimulationConfig cfg)
        {
            if (Active != null) return;
            var go = new GameObject("SimulationSession");
            DontDestroyOnLoad(go);
            Active = go.AddComponent<SimulationSession>();
            Active.StartSession(cfg);
        }

        private void StartSession(SimulationConfig cfg)
        {
            _cfg = cfg;
            _total = cfg.skills.Length * cfg.runsPerSkill;
            _startRealtime = Time.realtimeSinceStartup;
            Time.captureDeltaTime = cfg.simulationStep;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Difficulty.Override = cfg.preset;
            AudioListener.volume = 0f; // 빠르게 진행되는 판에서 소리가 겹치지 않게
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log($"[Simulation] start: {cfg.skills.Length} skills x {cfg.runsPerSkill} runs, preset {(cfg.preset != null ? cfg.preset.name : "none")}");
            StartRun();
        }

        private void StartRun()
        {
            BotSkill skill = _cfg.skills[_skillIndex];
            BotPilot.ForcedSkill = skill;
            GameSeed.ForcedSeed = _cfg.baseSeed + _runIndex;
            _runFinished = false;
            _recorder = null;
            SceneManager.LoadScene(_cfg.stageScene);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _cfg.stageScene) return;
            _recorder = FindAnyObjectByType<RunRecorder>();
            _flow = FindAnyObjectByType<GameFlow>();
            if (_recorder == null || _flow == null)
            {
                Debug.LogError("[Simulation] RunRecorder or GameFlow missing in stage scene. Stopping.");
                Finish();
                return;
            }
            _flow.ResultReady += OnRunFinished;
            if (!_cfg.renderDuringRun && Camera.main != null) Camera.main.enabled = false;
        }

        private void Update()
        {
            if (_recorder == null || _runFinished) return;
            if (_recorder.Elapsed > _cfg.maxRunSeconds)
            {
                _recorder.MarkTimedOut();
                OnRunFinished();
            }
        }

        private void OnRunFinished()
        {
            if (_runFinished) return;
            _runFinished = true;
            if (_flow != null) _flow.ResultReady -= OnRunFinished;

            RunResult r = _recorder.Result;
            r.run = _runIndex;
            r.skill = _cfg.skills[_skillIndex].name;
            r.preset = _cfg.preset != null ? _cfg.preset.name : "none";
            r.seed = GameRandom.Seed;
            _results.Add(r);
            Debug.Log($"[Simulation] {_results.Count}/{_total} {r.skill} seed {r.seed}: {(r.cleared ? "CLEAR" : r.timedOut ? "TIMEOUT" : "DEAD")} {r.endTime:0.0}s {r.endSection} " +
                      $"(real {Time.realtimeSinceStartup - _startRealtime:0}s)");

            _runIndex++;
            if (_runIndex >= _cfg.runsPerSkill)
            {
                _runIndex = 0;
                _skillIndex++;
            }
            if (_skillIndex >= _cfg.skills.Length) Finish();
            else StartRun();
        }

        private void Finish()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            string folder = null;
            if (_results.Count > 0) folder = SimulationReport.Write(_cfg, _results, Time.realtimeSinceStartup - _startRealtime);

            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            Difficulty.Override = null;
            BotPilot.ForcedSkill = null;
            GameSeed.ForcedSeed = null;
            Debug.Log($"[Simulation] done: {_results.Count} runs. Report: {folder}");
            Active = null;
            Destroy(gameObject);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

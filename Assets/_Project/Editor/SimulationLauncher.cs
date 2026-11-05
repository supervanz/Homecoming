using Homecoming.Bot;
using UnityEditor;
using UnityEngine;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// 봇 배치 시뮬레이션 시작: 메뉴 Homecoming/Run Bot Simulation 또는 CLI에서 Run(path).
    /// 플레이 모드에 들어간 직후 SimulationSession을 만든다. 끝나면 플레이 모드를 스스로 빠져나온다.
    /// </summary>
    public static class SimulationLauncher
    {
        private const string PendingKey = "Homecoming.Simulation.PendingConfig";
        public const string DefaultConfig = "Assets/_Project/Data/Simulation/SimulationConfig.asset";

        [MenuItem("Homecoming/Run Bot Simulation")]
        public static void RunDefault() => Run(DefaultConfig);

        public static string Run(string configPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "ABORT: already in play mode";
            if (AssetDatabase.LoadAssetAtPath<SimulationConfig>(configPath) == null) return "ABORT: config not found " + configPath;
            SessionState.SetString(PendingKey, configPath);
            EditorApplication.EnterPlaymode();
            return "entering play mode for simulation: " + configPath;
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            string path = SessionState.GetString(PendingKey, "");
            if (string.IsNullOrEmpty(path)) return;
            SessionState.EraseString(PendingKey);
            var cfg = AssetDatabase.LoadAssetAtPath<SimulationConfig>(path);
            if (cfg == null)
            {
                Debug.LogError("[Simulation] config not found: " + path);
                return;
            }
            SimulationSession.Begin(cfg);
        }
    }
}

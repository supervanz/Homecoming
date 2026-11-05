using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Bot
{
    /// <summary>배치 시뮬레이션 설정 (CLAUDE.md 11-3, 11-5).</summary>
    [CreateAssetMenu(menuName = "Homecoming/Simulation Config", fileName = "SimulationConfig")]
    public class SimulationConfig : ScriptableObject
    {
        [Tooltip("돌릴 실력 단계들")]
        public BotSkill[] skills = new BotSkill[0];
        [Tooltip("실력 단계마다 몇 판")]
        [Min(1)] public int runsPerSkill = 20;
        [Tooltip("적용할 난이도 프리셋")]
        public DifficultyPreset preset;
        [Tooltip("첫 판 시드. 판 i의 시드 = baseSeed + i (실력 단계 사이에 같은 시드를 써서 비교)")]
        public int baseSeed = 1000;
        [Tooltip("판마다 화면을 그릴지. 끄면 빨라진다")]
        public bool renderDuringRun = true;
        [Tooltip("한 판 최대 게임 시간 (초). 넘으면 timeout으로 기록")]
        public float maxRunSeconds = 480f;
        [Tooltip("시뮬레이션 1프레임이 나타내는 게임 시간 (초). 고정해야 재현된다")]
        public float simulationStep = 1f / 60f;
        public string stageScene = "Stage01";
        [Tooltip("리포트 폴더 (프로젝트 루트 기준)")]
        public string outputFolder = "BalanceReports";
        [Tooltip("리포트 폴더 이름에 붙일 메모 (예: bulletSpeed-10%)")]
        public string label = "";

        [Header("Targets (CLAUDE.md 11-5)")]
        [Tooltip("평균 클리어 시간 목표 (초)")]
        public float targetClearTimeMin = 240f;
        public float targetClearTimeMax = 300f;
        [Tooltip("사망이 한 구간에 이 비율 이상 몰리면 경고")]
        [Range(0f, 1f)] public float deathConcentrationWarn = 0.5f;
    }
}

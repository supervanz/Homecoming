using System;
using System.Collections.Generic;

namespace Homecoming.Bot
{
    /// <summary>한 판의 측정 결과 (CLAUDE.md 11-4). 시간은 모두 스테이지 경과 시간(초).</summary>
    [Serializable]
    public class RunResult
    {
        public int run;
        public string skill;
        public string preset;
        public int seed;

        public bool cleared;
        public bool timedOut;
        /// <summary>클리어(보스 격파) 또는 사망 시각</summary>
        public float endTime;
        /// <summary>끝난 구간 (사망 위치 / 클리어 시 보스 구간)</summary>
        public string endSection;
        /// <summary>마지막으로 맞은 원인 (사망 원인)</summary>
        public string deathCause;

        public int score;
        public List<string> hitSources = new List<string>();
        public List<string> hitSections = new List<string>();
        public List<float> hitTimes = new List<float>();
        public List<float> bombTimes = new List<float>();
        /// <summary>무기 2·3단계 도달 시각. 도달 못 하면 -1</summary>
        public float lv2Time = -1f;
        public float lv3Time = -1f;

        /// <summary>보스 등장, 1페이즈 시작, 전환 시작, 2페이즈 시작, 발악 시작, 격파 시각. 없으면 -1</summary>
        public float bossSpawn = -1f, bossPhase1 = -1f, bossTransition = -1f, bossPhase2 = -1f, bossDesperation = -1f, bossDefeated = -1f;

        /// <summary>1페이즈 소요 (시작 → 전환). 전환까지 못 가면 -1</summary>
        public float Phase1Duration => bossPhase1 >= 0f && bossTransition >= 0f ? bossTransition - bossPhase1 : -1f;
        /// <summary>2페이즈 소요 (2페이즈 시작 → 격파, 발악 포함). 격파 못 하면 -1</summary>
        public float Phase2Duration => bossPhase2 >= 0f && bossDefeated >= 0f ? bossDefeated - bossPhase2 : -1f;
        /// <summary>보스전 전체 (등장 → 격파). 격파 못 하면 -1</summary>
        public float BossFightDuration => bossSpawn >= 0f && bossDefeated >= 0f ? bossDefeated - bossSpawn : -1f;
    }
}

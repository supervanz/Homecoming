using System;
using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Bot
{
    /// <summary>
    /// 한 판 동안 측정 지표를 모은다 (11-4): 클리어·시간·사망 위치와 원인·피격·폭탄·무기 단계·보스 페이즈 시간.
    /// 사람이 플레이해도 동작하며, 판이 끝나면 콘솔에 한 줄 요약을 남긴다.
    /// </summary>
    public class RunRecorder : MonoBehaviour
    {
        [Serializable]
        public struct Section
        {
            public string label;
            [Tooltip("이 시각(초) 전까지가 이 구간")]
            public float endTime;
        }

        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private GameFlow flow;
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private ScoreKeeper score;
        [Tooltip("보스 등장 전 구간 (CLAUDE.md 12-5 타임라인)")]
        [SerializeField] private Section[] sections =
        {
            new Section { label = "0:00-0:30 Zako", endTime = 30f },
            new Section { label = "0:30-1:30 Aimer", endTime = 90f },
            new Section { label = "1:30-2:30 Carrier", endTime = 150f },
            new Section { label = "2:30-3:00 Rest", endTime = 999f },
        };

        public RunResult Result { get; } = new RunResult();
        public float Elapsed => spawner.Elapsed;

        private void OnEnable()
        {
            player.Hit += OnHit;
            player.Health.Died += OnDied;
            weapon.LevelChanged += OnLevel;
            bomb.BombUsed += OnBomb;
            bossDirector.Boss.StateChanged += OnBossState;
            flow.ResultReady += OnResult;
        }

        private void OnDisable()
        {
            if (player != null) { player.Hit -= OnHit; player.Health.Died -= OnDied; }
            if (weapon != null) weapon.LevelChanged -= OnLevel;
            if (bomb != null) bomb.BombUsed -= OnBomb;
            if (bossDirector != null && bossDirector.Boss != null) bossDirector.Boss.StateChanged -= OnBossState;
            if (flow != null) flow.ResultReady -= OnResult;
        }

        /// <summary>지금 시각이 어느 구간인지 (보스가 나왔으면 보스 상태)</summary>
        public string CurrentSection()
        {
            if (bossDirector.BossSpawned)
            {
                switch (bossDirector.Boss.State)
                {
                    case BossController.BossState.Entering:
                    case BossController.BossState.Phase1: return "Boss Phase1";
                    case BossController.BossState.Transition: return "Boss Transition";
                    case BossController.BossState.Phase2: return "Boss Phase2";
                    case BossController.BossState.Desperation: return "Boss Desperation";
                    case BossController.BossState.Defeated: return "Boss Defeated";
                }
            }
            float t = spawner.Elapsed;
            foreach (var s in sections)
                if (t < s.endTime) return s.label;
            return sections.Length > 0 ? sections[sections.Length - 1].label : "?";
        }

        private void OnHit(string source, int damage)
        {
            Result.hitSources.Add(source);
            Result.hitSections.Add(CurrentSection());
            Result.hitTimes.Add(spawner.Elapsed);
        }

        private void OnDied(Health _)
        {
            Result.cleared = false;
            Result.endTime = spawner.Elapsed;
            Result.endSection = CurrentSection();
            Result.deathCause = player.LastHitSource ?? "unknown";
        }

        private void OnLevel(int level)
        {
            if (level == 2 && Result.lv2Time < 0f) Result.lv2Time = spawner.Elapsed;
            if (level == 3 && Result.lv3Time < 0f) Result.lv3Time = spawner.Elapsed;
        }

        private void OnBomb() => Result.bombTimes.Add(spawner.Elapsed);

        private void OnBossState(BossController.BossState state)
        {
            float t = spawner.Elapsed;
            switch (state)
            {
                case BossController.BossState.Entering: Result.bossSpawn = t; break;
                case BossController.BossState.Phase1: Result.bossPhase1 = t; break;
                case BossController.BossState.Transition: Result.bossTransition = t; break;
                case BossController.BossState.Phase2: Result.bossPhase2 = t; break;
                case BossController.BossState.Desperation: Result.bossDesperation = t; break;
                case BossController.BossState.Defeated:
                    Result.bossDefeated = t;
                    Result.cleared = true;
                    Result.endTime = t;
                    Result.endSection = "Boss Defeated";
                    Result.deathCause = "";
                    break;
            }
        }

        private void OnResult()
        {
            Result.score = score.Score;
            Debug.Log($"[RunRecorder] {(Result.cleared ? "CLEAR" : "DEAD")} at {Result.endTime:0.0}s ({Result.endSection})" +
                      $"{(Result.cleared ? "" : $" by {Result.deathCause}")}, hits {Result.hitSources.Count}, bombs {Result.bombTimes.Count}, score {Result.score}");
        }

        /// <summary>시간 제한으로 끊긴 판을 마무리한다 (시뮬레이션용)</summary>
        public void MarkTimedOut()
        {
            Result.timedOut = true;
            Result.cleared = false;
            Result.endTime = spawner.Elapsed;
            Result.endSection = CurrentSection();
            Result.deathCause = "timeout";
            Result.score = score.Score;
        }
    }
}

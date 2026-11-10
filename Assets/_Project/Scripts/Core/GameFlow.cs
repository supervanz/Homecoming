using System;
using System.Collections;
using Homecoming.Boss;
using Homecoming.Player;
using Homecoming.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Homecoming.Core
{
    /// <summary>
    /// 스테이지 흐름: 플레이 → (플레이어 사망) 게임오버 / (보스 격파) 클리어 → 결과창 → 재시작 또는 타이틀.
    /// 결과창이 뜨면 게임을 멈춘다.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public enum FlowState { Playing, GameOver, Cleared }

        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private MenuInput menu;
        [Tooltip("사망 후 결과창까지 대기 (초)")]
        [SerializeField] private float gameOverDelay = 1.5f;
        [Tooltip("엔딩 시퀀스를 건너뛸 때(봇 시뮬레이션) 보스 격파 후 결과창까지 대기 (초)")]
        [SerializeField] private float clearDelay = 1f;
        [Tooltip("보스 격파 후 재생할 대기권 재진입 엔딩 (15장)")]
        [SerializeField] private Homecoming.Ending.EndingSequence ending;
        [SerializeField] private string titleScene = "Title";

        public FlowState State { get; private set; } = FlowState.Playing;
        public bool ResultShown { get; private set; }
        /// <summary>이번 판 플레이 시간 (초)</summary>
        public float PlayTime { get; private set; }

        public event Action ResultReady;
        /// <summary>결과창에서 재시작·타이틀을 고를 때 (선택음용)</summary>
        public event Action MenuChosen;

        private void Awake()
        {
            Time.timeScale = 1f;
        }

        private void OnEnable()
        {
            player.Health.Died += OnPlayerDied;
            bossDirector.Boss.Defeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            if (player != null) player.Health.Died -= OnPlayerDied;
            if (bossDirector != null && bossDirector.Boss != null) bossDirector.Boss.Defeated -= OnBossDefeated;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (State == FlowState.Playing) PlayTime += Time.deltaTime;
            if (!ResultShown) return;

            if (menu.ConfirmPressed)
            {
                MenuChosen?.Invoke();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else if (menu.BackPressed)
            {
                MenuChosen?.Invoke();
                SceneManager.LoadScene(titleScene);
            }
        }

        private void OnPlayerDied(Health _) => Finish(FlowState.GameOver, gameOverDelay);
        private void OnBossDefeated() => PlayEndingNow();

        /// <summary>클리어 처리: 엔딩을 재생하고 끝나면 결과창. F3 디버그 재생도 이 경로를 쓴다.</summary>
        public void PlayEndingNow()
        {
            if (State != FlowState.Playing) return;
            if (ending == null || !ending.ShouldPlay)
            {
                Finish(FlowState.Cleared, clearDelay);
                return;
            }
            State = FlowState.Cleared;
            ending.Play(() => StartCoroutine(ShowResultAfter(0f)));
        }

        private void Finish(FlowState result, float delay)
        {
            if (State != FlowState.Playing) return;
            State = result;
            StartCoroutine(ShowResultAfter(delay));
        }

        private IEnumerator ShowResultAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Time.timeScale = 0f;
            ResultShown = true;
            ResultReady?.Invoke();
        }
    }
}

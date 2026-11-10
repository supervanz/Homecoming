using System;
using Homecoming.Bot;
using Homecoming.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Homecoming.Ending
{
    /// <summary>
    /// 대기권 재진입 엔딩을 재생하고, 끝나면 결과창으로 넘긴다 (CLAUDE.md 15장).
    /// 봇 시뮬레이션에서는 건너뛴다. 개발 빌드에서는 F3으로 엔딩만 바로 재생할 수 있다 (15-3).
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;
        [SerializeField] private EndingController controller;
        [SerializeField] private GameFlow flow;

        private Action _onFinished;

        public bool IsPlaying { get; private set; }

        /// <summary>시뮬레이션 중에는 연출 없이 바로 결과로 간다.</summary>
        public bool ShouldPlay => SimulationSession.Active == null && director != null && director.playableAsset != null;

        private void Awake()
        {
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.stopped += OnStopped;
        }

        private void OnDestroy()
        {
            if (director != null) director.stopped -= OnStopped;
        }

        public void Play(Action onFinished)
        {
            if (IsPlaying) return;
            IsPlaying = true;
            _onFinished = onFinished;
            controller.Prepare();
            director.time = 0;
            director.Play();
        }

        private void OnStopped(PlayableDirector _)
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            Action done = _onFinished;
            _onFinished = null;
            done?.Invoke();
        }

        private void Update()
        {
            if (!Debug.isDebugBuild) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame && flow.State == GameFlow.FlowState.Playing)
            {
                Debug.Log("[Ending] debug play (F3)");
                flow.PlayEndingNow();
            }
        }
    }
}

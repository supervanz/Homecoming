using System.Collections;
using UnityEngine;

namespace Homecoming.Vfx
{
    /// <summary>
    /// 히트스톱: 아주 짧게 게임 시간을 거의 멈춘다 (13-5, 페이즈 전환·보스 격파에만).
    /// 끝날 때 다른 쪽(결과 화면 일시정지 등)이 시간 배율을 바꿨으면 건드리지 않는다.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        [Tooltip("히트스톱 동안의 시간 배율")]
        [SerializeField] private float frozenTimeScale = 0.02f;

        private Coroutine _running;
        private float _restoreScale = 1f;

        public static HitStop Instance { get; private set; }
        /// <summary>꺼 두면 히트스톱을 하지 않는다 (봇 시뮬레이션)</summary>
        public bool Enabled { get; set; } = true;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Freeze(float seconds)
        {
            if (!Enabled || seconds <= 0f) return;
            if (_running != null) StopCoroutine(_running);
            else _restoreScale = Time.timeScale;
            _running = StartCoroutine(Run(seconds));
        }

        private IEnumerator Run(float seconds)
        {
            Time.timeScale = frozenTimeScale;
            yield return new WaitForSecondsRealtime(seconds);
            Restore();
        }

        private void Restore()
        {
            if (Mathf.Approximately(Time.timeScale, frozenTimeScale)) Time.timeScale = _restoreScale;
            _running = null;
        }

        private void OnDisable()
        {
            if (_running != null) Restore();
        }
    }
}

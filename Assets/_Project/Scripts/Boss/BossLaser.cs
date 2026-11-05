using System.Collections;
using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>
    /// 예고선 → 레이저 발사. 판정은 평면 위 직사각형(선분 + 폭)과 원의 거리로 계산한다.
    /// </summary>
    public class BossLaser : MonoBehaviour
    {
        [Tooltip("예고선 외형 (얇고 반투명)")]
        [SerializeField] private Transform telegraph;
        [Tooltip("레이저 외형")]
        [SerializeField] private Transform beam;
        [Tooltip("예고선 두께 (외형)")]
        [SerializeField] private float telegraphWidth = 0.15f;
        [Tooltip("외형 높이 (평면 위로 살짝 띄움)")]
        [SerializeField] private float visualHeight = 0.05f;

        private enum LaserState { Off, Telegraph, Firing }

        private LaserState _state;
        private Vector3 _origin;
        private Vector3 _dir = Vector3.back;
        private float _width;
        private float _length;

        public int Damage { get; private set; }
        public bool IsFiring => _state == LaserState.Firing;
        /// <summary>예고선 또는 발사 중 (봇이 피할 대상으로 본다)</summary>
        public bool IsDangerous => _state != LaserState.Off;

        /// <summary>상태와 관계없이 지금 방향의 레이저 범위에 들어가는지 (봇의 예측용)</summary>
        public bool Covers(Vector3 point, float radius)
        {
            if (_state == LaserState.Off) return false;
            point.y = 0f;
            Vector3 v = point - _origin;
            float along = Vector3.Dot(v, _dir);
            if (along < 0f || along > _length) return false;
            Vector3 perp = v - _dir * along;
            float limit = _width * 0.5f + radius;
            return perp.sqrMagnitude <= limit * limit;
        }

        public static System.Collections.Generic.List<BossLaser> All { get; } = new System.Collections.Generic.List<BossLaser>();

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }

        /// <summary>사망 원인 집계용 이름 (레이저 행동 이름)</summary>
        public string SourceTag { get; private set; }

        public IEnumerator Play(BossLaserAction a, Transform origin)
        {
            SourceTag = a.name;
            _width = a.width;
            _length = a.length;
            Damage = a.damage;

            _state = LaserState.Telegraph;
            telegraph.gameObject.SetActive(true);
            for (float t = 0f; t < a.telegraphTime; t += Time.deltaTime)
            {
                Aim(origin, a.startAngle, telegraph, telegraphWidth);
                yield return null;
            }
            telegraph.gameObject.SetActive(false);

            _state = LaserState.Firing;
            LaserRegistry.Add(this);
            beam.gameObject.SetActive(true);
            for (float t = 0f; t < a.fireTime; t += Time.deltaTime)
            {
                float angle = a.mode == LaserMode.Sweep ? Mathf.Lerp(a.startAngle, a.endAngle, t / a.fireTime) : a.startAngle;
                Aim(origin, angle, beam, _width);
                yield return null;
            }
            Stop();
        }

        public void Stop()
        {
            _state = LaserState.Off;
            LaserRegistry.Remove(this);
            if (telegraph != null) telegraph.gameObject.SetActive(false);
            if (beam != null) beam.gameObject.SetActive(false);
        }

        public bool Hits(Vector3 point, float radius)
        {
            if (_state != LaserState.Firing) return false;
            point.y = 0f;
            Vector3 v = point - _origin;
            float along = Vector3.Dot(v, _dir);
            if (along < 0f || along > _length) return false;
            Vector3 perp = v - _dir * along;
            float limit = _width * 0.5f + radius;
            return perp.sqrMagnitude <= limit * limit;
        }

        private void Aim(Transform origin, float angle, Transform visual, float visualWidth)
        {
            _origin = origin.position;
            _origin.y = 0f;
            _dir = Quaternion.Euler(0f, angle, 0f) * Vector3.back;
            visual.SetPositionAndRotation(_origin + _dir * (_length * 0.5f) + Vector3.up * visualHeight, Quaternion.LookRotation(_dir, Vector3.up));
            visual.localScale = new Vector3(visualWidth, 0.1f, _length);
        }

        private void OnDisable()
        {
            Stop();
            All.Remove(this);
        }
    }
}

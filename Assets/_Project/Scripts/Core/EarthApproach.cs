using Homecoming.Enemy;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 배경의 지구 (CLAUDE.md 3장, 12-5): 2:30 WARNING 무렵 화면 아래에서 떠올라 보스전 동안 점점 커진다.
    /// 엔딩의 하늘 전환에서는 대기권 안으로 들어가며 아래로 빠져 사라진다.
    /// 게임 로직과 무관한 배경이다.
    /// </summary>
    public class EarthApproach : MonoBehaviour
    {
        [SerializeField] private WaveSpawner spawner;
        [Tooltip("대기 빛 (가산 혼합 구). _Intensity를 조절한다")]
        [SerializeField] private Renderer atmosphere;
        [Tooltip("이 시각(초)부터 보이기 시작")]
        [SerializeField] private float appearTime = 150f;
        [Tooltip("이 시각(초)에 가장 가까워짐")]
        [SerializeField] private float closestTime = 270f;
        [SerializeField] private Vector3 farPosition = new Vector3(0f, -401f, 98f);
        [SerializeField] private Vector3 nearPosition = new Vector3(0f, -250f, 78f);
        [SerializeField] private float farScale = 70f;
        [SerializeField] private float nearScale = 140f;
        [Tooltip("엔딩에서 빠져나갈 때 이동량 (화면 아래쪽으로)")]
        [SerializeField] private Vector3 exitOffset = new Vector3(0f, -128f, -215f);
        [Tooltip("자전 속도 (도/초)")]
        [SerializeField] private float spinSpeed = 1.5f;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private MaterialPropertyBlock _mpb;
        private float _atmoIntensity;
        private bool _frozen;
        private Vector3 _frozenPosition;
        private float _frozenScale;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _atmoIntensity = atmosphere != null ? atmosphere.sharedMaterial.GetFloat(IntensityId) : 0f;
            Place(-1f);
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
            if (_frozen) return;
            float t = Mathf.InverseLerp(appearTime, closestTime, spawner.Elapsed);
            Place(spawner.Elapsed < appearTime ? -1f : t);
        }

        private void Place(float t)
        {
            bool visible = t >= 0f;
            foreach (Transform c in transform) c.gameObject.SetActive(visible);
            if (!visible) return;
            float s = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(farPosition, nearPosition, s);
            transform.localScale = Vector3.one * Mathf.Lerp(farScale, nearScale, s);
        }

        /// <summary>엔딩 시작: 지금 위치에서 멈춘다.</summary>
        public void Freeze()
        {
            _frozen = true;
            _frozenPosition = transform.position;
            _frozenScale = transform.localScale.x;
            // F3 디버그처럼 아직 지구가 안 보일 때 엔딩이 시작되면 가장 가까운 위치로 보여 준다
            if (spawner.Elapsed < appearTime)
            {
                Place(1f);
                _frozenPosition = transform.position;
                _frozenScale = transform.localScale.x;
            }
        }

        /// <summary>하늘 전환 진행 (0 → 1): 지구가 아래로 빠지며 대기 빛이 사라진다.</summary>
        public void SetExit(float p)
        {
            transform.position = _frozenPosition + exitOffset * p;
            transform.localScale = Vector3.one * _frozenScale * (1f + p);
            if (atmosphere != null)
            {
                atmosphere.GetPropertyBlock(_mpb);
                _mpb.SetFloat(IntensityId, _atmoIntensity * (1f - p));
                atmosphere.SetPropertyBlock(_mpb);
            }
            if (p >= 1f) foreach (Transform c in transform) c.gameObject.SetActive(false);
        }
    }
}

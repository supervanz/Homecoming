using UnityEngine;

namespace Homecoming.Vfx
{
    /// <summary>
    /// 카메라 흔들림. 기준 위치에서 잠깐 어긋났다 돌아온다.
    /// 히트스톱 중에도 흔들리도록 실제 시간으로 진행한다.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private VfxConfig config;
        [Tooltip("흔들림 진동수 (Hz)")]
        [SerializeField] private float frequency = 25f;

        private Vector3 _basePosition;
        private float _strength;
        private float _duration;
        private float _timer;

        public static CameraShake Instance { get; private set; }
        public bool Enabled => ScreenShakeSetting.IsEnabled(config.screenShakeDefault);

        private void Awake()
        {
            Instance = this;
            _basePosition = transform.localPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>흔들림이 돌아올 기준 위치를 바꾼다 (화면 비율에 맞춰 카메라를 옮길 때, CameraFit).</summary>
        public void SetBasePosition(Vector3 localPosition) => _basePosition = localPosition;

        /// <summary>남은 흔들림보다 센 요청만 덮어쓴다.</summary>
        public void Shake(float strength, float duration)
        {
            if (strength <= 0f || duration <= 0f || !Enabled) return;
            float remaining = _timer > 0f ? _strength * (_timer / _duration) : 0f;
            if (strength < remaining) return;
            _strength = Mathf.Min(strength, config.maxShake);
            _duration = duration;
            _timer = duration;
        }

        private void LateUpdate()
        {
            if (_timer <= 0f) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                transform.localPosition = _basePosition;
                return;
            }
            float amp = _strength * (_timer / _duration);
            float t = Time.unscaledTime * frequency;
            Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, 0f, Mathf.PerlinNoise(0f, t) - 0.5f) * (2f * amp);
            transform.localPosition = _basePosition + offset;
        }
    }
}

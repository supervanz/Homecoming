using UnityEngine;

namespace Homecoming.Audio
{
    /// <summary>
    /// 배경 음악 (14-2): 두 소스로 크로스페이드, 덕킹 (14-4). 씬이 바뀌어도 남아 있다 (먼저 생긴 하나만 유지).
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private SoundConfig config;

        public static MusicPlayer Instance { get; private set; }

        private AudioSource _a, _b;
        private AudioSource _current;
        private float _currentVolume;
        private float _fade;
        private float _duckTimer;

        public MusicTrack Current { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _a = MakeSource();
            _b = MakeSource();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private AudioSource MakeSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.priority = 0;
            s.outputAudioMixerGroup = config.bgmGroup;
            return s;
        }

        /// <summary>곡 전환 (같은 곡이면 무시). None이면 페이드아웃</summary>
        public void Play(MusicTrack track)
        {
            if (track == Current) return;
            Current = track;
            AudioSource next = _current == _a ? _b : _a;
            SoundConfig.Music m = config.Find(track);
            next.Stop();
            if (m != null && m.clip != null)
            {
                next.clip = m.clip;
                next.loop = m.loop;
                next.volume = 0f;
                next.Play();
                _currentVolume = m.volume;
            }
            else
            {
                _currentVolume = 0f;
            }
            _current = next;
            _fade = 0f;
        }

        public void Stop() => Play(MusicTrack.None);

        public void Duck(float seconds) => _duckTimer = Mathf.Max(_duckTimer, seconds);

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_duckTimer > 0f) _duckTimer -= dt;
            _fade = config.crossfadeTime > 0f ? Mathf.Min(1f, _fade + dt / config.crossfadeTime) : 1f;

            float duck = _duckTimer > 0f ? config.duckLevel : 1f;
            AudioSource other = _current == _a ? _b : _a;
            if (_current != null && _current.isPlaying) _current.volume = Mathf.MoveTowards(_current.volume, _currentVolume * _fade * duck, dt / Mathf.Max(0.01f, config.duckRelease));
            if (other != null && other.isPlaying)
            {
                other.volume = Mathf.MoveTowards(other.volume, 0f, dt / Mathf.Max(0.01f, config.crossfadeTime));
                if (other.volume <= 0f) other.Stop();
            }
        }
    }
}

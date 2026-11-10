using System.Collections.Generic;
using UnityEngine;

namespace Homecoming.Audio
{
    /// <summary>
    /// 효과음 재생 (14-4): AudioSource 풀, 같은 소리 동시 재생 상한, 피치·볼륨 무작위화, 덕킹, 반복 재생.
    /// 씬이 바뀌어도 남아 있어 게임 시작음 같은 소리가 끊기지 않는다 (먼저 생긴 하나만 유지).
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private SoundConfig config;

        public static SfxPlayer Instance { get; private set; }

        private AudioSource[] _voices;
        private SoundId[] _voiceIds;
        private readonly Dictionary<SoundId, float> _lastPlayed = new Dictionary<SoundId, float>();
        private readonly Dictionary<SoundId, AudioSource> _loops = new Dictionary<SoundId, AudioSource>();
        private float _duckTimer;

        public SoundConfig Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _voices = new AudioSource[config.sfxVoices];
            _voiceIds = new SoundId[config.sfxVoices];
            for (int i = 0; i < _voices.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _voices[i] = s;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>덕킹: 일정 시간 일반 효과음 볼륨을 낮춘다 (폭탄·보스 격파)</summary>
        public void Duck(float seconds) => _duckTimer = Mathf.Max(_duckTimer, seconds);

        private float DuckMultiplier(SoundConfig.Sound s)
        {
            if (s.ignoreDucking || _duckTimer <= 0f) return 1f;
            return config.duckLevel;
        }

        private void Update()
        {
            if (_duckTimer > 0f) _duckTimer -= Time.unscaledDeltaTime;
        }

        public void Play(SoundId id)
        {
            SoundConfig.Sound s = config.Find(id);
            if (s == null || s.clip == null) return;

            float now = Time.unscaledTime;
            if (s.minInterval > 0f && _lastPlayed.TryGetValue(id, out float last) && now - last < s.minInterval) return;

            int playing = 0;
            for (int i = 0; i < _voices.Length; i++)
                if (_voiceIds[i] == id && _voices[i].isPlaying) playing++;
            if (playing >= s.maxSimultaneous) return;

            int voice = PickVoice(s.priority);
            if (voice < 0) return;

            AudioSource src = _voices[voice];
            src.Stop();
            src.clip = s.clip;
            src.loop = false;
            src.priority = s.priority;
            src.outputAudioMixerGroup = s.group == SoundGroup.Ui ? config.uiGroup : config.sfxGroup;
            src.volume = s.volume * (1f - Random.Range(0f, s.volumeJitter)) * DuckMultiplier(s);
            src.pitch = Random.Range(s.pitchRange.x, s.pitchRange.y);
            src.Play();
            _voiceIds[voice] = id;
            _lastPlayed[id] = now;
        }

        /// <summary>빈 소스, 없으면 우선순위가 가장 낮은 소스를 고른다. 새 소리보다 다 중요하면 -1</summary>
        private int PickVoice(int priority)
        {
            int worst = -1;
            int worstPriority = -1;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (!_voices[i].isPlaying) return i;
                if (_voices[i].priority > worstPriority)
                {
                    worstPriority = _voices[i].priority;
                    worst = i;
                }
            }
            return worstPriority >= priority ? worst : -1;
        }

        /// <summary>반복 재생 시작 (저체력 경고·WARNING 경보 등). 이미 재생 중이면 그대로</summary>
        public void StartLoop(SoundId id)
        {
            if (_loops.TryGetValue(id, out var existing) && existing != null && existing.isPlaying) return;
            SoundConfig.Sound s = config.Find(id);
            if (s == null || s.clip == null) return;
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.clip = s.clip;
            src.loop = true;
            src.priority = s.priority;
            src.outputAudioMixerGroup = s.group == SoundGroup.Ui ? config.uiGroup : config.sfxGroup;
            src.volume = s.volume;
            src.Play();
            _loops[id] = src;
        }

        public void StopLoop(SoundId id)
        {
            if (!_loops.TryGetValue(id, out var src)) return;
            if (src != null) Destroy(src);
            _loops.Remove(id);
        }

        public void StopAllLoops()
        {
            foreach (var src in _loops.Values) if (src != null) Destroy(src);
            _loops.Clear();
        }
    }
}

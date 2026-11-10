using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Homecoming.Audio
{
    /// <summary>
    /// 효과음·음악 설정 (CLAUDE.md 14-5). 볼륨·랜덤 범위·동시 재생 상한을 코드 수정 없이 조정한다.
    /// 볼륨 위계 (14-4): 피탄 &lt; 소형 폭발 &lt; 중형 폭발 &lt; 플레이어 피격 &lt; 페이즈 전환 &lt; 폭탄 &lt; 보스 격파.
    /// </summary>
    [CreateAssetMenu(menuName = "Homecoming/Sound Config", fileName = "SoundConfig")]
    public class SoundConfig : ScriptableObject
    {
        [Serializable]
        public class Sound
        {
            public SoundId id;
            public AudioClip clip;
            public SoundGroup group = SoundGroup.Sfx;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("재생마다 볼륨을 이만큼 무작위로 낮춘다 (반복 피로 감소)")]
            [Range(0f, 0.5f)] public float volumeJitter = 0f;
            [Tooltip("재생마다 피치 범위 (min, max)")]
            public Vector2 pitchRange = new Vector2(1f, 1f);
            [Tooltip("같은 소리 동시 재생 상한")]
            [Min(1)] public int maxSimultaneous = 4;
            [Tooltip("같은 소리를 다시 내기까지 최소 간격 (초)")]
            public float minInterval = 0f;
            [Tooltip("우선순위 (0이 가장 높음). 소스가 모자라면 낮은 것부터 끊는다")]
            [Range(0, 256)] public int priority = 128;
            [Tooltip("덕킹 중에도 볼륨을 낮추지 않는다 (플레이어 피격·경고음)")]
            public bool ignoreDucking;
        }

        [Serializable]
        public class Music
        {
            public MusicTrack track;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.6f;
            public bool loop = true;
        }

        [Header("Mixer (Master / BGM / SFX / UI)")]
        public AudioMixerGroup bgmGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup uiGroup;

        [Header("Mixing")]
        [Tooltip("동시에 쓸 수 있는 효과음 소스 수")]
        [Min(4)] public int sfxVoices = 24;
        [Tooltip("곡 전환 크로스페이드 시간 (초)")]
        public float crossfadeTime = 1f;
        [Tooltip("덕킹 시 BGM·일반 효과음 볼륨 배율")]
        [Range(0f, 1f)] public float duckLevel = 0.35f;
        [Tooltip("덕킹이 원래대로 돌아오는 시간 (초)")]
        public float duckRelease = 0.8f;
        [Tooltip("이 체력 이하에서 저체력 경고음 반복")]
        public int lowHpThreshold = 1;

        public Sound[] sounds = new Sound[0];
        public Music[] music = new Music[0];

        public Sound Find(SoundId id)
        {
            foreach (var s in sounds) if (s.id == id) return s;
            return null;
        }

        public Music Find(MusicTrack track)
        {
            foreach (var m in music) if (m.track == track) return m;
            return null;
        }
    }
}

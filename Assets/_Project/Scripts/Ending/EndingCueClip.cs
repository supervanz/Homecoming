using Homecoming.Audio;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Homecoming.Ending
{
    /// <summary>엔딩 연출 클립 하나. 무엇을 할지(cue)와 그에 필요한 값만 가진다.</summary>
    [System.Serializable]
    public class EndingCueClip : PlayableAsset, ITimelineClipAsset
    {
        public EndingCue cue;
        [Tooltip("Sound 클립: 낼 효과음")]
        public SoundId sound;
        [Tooltip("Music 클립: 바꿀 곡")]
        public MusicTrack music;
        [Tooltip("진행 곡선 (0~1 → 0~1). 기본은 부드러운 S자")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.None;

        // 실제 진행은 트랙 믹서(EndingCueMixer)가 디렉터 시간으로 계산한다.
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => Playable.Create(graph);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Homecoming.Ending
{
    /// <summary>
    /// 엔딩 연출 트랙. 클립 하나가 EndingCue 하나이며, 바인딩한 EndingController가 실제로 화면·소리를 바꾼다.
    /// </summary>
    [TrackColor(1f, 0.6f, 0.2f)]
    [TrackClipType(typeof(EndingCueClip))]
    [TrackBindingType(typeof(EndingController))]
    public class EndingCueTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<EndingCueMixer>.Create(graph, inputCount);
            var mixer = playable.GetBehaviour();
            foreach (TimelineClip clip in GetClips())
                if (clip.asset is EndingCueClip cue) mixer.Add(clip, cue);
            mixer.Sort();
            return playable;
        }
    }

    /// <summary>
    /// 디렉터 시간을 기준으로 각 클립의 시작(Begin) → 진행(Apply 0~1) → 끝(End)을 한 번씩 호출한다.
    /// 플레이 모드에서만 동작한다 (에디터에서 타임라인을 훑어볼 때는 씬을 바꾸지 않음).
    /// </summary>
    public class EndingCueMixer : PlayableBehaviour
    {
        private class Entry
        {
            public double start, end;
            public EndingCueClip clip;
            public bool started, ended;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public void Add(TimelineClip clip, EndingCueClip cue) =>
            _entries.Add(new Entry { start = clip.start, end = clip.end, clip = cue });

        public void Sort() => _entries.Sort((a, b) => a.start.CompareTo(b.start));

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying) return;
            var controller = playerData as EndingController;
            if (controller == null) return;
            var director = playable.GetGraph().GetResolver() as PlayableDirector;
            if (director == null) return;

            double t = director.time;
            foreach (Entry e in _entries)
            {
                if (e.ended || t < e.start) continue;
                if (!e.started)
                {
                    e.started = true;
                    controller.Begin(e.clip);
                }
                double length = e.end - e.start;
                float p = length > 0 ? Mathf.Clamp01((float)((t - e.start) / length)) : 1f;
                controller.Apply(e.clip, e.clip.curve.Evaluate(p));
                if (t >= e.end)
                {
                    e.ended = true;
                    controller.End(e.clip);
                }
            }
        }
    }
}

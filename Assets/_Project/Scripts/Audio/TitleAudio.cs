using Homecoming.UI;
using UnityEngine;

namespace Homecoming.Audio
{
    /// <summary>
    /// 타이틀 사운드: 타이틀 곡, 시작음. WebGL에서는 브라우저가 첫 입력 전 재생을 막으므로
    /// 음악은 첫 입력 뒤부터 들린다 (14-5).
    /// </summary>
    public class TitleAudio : MonoBehaviour
    {
        [SerializeField] private TitleScreen title;

        private void OnEnable() => title.StartRequested += OnStart;
        private void OnDisable() { if (title != null) title.StartRequested -= OnStart; }

        private void Start()
        {
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Play(MusicTrack.Title);
        }

        private void OnStart()
        {
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.Play(SoundId.GameStart);
        }
    }
}

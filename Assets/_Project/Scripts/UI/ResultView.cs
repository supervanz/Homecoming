using Homecoming.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Homecoming.UI
{
    /// <summary>결과창: CLEAR / GAME OVER, 점수, 플레이 시간, 재시작 안내.</summary>
    public class ResultView : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private GameFlow flow;
        [SerializeField] private ScoreKeeper score;
        [Tooltip("터치 기기에서 보일 안내 문구 (탭 = 다시 하기)")]
        [SerializeField] private string touchHint = "TAP  RETRY";

        private VisualElement _overlay;
        private Label _title, _score, _time;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            _overlay = root.Q("result");
            _title = root.Q<Label>("result-title");
            _score = root.Q<Label>("result-score");
            _time = root.Q<Label>("result-time");
            _overlay.EnableInClassList("overlay--visible", false);
            var hint = root.Q<Label>("result-hint");
            if (hint != null && Homecoming.Player.TouchControls.IsTouchDevice) hint.text = touchHint;
            flow.ResultReady += Show;
        }

        private void OnDisable()
        {
            if (flow != null) flow.ResultReady -= Show;
        }

        private void Show()
        {
            bool cleared = flow.State == GameFlow.FlowState.Cleared;
            _title.text = cleared ? "MISSION CLEAR" : "GAME OVER";
            _title.EnableInClassList("result-title--over", !cleared);
            _score.text = $"SCORE  {score.Score:D7}";
            int t = Mathf.FloorToInt(flow.PlayTime);
            _time.text = $"TIME  {t / 60:00}:{t % 60:00}";
            _overlay.EnableInClassList("overlay--visible", true);
        }
    }
}

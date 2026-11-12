using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Homecoming.UI
{
    /// <summary>타이틀: Confirm 입력으로 스테이지 시작. 안내 문구를 깜빡인다.</summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private MenuInput menu;
        [SerializeField] private string stageScene = "Stage01";
        [Tooltip("안내 문구 깜빡임 속도 (회/초)")]
        [SerializeField] private float hintBlinkRate = 1.5f;

        private Label _hint;

        /// <summary>시작을 눌렀을 때 (게임 시작음용)</summary>
        public event System.Action StartRequested;

        private void OnEnable()
        {
            Time.timeScale = 1f;
            _hint = document.rootVisualElement.Q<Label>("hint");
            if (_hint != null && Homecoming.Player.TouchControls.IsTouchDevice) _hint.text = touchHint;
        }

        [Tooltip("터치 기기에서 보일 안내 문구")]
        [SerializeField] private string touchHint = "TAP TO START";

        private void Update()
        {
            if (_hint != null)
                _hint.style.opacity = Mathf.FloorToInt(Time.unscaledTime * hintBlinkRate * 2f) % 2 == 0 ? 1f : 0.25f;

            // 이 첫 입력은 M8에서 WebGL 오디오를 시작하는 시점으로도 쓴다 (14-5).
            if (menu.ConfirmPressed)
            {
                StartRequested?.Invoke();
                SceneManager.LoadScene(stageScene);
            }
        }
    }
}

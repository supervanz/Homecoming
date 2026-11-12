using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Homecoming.Player
{
    /// <summary>
    /// 모바일 조작: 화면 어디든 누르고 끌면 기체가 손가락의 움직임을 따라간다(상대 드래그). 누르고 있는 동안 자동 사격.
    /// 폭탄은 화면의 BOMB 버튼. 마우스로 끌어도 같게 동작한다.
    /// 결과는 키보드·게임패드와 같은 ShipInputFrame으로 PlayerInputSource에 합쳐진다 (봇과 같은 입력 인터페이스, 11-2).
    /// 기체 속도는 키보드와 같은 최대 이동 속도를 넘지 않는다.
    /// </summary>
    [DefaultExecutionOrder(-110)] // ShipInput(-100)이 읽기 전에 이번 프레임 값을 계산한다
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Camera cam;
        [Tooltip("BOMB 버튼이 있는 HUD")]
        [SerializeField] private UIDocument hud;
        [SerializeField] private string bombButtonName = "bomb-button";
        [Tooltip("손가락 이동 대비 기체 이동 배율 (1 = 손가락과 같은 거리)")]
        [SerializeField] private float dragScale = 1.2f;
        [Tooltip("이 거리(유닛) 안이면 멈춘 것으로 본다 (떨림 방지)")]
        [SerializeField] private float deadZone = 0.03f;

        private Button _bombButton;
        private bool _dragging;
        private Vector3 _shipAnchor;
        private Vector3 _fingerAnchor;
        private bool _bombRequested;

        /// <summary>지금 손가락(또는 마우스)으로 조종 중인지</summary>
        public bool Active => _dragging;
        public Vector2 Move { get; private set; }

        /// <summary>터치 기기에서만 BOMB 버튼을 보인다</summary>
        public static bool IsTouchDevice => Application.isMobilePlatform || Touchscreen.current != null;

        private void OnEnable()
        {
            if (hud == null) return;
            _bombButton = hud.rootVisualElement.Q<Button>(bombButtonName);
            if (_bombButton == null) return;
            _bombButton.style.display = IsTouchDevice ? DisplayStyle.Flex : DisplayStyle.None;
            _bombButton.clicked += OnBombClicked;
        }

        private void OnDisable()
        {
            if (_bombButton != null) _bombButton.clicked -= OnBombClicked;
            _dragging = false;
        }

        private void OnBombClicked() => _bombRequested = true;

        /// <summary>이번 프레임에 BOMB 버튼이 눌렸는지 (한 번 읽으면 지워진다)</summary>
        public bool ConsumeBomb()
        {
            bool b = _bombRequested;
            _bombRequested = false;
            return b;
        }

        private void Update()
        {
            Pointer p = Pointer.current;
            if (p == null || !p.press.isPressed)
            {
                _dragging = false;
                Move = Vector2.zero;
                return;
            }

            Vector2 screen = p.position.ReadValue();
            if (!_dragging)
            {
                if (OverBombButton(screen)) return; // 버튼을 누른 손가락은 조종에 쓰지 않는다
                if (!ToPlane(screen, out _fingerAnchor)) return;
                _shipAnchor = movement.transform.position;
                _dragging = true;
            }

            if (!ToPlane(screen, out Vector3 finger)) return;
            Vector3 target = _shipAnchor + (finger - _fingerAnchor) * dragScale;
            Vector3 delta = target - movement.transform.position;
            delta.y = 0f;
            float step = movement.MaxSpeed * Time.deltaTime;
            if (delta.magnitude < deadZone || step <= 0f)
            {
                Move = Vector2.zero;
                return;
            }
            // 이번 프레임에 목표까지 가는 데 필요한 입력 (최대 1 = 키보드 최고 속도)
            Move = Vector2.ClampMagnitude(new Vector2(delta.x, delta.z) / step, 1f);
        }

        private bool ToPlane(Vector2 screen, out Vector3 world)
        {
            Ray r = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(r, out float d)) { world = r.GetPoint(d); return true; }
            world = default;
            return false;
        }

        private bool OverBombButton(Vector2 screen)
        {
            if (_bombButton == null || _bombButton.resolvedStyle.display == DisplayStyle.None || _bombButton.panel == null) return false;
            // 입력 좌표는 왼쪽 아래 원점, UI 패널은 왼쪽 위 원점
            Vector2 flipped = new Vector2(screen.x, Screen.height - screen.y);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_bombButton.panel, flipped);
            return _bombButton.worldBound.Contains(panelPos);
        }
    }
}

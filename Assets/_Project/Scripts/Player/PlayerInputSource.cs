using UnityEngine;
using UnityEngine.InputSystem;

namespace Homecoming.Player
{
    /// <summary>Input System 액션(키보드·게임패드)과 터치 조작을 읽어 ShipInputFrame으로 변환한다.</summary>
    public class PlayerInputSource : MonoBehaviour, IShipInputSource
    {
        [SerializeField] private InputActionAsset actions;
        [Tooltip("모바일 터치 조작 (없으면 키보드·게임패드만)")]
        [SerializeField] private TouchControls touch;
        [SerializeField] private string mapName = "Gameplay";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string fireActionName = "Fire";
        [SerializeField] private string bombActionName = "Bomb";

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _fire;
        private InputAction _bomb;

        private void Awake()
        {
            _map = actions.FindActionMap(mapName, throwIfNotFound: true);
            _move = _map.FindAction(moveActionName, throwIfNotFound: true);
            _fire = _map.FindAction(fireActionName, throwIfNotFound: true);
            _bomb = _map.FindAction(bombActionName, throwIfNotFound: true);
        }

        private void OnEnable() => _map?.Enable();
        private void OnDisable() => _map?.Disable();

        public ShipInputFrame ReadInput()
        {
            bool touching = touch != null && touch.Active;
            bool touchBomb = touch != null && touch.ConsumeBomb();
            return new ShipInputFrame
            {
                // 터치로 조종 중이면 터치 이동, 아니면 키보드·게임패드
                Move = touching ? touch.Move : Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f),
                Fire = _fire.IsPressed() || touching, // 터치 중 자동 사격
                Bomb = _bomb.WasPressedThisFrame() || touchBomb,
            };
        }
    }
}

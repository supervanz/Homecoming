using UnityEngine;
using UnityEngine.InputSystem;

namespace Homecoming.Player
{
    /// <summary>Input System 액션을 읽어 ShipInputFrame으로 변환한다.</summary>
    public class PlayerInputSource : MonoBehaviour, IShipInputSource
    {
        [SerializeField] private InputActionAsset actions;
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
            return new ShipInputFrame
            {
                Move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f),
                Fire = _fire.IsPressed(),
                Bomb = _bomb.WasPressedThisFrame(),
            };
        }
    }
}

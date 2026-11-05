using UnityEngine;
using UnityEngine.InputSystem;

namespace Homecoming.UI
{
    /// <summary>메뉴 입력 (Confirm: Space/Enter/South, Back: Esc/East). 타이틀과 결과창이 쓴다.</summary>
    public class MenuInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string mapName = "Menu";
        [SerializeField] private string confirmActionName = "Confirm";
        [SerializeField] private string backActionName = "Back";

        private InputActionMap _map;
        private InputAction _confirm;
        private InputAction _back;

        public bool ConfirmPressed => _confirm.WasPressedThisFrame();
        public bool BackPressed => _back.WasPressedThisFrame();

        private void Awake()
        {
            _map = actions.FindActionMap(mapName, throwIfNotFound: true);
            _confirm = _map.FindAction(confirmActionName, throwIfNotFound: true);
            _back = _map.FindAction(backActionName, throwIfNotFound: true);
        }

        private void OnEnable() => _map?.Enable();
        private void OnDisable() => _map?.Disable();
    }
}

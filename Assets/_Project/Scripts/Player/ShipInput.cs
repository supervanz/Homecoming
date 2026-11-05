using UnityEngine;

namespace Homecoming.Player
{
    /// <summary>
    /// 프레임마다 입력 공급원에서 한 번 읽어 Current에 보관한다.
    /// 이동·무기는 이 값만 읽으므로, 공급원을 봇으로 바꿔도 나머지 코드는 그대로다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ShipInput : MonoBehaviour
    {
        [Tooltip("IShipInputSource를 구현한 컴포넌트")]
        [SerializeField] private MonoBehaviour sourceBehaviour;

        private IShipInputSource _source;

        public ShipInputFrame Current { get; private set; }

        private void Awake()
        {
            if (sourceBehaviour != null)
            {
                _source = sourceBehaviour as IShipInputSource;
                if (_source == null)
                    Debug.LogError($"{sourceBehaviour.name}: IShipInputSource를 구현하지 않은 컴포넌트입니다.", this);
            }
        }

        public void SetSource(IShipInputSource source) => _source = source;

        private void Update()
        {
            Current = _source != null ? _source.ReadInput() : default;
        }
    }
}

using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>천천히 떨어지는 파워업 아이템. 획득 판정은 CollisionSystem이 한다.</summary>
    public class PowerUpItem : MonoBehaviour
    {
        [Tooltip("회전시킬 외형")]
        [SerializeField] private Transform visual;

        private PowerUpConfig _config;
        private PlayArea _playArea;
        private PowerUpPool _owner;
        private bool _live;

        public Vector3 Position => transform.position;
        public float Radius => _config.pickupRadius;

        public void Launch(Vector3 position, PowerUpConfig config, PlayArea playArea, PowerUpPool owner)
        {
            position.y = 0f;
            transform.position = position;
            _config = config;
            _playArea = playArea;
            _owner = owner;
            _live = true;
        }

        public void Despawn()
        {
            if (!_live) return;
            _live = false;
            _owner.Release(this);
        }

        private void Update()
        {
            transform.position += Vector3.back * (_config.fallSpeed * Time.deltaTime);
            if (visual != null) visual.Rotate(0f, _config.spinSpeed * Time.deltaTime, 0f, Space.World);
            if (!_playArea.Contains(transform.position, _config.despawnMargin))
                Despawn();
        }

        private void OnDrawGizmos()
        {
            if (_config == null) return;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 1f);
            GizmoCircle.Draw(new Vector3(transform.position.x, 0f, transform.position.z), _config.pickupRadius);
        }
    }
}

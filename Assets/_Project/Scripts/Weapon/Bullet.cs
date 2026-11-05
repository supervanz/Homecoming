using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>평면 위에서 직진하는 탄. 플레이 영역을 벗어나면 풀로 돌아간다.</summary>
    public class Bullet : MonoBehaviour
    {
        [Tooltip("플레이 영역 바깥으로 이만큼 나가면 회수")]
        [SerializeField] private float despawnMargin = 1f;
        [Tooltip("판정 반경 (외형보다 작게)")]
        [SerializeField] private float hitRadius = 0.15f;
        [SerializeField] private int damage = 1;
        [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0.2f, 1f);

        private Vector3 _velocity;
        private PlayArea _playArea;
        private BulletPool _owner;
        private bool _live;

        public Vector3 Position => transform.position;
        public float Radius => hitRadius;
        public int Damage => damage;

        public void Launch(Vector3 position, Vector3 direction, float speed, PlayArea playArea, BulletPool owner)
        {
            position.y = 0f;
            direction.y = 0f;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction.normalized, Vector3.up));
            _velocity = direction.normalized * speed;
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
            transform.position += _velocity * Time.deltaTime;
            if (!_playArea.Contains(transform.position, despawnMargin))
                Despawn();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            GizmoCircle.Draw(new Vector3(transform.position.x, 0f, transform.position.z), hitRadius);
        }
    }
}

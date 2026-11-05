using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>평면 위에서 직진하는 탄. 플레이 영역을 벗어나면 풀로 돌아간다.</summary>
    public class Bullet : MonoBehaviour
    {
        [Tooltip("플레이 영역 바깥으로 이만큼 나가면 회수")]
        [SerializeField] private float despawnMargin = 1f;

        private Vector3 _velocity;
        private PlayArea _playArea;
        private BulletPool _owner;

        public void Launch(Vector3 position, Vector3 direction, float speed, PlayArea playArea, BulletPool owner)
        {
            position.y = 0f;
            direction.y = 0f;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction.normalized, Vector3.up));
            _velocity = direction.normalized * speed;
            _playArea = playArea;
            _owner = owner;
        }

        private void Update()
        {
            transform.position += _velocity * Time.deltaTime;
            if (!_playArea.Contains(transform.position, despawnMargin))
                _owner.Release(this);
        }
    }
}

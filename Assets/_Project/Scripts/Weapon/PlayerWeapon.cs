using Homecoming.Core;
using Homecoming.Player;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>Fire 입력을 누르는 동안 정해진 간격으로 탄을 발사한다.</summary>
    public class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponConfig config;
        [SerializeField] private ShipInput input;
        [SerializeField] private BulletPool pool;
        [SerializeField] private PlayArea playArea;
        [Tooltip("탄이 나가는 위치")]
        [SerializeField] private Transform muzzle;

        private float _cooldown;

        public int ShotsFired { get; private set; }

        private void Update()
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (input.Current.Fire && _cooldown <= 0f)
            {
                Shoot();
                _cooldown = config.fireInterval;
            }
        }

        private void Shoot()
        {
            int count = Mathf.Max(1, config.bulletCount);
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-config.spreadAngle * 0.5f, config.spreadAngle * 0.5f, i / (float)(count - 1));
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                pool.Get().Launch(muzzle.position, dir, config.bulletSpeed, playArea, pool);
            }
            ShotsFired++;
        }
    }
}

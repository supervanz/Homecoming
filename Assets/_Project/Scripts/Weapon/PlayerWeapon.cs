using Homecoming.Core;
using Homecoming.Player;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>Fire 입력을 누르는 동안 현재 무기 단계의 수치로 탄을 발사한다.</summary>
    public class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponConfig config;
        [SerializeField] private ShipInput input;
        [SerializeField] private BulletPool pool;
        [SerializeField] private PlayArea playArea;
        [Tooltip("탄이 나가는 위치")]
        [SerializeField] private Transform muzzle;

        private float _cooldown;

        public int Level { get; private set; } = 1;
        public int MaxLevel => config.MaxLevel;
        public int ShotsFired { get; private set; }

        /// <summary>한 단계 올린다. 이미 최대면 false.</summary>
        public bool LevelUp()
        {
            if (Level >= config.MaxLevel) return false;
            Level++;
            return true;
        }

        private void Update()
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (input.Current.Fire && _cooldown <= 0f)
            {
                WeaponLevel lv = config.GetLevel(Level);
                Shoot(lv);
                _cooldown = lv.fireInterval;
            }
        }

        private void Shoot(WeaponLevel lv)
        {
            int count = Mathf.Max(1, lv.bulletCount);
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-lv.spreadAngle * 0.5f, lv.spreadAngle * 0.5f, i / (float)(count - 1));
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                pool.Get().Launch(muzzle.position, dir, lv.bulletSpeed, playArea, pool);
            }
            ShotsFired++;
        }
    }
}

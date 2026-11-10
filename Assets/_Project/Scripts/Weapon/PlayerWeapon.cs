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

        /// <summary>발사할 때 (현재 단계). 발사음용</summary>
        public event System.Action<int> Fired;
        /// <summary>파워업을 먹었을 때 (최대 단계 포함). 획득음용</summary>
        public event System.Action PowerUpCollected;

        /// <summary>무기 단계가 올랐을 때 (도달 시점 기록용)</summary>
        public event System.Action<int> LevelChanged;

        /// <summary>이미 최대 단계에서 파워업을 먹었을 때 (보너스 점수용)</summary>
        public event System.Action MaxLevelPowerUpCollected;

        /// <summary>한 단계 올린다. 이미 최대면 false.</summary>
        public bool LevelUp()
        {
            if (Level >= config.MaxLevel) return false;
            Level++;
            LevelChanged?.Invoke(Level);
            return true;
        }

        /// <summary>파워업 획득: 단계를 올리고, 이미 최대면 보너스 이벤트를 보낸다.</summary>
        public void CollectPowerUp()
        {
            PowerUpCollected?.Invoke();
            if (!LevelUp()) MaxLevelPowerUpCollected?.Invoke();
        }

        private void Update()
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (input.Current.Fire && _cooldown <= 0f)
            {
                WeaponLevel lv = config.GetLevel(Level);
                Shoot(lv);
                _cooldown = lv.fireInterval * Difficulty.Current.playerFireIntervalScale;
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
            Fired?.Invoke(Level);
        }
    }
}

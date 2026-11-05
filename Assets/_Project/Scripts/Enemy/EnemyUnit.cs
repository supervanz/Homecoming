using Homecoming.Core;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Enemy
{
    /// <summary>
    /// 적 한 기의 수명 관리: 스폰 시 체력·판정·경로·사격을 설정하고,
    /// 격파되거나 경로가 끝나면 풀로 돌아간다.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(CircleHitbox), typeof(PathFollower))]
    public class EnemyUnit : MonoBehaviour
    {
        [Tooltip("사격하지 않는 적은 비워 둔다")]
        [SerializeField] private EnemyGun gun;

        private Health _health;
        private CircleHitbox _hitbox;
        private PathFollower _follower;
        private EnemyConfig _config;
        private EnemyPool _pool;
        private bool _live;

        public CircleHitbox Hitbox => _hitbox;
        public Health Health => _health;
        public EnemyConfig Config => _config;
        public int ContactDamage => _config.contactDamage;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _hitbox = GetComponent<CircleHitbox>();
            _follower = GetComponent<PathFollower>();
            _health.Died += _ => Despawn();
            _follower.Finished += Despawn;
        }

        public void Spawn(EnemyConfig config, PathConfig path, Vector3 origin, bool mirror, StageContext ctx, EnemyPool pool)
        {
            _config = config;
            _pool = pool;
            _health.ResetHealth(config.maxHp);
            _hitbox.Radius = config.hitRadius;
            _follower.Begin(path, origin, mirror, ctx.ScrollSpeed);
            if (gun != null) gun.Begin(config.patterns, config.fireInterval, config.firstShotDelay, ctx);
            _live = true;
            EnemyRegistry.Add(this);
        }

        public void TakeDamage(int amount) => _health.TakeDamage(amount);

        private void Despawn()
        {
            if (!_live) return;
            _live = false;
            EnemyRegistry.Remove(this);
            _follower.Stop();
            if (gun != null) gun.Stop();
            _pool.Release(this);
        }
    }
}

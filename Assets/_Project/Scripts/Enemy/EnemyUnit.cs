using System;
using Homecoming.Core;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Enemy
{
    /// <summary>
    /// 적 한 기의 수명 관리: 스폰 시 체력·판정·경로·사격을 설정하고,
    /// 격파되거나 경로가 끝나면 풀로 돌아간다.
    /// </summary>
    [RequireComponent(typeof(HitTarget), typeof(PathFollower))]
    public class EnemyUnit : MonoBehaviour
    {
        /// <summary>플레이어에게 격파됐을 때만 발생 (화면 밖 퇴장은 제외). 드롭·점수가 구독한다.</summary>
        public static event Action<EnemyUnit> Killed;

        [Tooltip("사격하지 않는 적은 비워 둔다")]
        [SerializeField] private EnemyGun gun;

        private HitTarget _target;
        private PathFollower _follower;
        private EnemyConfig _config;
        private EnemyPool _pool;
        private bool _live;

        public HitTarget Target => _target;
        public EnemyConfig Config => _config;
        /// <summary>격파 시 확정으로 파워업을 떨어뜨리는 기체인지</summary>
        public bool CarriesDrop { get; private set; }

        private void Awake()
        {
            _target = GetComponent<HitTarget>();
            _follower = GetComponent<PathFollower>();
            _target.Health.Died += _ => OnKilled();
            _follower.Finished += Despawn;
        }

        public void Spawn(EnemyConfig config, PathConfig path, Vector3 origin, bool mirror, bool carriesDrop, StageContext ctx, EnemyPool pool)
        {
            _config = config;
            _pool = pool;
            CarriesDrop = carriesDrop;
            DifficultyPreset d = Difficulty.Current;
            _target.Health.ResetHealth(Mathf.Max(1, Mathf.RoundToInt(config.maxHp * d.enemyHpScale)));
            _target.Hitbox.Radius = config.hitRadius;
            _target.ContactDamage = config.contactDamage;
            _target.SourceTag = config.name;
            _target.Invulnerable = false;
            _follower.Begin(path, origin, mirror, ctx.ScrollSpeed, d.enemySpeedScale);
            if (gun != null) gun.Begin(config.patterns, config.fireInterval * d.enemyFireIntervalScale, config.firstShotDelay, ctx, config.name);
            _live = true;
            _target.Register();
            EnemyRegistry.Add(this);
        }

        public void TakeDamage(int amount) => _target.TakeDamage(amount);

        private void OnKilled()
        {
            if (!_live) return;
            Killed?.Invoke(this);
            Despawn();
        }

        private void Despawn()
        {
            if (!_live) return;
            _live = false;
            _target.Unregister();
            EnemyRegistry.Remove(this);
            _follower.Stop();
            if (gun != null) gun.Stop();
            _pool.Release(this);
        }
    }
}

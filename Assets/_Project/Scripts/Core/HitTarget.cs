using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 플레이어 탄·폭탄에 맞을 수 있는 대상 (일반 적, 보스 본체, 보스 부위).
    /// 판정(CircleHitbox)과 체력(Health)을 묶고, 맞을 수 있는 동안 HitTargetRegistry에 등록된다.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(CircleHitbox))]
    public class HitTarget : MonoBehaviour
    {
        [Tooltip("폭탄 피해 배율 (보스는 낮게)")]
        [SerializeField] private float bombDamageScale = 1f;
        [Tooltip("플레이어와 몸통이 부딪혔을 때 주는 피해. 0이면 몸통 판정 없음")]
        [SerializeField] private int contactDamage = 1;

        private Health _health;
        private CircleHitbox _hitbox;

        public Health Health => _health != null ? _health : (_health = GetComponent<Health>());
        public CircleHitbox Hitbox => _hitbox != null ? _hitbox : (_hitbox = GetComponent<CircleHitbox>());

        /// <summary>몸통 충돌 시 사망 원인으로 기록할 이름</summary>
        public string SourceTag { get; set; }

        /// <summary>켜져 있으면 탄은 막지만 피해는 받지 않는다 (등장·전환 중)</summary>
        public bool Invulnerable { get; set; }
        public bool IsAlive => !Health.IsDead;

        public int ContactDamage
        {
            get => contactDamage;
            set => contactDamage = value;
        }

        public float BombDamageScale
        {
            get => bombDamageScale;
            set => bombDamageScale = value;
        }

        /// <summary>어떤 대상이든 피해를 받았을 때 (피탄음용)</summary>
        public static event System.Action<HitTarget> AnyDamaged;

        public void TakeDamage(int amount)
        {
            if (Invulnerable) return;
            Health.TakeDamage(amount);
            AnyDamaged?.Invoke(this);
        }

        public void TakeBombDamage(int amount)
        {
            TakeDamage(Mathf.Max(1, Mathf.RoundToInt(amount * bombDamageScale)));
        }

        public void Register() => HitTargetRegistry.Add(this);
        public void Unregister() => HitTargetRegistry.Remove(this);

        private void OnDisable() => Unregister();
    }
}

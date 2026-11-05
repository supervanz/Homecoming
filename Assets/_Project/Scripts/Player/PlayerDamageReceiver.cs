using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Player
{
    /// <summary>플레이어 피격 처리: 체력 감소, 피격 후 무적 시간, 무적 중 깜빡임.</summary>
    public class PlayerDamageReceiver : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Health health;
        [SerializeField] private CircleHitbox hitbox;
        [Tooltip("깜빡일 외형 모델")]
        [SerializeField] private GameObject visual;

        private float _invincibleTimer;
        private Renderer[] _renderers;

        public bool IsInvincible => _invincibleTimer > 0f;
        public bool CanBeHit => isActiveAndEnabled && !health.IsDead && !IsInvincible;
        public Health Health => health;

        private void Awake()
        {
            health.ResetHealth(config.maxHp);
            hitbox.Radius = config.hitRadius;
            _renderers = visual.GetComponentsInChildren<Renderer>(true);
            health.Died += OnDied;
        }

        private void OnDestroy() => health.Died -= OnDied;

        public void ReceiveHit(int damage)
        {
            if (!CanBeHit) return;
            health.TakeDamage(damage);
            if (!health.IsDead) _invincibleTimer = config.invincibleTime;
        }

        private void Update()
        {
            if (_invincibleTimer <= 0f) return;
            _invincibleTimer -= Time.deltaTime;
            bool show = _invincibleTimer <= 0f || Mathf.FloorToInt(_invincibleTimer / config.blinkInterval) % 2 == 0;
            SetVisible(show);
        }

        private void SetVisible(bool show)
        {
            foreach (var r in _renderers) r.enabled = show;
        }

        private void OnDied(Health _)
        {
            // 게임오버 흐름은 M6에서 연결한다. 지금은 기체만 사라진다.
            Debug.Log("Player destroyed");
            SetVisible(true);
            gameObject.SetActive(false);
        }
    }
}

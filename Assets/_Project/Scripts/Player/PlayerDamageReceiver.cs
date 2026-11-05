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
        public bool IsAlive => isActiveAndEnabled && !health.IsDead;
        public bool CanBeHit => IsAlive && !IsInvincible;
        public Health Health => health;

        /// <summary>외부(폭탄 등)에서 무적 시간을 준다. 남은 시간보다 길 때만 늘린다.</summary>
        public void GrantInvincibility(float seconds)
        {
            _invincibleTimer = Mathf.Max(_invincibleTimer, seconds);
        }

        private void Awake()
        {
            DifficultyPreset d = Difficulty.Current;
            health.ResetHealth(Mathf.Max(1, config.maxHp + d.playerMaxHpBonus));
            hitbox.Radius = config.hitRadius * d.playerHitRadiusScale;
            _renderers = visual.GetComponentsInChildren<Renderer>(true);
            health.Died += OnDied;
        }

        private void OnDestroy() => health.Died -= OnDied;

        /// <summary>맞았을 때 (원인 이름, 피해). 사망 원인 집계용</summary>
        public event System.Action<string, int> Hit;
        public string LastHitSource { get; private set; }

        public void ReceiveHit(int damage, string source = null)
        {
            if (!CanBeHit) return;
            LastHitSource = string.IsNullOrEmpty(source) ? "unknown" : source;
            Hit?.Invoke(LastHitSource, damage);
            health.TakeDamage(damage);
            if (!health.IsDead) _invincibleTimer = config.invincibleTime * Difficulty.Current.invincibleTimeScale;
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

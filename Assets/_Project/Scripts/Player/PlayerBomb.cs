using Homecoming.Enemy;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Player
{
    /// <summary>
    /// Bomb 입력으로 폭탄 발동: 적 탄 전부 제거 + 화면 내 적 전체 피해 + 지속 시간 동안 탄 제거·무적.
    /// </summary>
    public class PlayerBomb : MonoBehaviour
    {
        [SerializeField] private BombConfig config;
        [SerializeField] private ShipInput input;
        [SerializeField] private BulletPool enemyBullets;
        [SerializeField] private PlayerDamageReceiver receiver;
        [Tooltip("임시 연출용 원판 (M8에서 교체)")]
        [SerializeField] private Renderer wave;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _mpb;
        private Color _waveColor;
        private float _timer;

        public int Count { get; private set; }
        public bool IsActive => _timer > 0f;
        public int Used { get; private set; }

        private void Awake()
        {
            Count = config.startCount;
            _mpb = new MaterialPropertyBlock();
            _waveColor = wave.sharedMaterial.GetColor(BaseColorId);
            wave.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (input.Current.Bomb && Count > 0 && !IsActive && receiver.IsAlive)
                Trigger();

            if (!IsActive) return;

            _timer -= Time.deltaTime;
            enemyBullets.ReleaseAll();
            UpdateWave();
        }

        private void Trigger()
        {
            Count--;
            Used++;
            _timer = config.duration;
            receiver.GrantInvincibility(config.duration);
            enemyBullets.ReleaseAll();

            // 피해로 적이 죽으면 목록에서 빠지므로 뒤에서부터 돈다.
            var enemies = EnemyRegistry.Active;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                enemies[i].TakeDamage(config.damage);
            }

            wave.transform.position = new Vector3(transform.position.x, -0.5f, transform.position.z);
            wave.gameObject.SetActive(true);
            UpdateWave();
        }

        private void UpdateWave()
        {
            float t = 1f - Mathf.Clamp01(_timer / config.duration);
            float size = Mathf.Lerp(0.5f, config.waveMaxSize, t);
            wave.transform.localScale = new Vector3(size, 0.01f, size);
            Color c = _waveColor;
            c.a = config.waveStartAlpha * (1f - t);
            wave.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            wave.SetPropertyBlock(_mpb);
            if (_timer <= 0f) wave.gameObject.SetActive(false);
        }
    }
}

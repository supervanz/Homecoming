using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Vfx
{
    /// <summary>
    /// 피탄 시 기체를 1~2프레임 하얗게 번쩍이게 한다 (13-3 1단계, 13-5).
    /// 손상 3단계의 붉은 깜빡임도 같은 방식(MaterialPropertyBlock)으로 낸다.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private VfxConfig config;
        [SerializeField] private Health health;
        [Tooltip("번쩍일 외형. 비우면 이 오브젝트 아래 전체")]
        [SerializeField] private GameObject visual;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private float _flashTimer;
        private bool _tinted;

        /// <summary>손상 3단계 깜빡임 (DamageStateVfx가 켠다)</summary>
        public bool CriticalBlink { get; set; }

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            Renderer[] all = (visual != null ? visual : gameObject).GetComponentsInChildren<Renderer>(true);
            // 파티클 렌더러(손상 이펙트)는 제외한다
            _renderers = System.Array.FindAll(all, r => !(r is ParticleSystemRenderer));
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += OnDamaged;
            _flashTimer = 0f;
            CriticalBlink = false;
            _tinted = true;
            Apply(false, Color.black);
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            Apply(false, Color.black);
        }

        private void OnDamaged(Health _, int __) => _flashTimer = config.hitFlashDuration;

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                Apply(true, config.hitFlashColor);
                return;
            }
            if (CriticalBlink && Mathf.FloorToInt(Time.time / config.criticalBlinkInterval) % 2 == 0)
            {
                Apply(true, config.criticalBlinkColor);
                return;
            }
            Apply(false, Color.black);
        }

        private void Apply(bool on, Color c)
        {
            if (_renderers == null) return;
            if (!on && !_tinted) return;
            _tinted = on;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                if (!on) { r.SetPropertyBlock(null); continue; }
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionId, c);
                _mpb.SetColor(BaseColorId, Color.Lerp(Color.white, c, 0.5f));
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}

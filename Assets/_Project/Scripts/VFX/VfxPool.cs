using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Homecoming.Vfx
{
    /// <summary>
    /// 일회성 이펙트 오브젝트 풀 (13-7). 종류별 동시 재생 상한과 전체 상한을 지킨다.
    /// </summary>
    public class VfxPool : MonoBehaviour
    {
        [SerializeField] private VfxConfig config;

        private readonly Dictionary<VfxId, ObjectPool<PooledEffect>> _pools = new Dictionary<VfxId, ObjectPool<PooledEffect>>();
        private readonly Dictionary<VfxId, int> _activeById = new Dictionary<VfxId, int>();
        private int _active;

        public static VfxPool Instance { get; private set; }
        public VfxConfig Config => config;
        public int ActiveCount => _active;

        /// <summary>꺼 두면 이펙트를 재생하지 않는다 (렌더링하지 않는 봇 시뮬레이션용)</summary>
        public bool Enabled { get; set; } = true;

        private void Awake()
        {
            Instance = this;
            foreach (var e in config.effects)
            {
                if (e.prefab == null || _pools.ContainsKey(e.id)) continue;
                VfxConfig.Effect entry = e;
                var pool = new ObjectPool<PooledEffect>(
                    createFunc: () => Create(entry),
                    actionOnGet: fx => fx.gameObject.SetActive(true),
                    actionOnRelease: fx => fx.gameObject.SetActive(false),
                    actionOnDestroy: fx => { if (fx != null) Destroy(fx.gameObject); },
                    collectionCheck: false,
                    defaultCapacity: Mathf.Max(1, entry.prewarm),
                    maxSize: entry.maxSimultaneous);
                _pools.Add(entry.id, pool);
                _activeById[entry.id] = 0;

                // 첫 폭발 때 생성 비용으로 끊기지 않게 미리 만든다.
                var warm = new List<PooledEffect>();
                for (int i = 0; i < entry.prewarm; i++) warm.Add(pool.Get());
                foreach (var fx in warm) pool.Release(fx);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private PooledEffect Create(VfxConfig.Effect e)
        {
            PooledEffect fx = Instantiate(e.prefab, transform);
            fx.Bind(e.id, Release);
            fx.gameObject.SetActive(false);
            return fx;
        }

        /// <summary>평면 위치(x, z)에 이펙트를 재생한다. 높이는 config.effectHeight로 고정.</summary>
        public bool Play(VfxId id, Vector3 planePosition, float scaleMultiplier = 1f)
        {
            if (!Enabled || !_pools.TryGetValue(id, out var pool)) return false;
            VfxConfig.Effect e = config.Find(id);
            if (_active >= config.maxActiveEffects || _activeById[id] >= e.maxSimultaneous) return false;

            PooledEffect fx = pool.Get();
            _active++;
            _activeById[id]++;
            Vector3 p = new Vector3(planePosition.x, config.effectHeight, planePosition.z);
            fx.PlayAt(p, e.scale * scaleMultiplier);
            return true;
        }

        private void Release(PooledEffect fx)
        {
            if (!fx.gameObject.activeSelf) return;
            _active = Mathf.Max(0, _active - 1);
            _activeById[fx.Id] = Mathf.Max(0, _activeById[fx.Id] - 1);
            _pools[fx.Id].Release(fx);
        }
    }
}

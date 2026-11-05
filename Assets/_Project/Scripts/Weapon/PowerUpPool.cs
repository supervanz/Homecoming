using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Homecoming.Weapon
{
    /// <summary>파워업 아이템 오브젝트 풀.</summary>
    public class PowerUpPool : MonoBehaviour
    {
        [SerializeField] private PowerUpItem prefab;
        [SerializeField] private int defaultCapacity = 4;
        [SerializeField] private int maxSize = 16;

        private ObjectPool<PowerUpItem> _pool;
        private readonly List<PowerUpItem> _active = new List<PowerUpItem>();

        public IReadOnlyList<PowerUpItem> Active => _active;

        private void Awake()
        {
            _pool = new ObjectPool<PowerUpItem>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: p => { p.gameObject.SetActive(true); _active.Add(p); },
                actionOnRelease: p => { p.gameObject.SetActive(false); _active.Remove(p); },
                // 플레이 모드 종료 시 씬 언로드로 이미 파괴된 뒤 Clear가 불릴 수 있다.
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public PowerUpItem Get() => _pool.Get();

        public void Release(PowerUpItem item) => _pool.Release(item);
    }
}

using UnityEngine;
using UnityEngine.Pool;

namespace Homecoming.Weapon
{
    /// <summary>탄 오브젝트 풀. 생성·파괴 대신 재사용해 WebGL에서 GC 부담을 줄인다.</summary>
    public class BulletPool : MonoBehaviour
    {
        [SerializeField] private Bullet prefab;
        [SerializeField] private int defaultCapacity = 64;
        [SerializeField] private int maxSize = 256;

        private ObjectPool<Bullet> _pool;

        public int CountActive => _pool?.CountActive ?? 0;
        public int CountInactive => _pool?.CountInactive ?? 0;

        private void Awake()
        {
            _pool = new ObjectPool<Bullet>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public Bullet Get() => _pool.Get();

        public void Release(Bullet bullet) => _pool.Release(bullet);
    }
}

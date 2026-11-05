using System.Collections.Generic;
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
        private readonly List<Bullet> _active = new List<Bullet>();

        /// <summary>현재 화면에 나와 있는 탄 목록 (충돌 판정용)</summary>
        public IReadOnlyList<Bullet> Active => _active;
        public int CountActive => _active.Count;
        public int CountInactive => _pool?.CountInactive ?? 0;

        private void Awake()
        {
            _pool = new ObjectPool<Bullet>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: b => { b.gameObject.SetActive(true); _active.Add(b); },
                actionOnRelease: b => { b.gameObject.SetActive(false); _active.Remove(b); },
                actionOnDestroy: b => Destroy(b.gameObject),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public Bullet Get() => _pool.Get();

        public void Release(Bullet bullet) => _pool.Release(bullet);
    }
}

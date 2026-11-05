using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Homecoming.Enemy
{
    /// <summary>적 종류(프리팹)별 오브젝트 풀.</summary>
    public class EnemyPool : MonoBehaviour
    {
        [SerializeField] private int defaultCapacity = 16;
        [SerializeField] private int maxSizePerType = 64;

        private readonly Dictionary<EnemyUnit, ObjectPool<EnemyUnit>> _pools = new Dictionary<EnemyUnit, ObjectPool<EnemyUnit>>();
        private readonly Dictionary<EnemyUnit, ObjectPool<EnemyUnit>> _owner = new Dictionary<EnemyUnit, ObjectPool<EnemyUnit>>();

        public EnemyUnit Get(EnemyUnit prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<EnemyUnit>(
                    createFunc: () => Instantiate(prefab, transform),
                    actionOnGet: e => e.gameObject.SetActive(true),
                    actionOnRelease: e => e.gameObject.SetActive(false),
                    // 플레이 모드 종료 시 씬 언로드로 이미 파괴된 뒤 Clear가 불릴 수 있다.
                    actionOnDestroy: e => { if (e != null) Destroy(e.gameObject); },
                    collectionCheck: false,
                    defaultCapacity: defaultCapacity,
                    maxSize: maxSizePerType);
                _pools.Add(prefab, pool);
            }
            EnemyUnit enemy = pool.Get();
            _owner[enemy] = pool;
            return enemy;
        }

        public void Release(EnemyUnit enemy)
        {
            if (_owner.TryGetValue(enemy, out var pool)) pool.Release(enemy);
            else enemy.gameObject.SetActive(false);
        }
    }
}

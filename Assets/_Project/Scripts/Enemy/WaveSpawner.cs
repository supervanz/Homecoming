using System.Collections;
using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Enemy
{
    /// <summary>StageTimeline을 시간 순으로 읽어 편대를 스폰한다.</summary>
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private StageTimeline timeline;
        [SerializeField] private StageContext context;
        [SerializeField] private EnemyPool pool;

        private int _next;

        /// <summary>스테이지 시작 후 경과 시간 (초)</summary>
        public float Elapsed { get; private set; }
        public bool Finished => _next >= timeline.entries.Length;

        private void Update()
        {
            Elapsed += Time.deltaTime;
            while (_next < timeline.entries.Length && timeline.entries[_next].time <= Elapsed)
            {
                SpawnEntry entry = timeline.entries[_next];
                _next++;
                if (entry.enemy == null || entry.formation == null || entry.formation.path == null)
                {
                    Debug.LogWarning($"Timeline entry at {entry.time}s is missing enemy/formation/path.", timeline);
                    continue;
                }
                StartCoroutine(SpawnFormation(entry));
            }
        }

        private IEnumerator SpawnFormation(SpawnEntry entry)
        {
            FormationConfig f = entry.formation;
            Vector3 center = context.PlayArea.Center;
            float sign = entry.mirror ? -1f : 1f;

            for (int i = 0; i < f.memberOffsets.Length; i++)
            {
                if (i > 0 && f.memberDelay > 0f) yield return new WaitForSeconds(f.memberDelay);
                Vector2 offset = f.memberOffsets[i];
                Vector3 origin = center + new Vector3(entry.spawnPosition.x + sign * offset.x, 0f, entry.spawnPosition.y + offset.y);
                EnemyUnit enemy = pool.Get(entry.enemy.prefab);
                enemy.Spawn(entry.enemy, f.path, origin, entry.mirror, context, pool);
            }
        }
    }
}

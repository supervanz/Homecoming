using Homecoming.Core;
using Homecoming.Enemy;
using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>타임라인 시각에 맞춰 WARNING을 띄우고 보스를 등장시킨다.</summary>
    public class BossDirector : MonoBehaviour
    {
        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private BossController boss;
        [SerializeField] private StageContext context;
        [SerializeField] private EnemyPool enemyPool;

        public bool BossSpawned { get; private set; }
        public bool WarningActive => !BossSpawned && spawner.Elapsed >= spawner.Timeline.bossWarningTime;
        public bool StageCleared { get; private set; }
        public BossController Boss => boss;

        private void Awake()
        {
            boss.gameObject.SetActive(false);
            boss.Defeated += OnBossDefeated;
        }

        private void OnDestroy()
        {
            if (boss != null) boss.Defeated -= OnBossDefeated;
        }

        private void Update()
        {
            if (BossSpawned || spawner.Elapsed < spawner.Timeline.bossSpawnTime) return;
            BossSpawned = true;
            boss.gameObject.SetActive(true);
            boss.Begin(context, enemyPool);
        }

        private void OnBossDefeated()
        {
            StageCleared = true;
        }
    }
}

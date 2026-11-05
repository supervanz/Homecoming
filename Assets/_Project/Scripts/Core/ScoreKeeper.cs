using Homecoming.Boss;
using Homecoming.Enemy;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>격파·부위 파괴·최대 단계 파워업 이벤트를 받아 점수를 합산한다. 점수 값은 각 설정 에셋에 있다.</summary>
    public class ScoreKeeper : MonoBehaviour
    {
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private PlayerWeapon playerWeapon;
        [SerializeField] private PowerUpConfig powerUpConfig;

        public int Score { get; private set; }

        private void OnEnable()
        {
            EnemyUnit.Killed += OnEnemyKilled;
            playerWeapon.MaxLevelPowerUpCollected += OnMaxLevelPowerUp;
            BossController boss = bossDirector.Boss;
            boss.Defeated += OnBossDefeated;
            foreach (var p in boss.Panels) p.Destroyed += OnPanelDestroyed;
        }

        private void OnDisable()
        {
            EnemyUnit.Killed -= OnEnemyKilled;
            if (playerWeapon != null) playerWeapon.MaxLevelPowerUpCollected -= OnMaxLevelPowerUp;
            if (bossDirector == null || bossDirector.Boss == null) return;
            BossController boss = bossDirector.Boss;
            boss.Defeated -= OnBossDefeated;
            foreach (var p in boss.Panels) p.Destroyed -= OnPanelDestroyed;
        }

        private void Add(int points) => Score += Mathf.Max(0, points);

        private void OnEnemyKilled(EnemyUnit enemy) => Add(enemy.Config.score);
        private void OnMaxLevelPowerUp() => Add(powerUpConfig.maxLevelBonus);
        private void OnPanelDestroyed(BossPart _) => Add(bossDirector.Boss.Config.panelScore);
        private void OnBossDefeated() => Add(bossDirector.Boss.Config.defeatScore);
    }
}

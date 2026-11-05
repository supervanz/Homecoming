using Homecoming.Core;
using Homecoming.Enemy;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>적이 격파되면 드롭 확률(또는 확정 드롭)에 따라 파워업을 떨어뜨린다.</summary>
    public class PowerUpDropper : MonoBehaviour
    {
        [SerializeField] private PowerUpConfig config;
        [SerializeField] private PowerUpPool pool;
        [SerializeField] private PlayArea playArea;

        public int Dropped { get; private set; }

        private void OnEnable() => EnemyUnit.Killed += OnEnemyKilled;
        private void OnDisable() => EnemyUnit.Killed -= OnEnemyKilled;

        private void OnEnemyKilled(EnemyUnit enemy)
        {
            bool drop = enemy.CarriesDrop || GameRandom.Value < enemy.Config.powerUpDropChance;
            if (!drop) return;
            pool.Get().Launch(enemy.transform.position, config, playArea, pool);
            Dropped++;
        }
    }
}

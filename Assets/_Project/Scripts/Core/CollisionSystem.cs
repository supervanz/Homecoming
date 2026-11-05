using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 모든 충돌을 한곳에서 원-원 거리로 판정한다. 이동이 끝난 뒤 실행된다.
    /// 플레이어 탄 → 적, 적 탄 → 플레이어, 적 기체 → 플레이어(몸통 충돌).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CollisionSystem : MonoBehaviour
    {
        [SerializeField] private BulletPool playerBullets;
        [SerializeField] private BulletPool enemyBullets;
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private CircleHitbox playerHitbox;

        private void Update()
        {
            CheckPlayerBullets();
            CheckPlayer();
        }

        private void CheckPlayerBullets()
        {
            var enemies = EnemyRegistry.Active;
            var bullets = playerBullets.Active;
            for (int b = bullets.Count - 1; b >= 0; b--)
            {
                if (b >= bullets.Count) continue;
                Bullet bullet = bullets[b];
                for (int e = enemies.Count - 1; e >= 0; e--)
                {
                    EnemyUnit enemy = enemies[e];
                    if (!enemy.Hitbox.Overlaps(bullet.Position, bullet.Radius)) continue;
                    enemy.TakeDamage(bullet.Damage);
                    bullet.Despawn();
                    break;
                }
            }
        }

        private void CheckPlayer()
        {
            if (!player.CanBeHit) return;

            var bullets = enemyBullets.Active;
            for (int b = bullets.Count - 1; b >= 0; b--)
            {
                Bullet bullet = bullets[b];
                if (!playerHitbox.Overlaps(bullet.Position, bullet.Radius)) continue;
                player.ReceiveHit(bullet.Damage);
                bullet.Despawn();
                return;
            }

            var enemies = EnemyRegistry.Active;
            for (int e = enemies.Count - 1; e >= 0; e--)
            {
                EnemyUnit enemy = enemies[e];
                if (!playerHitbox.Overlaps(enemy.Hitbox.Center, enemy.Hitbox.Radius)) continue;
                player.ReceiveHit(enemy.ContactDamage);
                return;
            }
        }
    }
}

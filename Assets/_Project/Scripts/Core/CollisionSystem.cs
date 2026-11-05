using Homecoming.Boss;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 모든 충돌을 한곳에서 평면 거리로 판정한다. 이동이 끝난 뒤 실행된다.
    /// 플레이어 탄 → 맞을 수 있는 대상, 파워업 → 플레이어, 적 탄·레이저·몸통 → 플레이어.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CollisionSystem : MonoBehaviour
    {
        [SerializeField] private BulletPool playerBullets;
        [SerializeField] private BulletPool enemyBullets;
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private CircleHitbox playerHitbox;
        [SerializeField] private PowerUpPool powerUps;
        [SerializeField] private PlayerWeapon playerWeapon;

        private void Update()
        {
            CheckPlayerBullets();
            CheckPowerUps();
            CheckPlayer();
        }

        private void CheckPlayerBullets()
        {
            var targets = HitTargetRegistry.Active;
            var bullets = playerBullets.Active;
            for (int b = bullets.Count - 1; b >= 0; b--)
            {
                if (b >= bullets.Count) continue;
                Bullet bullet = bullets[b];
                for (int t = targets.Count - 1; t >= 0; t--)
                {
                    if (t >= targets.Count) continue;
                    HitTarget target = targets[t];
                    if (!target.Hitbox.Overlaps(bullet.Position, bullet.Radius)) continue;
                    target.TakeDamage(bullet.Damage);
                    bullet.Despawn();
                    break;
                }
            }
        }

        // 무적 중에도 아이템은 먹을 수 있다.
        private void CheckPowerUps()
        {
            if (!player.IsAlive) return;
            var items = powerUps.Active;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                PowerUpItem item = items[i];
                if (!playerHitbox.Overlaps(item.Position, item.Radius)) continue;
                playerWeapon.LevelUp();
                item.Despawn();
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

            var lasers = LaserRegistry.Active;
            for (int i = lasers.Count - 1; i >= 0; i--)
            {
                if (!lasers[i].Hits(playerHitbox.Center, playerHitbox.Radius)) continue;
                player.ReceiveHit(lasers[i].Damage);
                return;
            }

            var targets = HitTargetRegistry.Active;
            for (int t = targets.Count - 1; t >= 0; t--)
            {
                HitTarget target = targets[t];
                if (target.ContactDamage <= 0) continue;
                if (!playerHitbox.Overlaps(target.Hitbox.Center, target.Hitbox.Radius)) continue;
                player.ReceiveHit(target.ContactDamage);
                return;
            }
        }
    }
}

using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>탄 패턴 한 번 분량을 한 지점에서 발사한다. 일반 적과 보스가 같이 쓴다.</summary>
    public static class BulletEmitter
    {
        /// <param name="insideMargin">이만큼 영역 안쪽에 있어야 발사 (화면 밖 저격 방지)</param>
        public static void Fire(BulletPatternConfig p, Vector3 origin, float extraAngle, StageContext ctx, float insideMargin)
        {
            origin.y = 0f;
            if (!ctx.PlayArea.Contains(origin, -insideMargin)) return;

            Vector3 baseDir = Vector3.back;
            if (p.aimAtPlayer && ctx.PlayerAlive)
            {
                Vector3 toPlayer = ctx.Player.position - origin;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.0001f) baseDir = toPlayer.normalized;
            }

            int n = Mathf.Max(1, p.bulletCount);
            for (int i = 0; i < n; i++)
            {
                float angle = p.type == BulletPatternType.Ring
                    ? 360f * i / n
                    : (n == 1 ? 0f : Mathf.Lerp(-p.spreadAngle * 0.5f, p.spreadAngle * 0.5f, i / (float)(n - 1)));
                Vector3 dir = Quaternion.Euler(0f, angle + extraAngle, 0f) * baseDir;
                ctx.EnemyBullets.Get().Launch(origin, dir, p.bulletSpeed, ctx.PlayArea, ctx.EnemyBullets);
            }
        }
    }
}

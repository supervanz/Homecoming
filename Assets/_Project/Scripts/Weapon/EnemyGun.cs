using System.Collections;
using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Weapon
{
    /// <summary>탄 패턴 목록을 순서대로 반복 발사한다. 플레이 영역 안에 있을 때만 쏜다.</summary>
    public class EnemyGun : MonoBehaviour
    {
        [Tooltip("이만큼 영역 안쪽에 들어와야 발사 (화면 밖 저격 방지)")]
        [SerializeField] private float insideMargin = 0.5f;

        private BulletPatternConfig[] _patterns;
        private float _interval;
        private StageContext _ctx;
        private Coroutine _loop;

        public void Begin(BulletPatternConfig[] patterns, float interval, float firstDelay, StageContext ctx)
        {
            Stop();
            if (patterns == null || patterns.Length == 0) return;
            _patterns = patterns;
            _interval = interval;
            _ctx = ctx;
            _loop = StartCoroutine(FireLoop(firstDelay));
        }

        public void Stop()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        private void OnDisable() => Stop();

        private IEnumerator FireLoop(float firstDelay)
        {
            yield return new WaitForSeconds(firstDelay);
            int index = 0;
            while (true)
            {
                BulletPatternConfig p = _patterns[index % _patterns.Length];
                index++;
                for (int b = 0; b < p.burstCount; b++)
                {
                    FireOnce(p, b * p.rotationPerBurst);
                    if (b < p.burstCount - 1) yield return new WaitForSeconds(p.burstInterval);
                }
                yield return new WaitForSeconds(_interval);
            }
        }

        private void FireOnce(BulletPatternConfig p, float extraAngle)
        {
            Vector3 origin = transform.position;
            origin.y = 0f;
            if (!_ctx.PlayArea.Contains(origin, -insideMargin)) return;

            Vector3 baseDir = Vector3.back;
            if (p.aimAtPlayer && _ctx.PlayerAlive)
            {
                Vector3 toPlayer = _ctx.Player.position - origin;
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
                _ctx.EnemyBullets.Get().Launch(origin, dir, p.bulletSpeed, _ctx.PlayArea, _ctx.EnemyBullets);
            }
        }
    }
}

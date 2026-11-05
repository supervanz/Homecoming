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

        private string _sourceTag;

        public void Begin(BulletPatternConfig[] patterns, float interval, float firstDelay, StageContext ctx, string sourceTag = null)
        {
            Stop();
            if (patterns == null || patterns.Length == 0) return;
            _sourceTag = sourceTag;
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
                    BulletEmitter.Fire(p, transform.position, b * p.rotationPerBurst, _ctx, insideMargin, _sourceTag);
                    if (b < p.burstCount - 1) yield return new WaitForSeconds(p.burstInterval);
                }
                yield return new WaitForSeconds(_interval);
            }
        }
    }
}

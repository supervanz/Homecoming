using System.Collections;
using System.Collections.Generic;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Boss
{
    public enum BossEmitter
    {
        Core,
        /// <summary>살아 있는 태양전지판 포대마다. 부서진 포대는 쏘지 않는다.</summary>
        Panels,
        CoreAndPanels,
    }

    /// <summary>탄 패턴을 지정한 발사 지점에서 반복 발사한다.</summary>
    [CreateAssetMenu(menuName = "Homecoming/Boss/Bullet Action", fileName = "BA_Bullet")]
    public class BossBulletAction : BossActionConfig
    {
        public BulletPatternConfig pattern;
        public BossEmitter emitter = BossEmitter.Core;
        [Tooltip("패턴을 몇 번 반복할지")]
        [Min(1)] public int repeat = 1;
        [Tooltip("반복 사이 간격 (초)")]
        public float repeatInterval = 0.5f;

        public override IEnumerator Run(BossController boss)
        {
            var origins = new List<Vector3>(3);
            for (int r = 0; r < repeat; r++)
            {
                for (int b = 0; b < pattern.burstCount; b++)
                {
                    boss.GetEmitterPositions(emitter, origins);
                    foreach (var o in origins)
                        BulletEmitter.Fire(pattern, o, b * pattern.rotationPerBurst, boss.Context, 0f, name);
                    if (b < pattern.burstCount - 1) yield return new WaitForSeconds(pattern.burstInterval);
                }
                if (r < repeat - 1) yield return new WaitForSeconds(repeatInterval);
            }
        }
    }
}

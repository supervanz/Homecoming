using System.Collections;
using Homecoming.Enemy;
using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>보스 위치 기준으로 편대를 소환한다 (소형 위성 드론).</summary>
    [CreateAssetMenu(menuName = "Homecoming/Boss/Summon Action", fileName = "BA_Summon")]
    public class BossSummonAction : BossActionConfig
    {
        public EnemyConfig enemy;
        [Tooltip("오프셋은 보스 코어 기준")]
        public FormationConfig formation;
        [Tooltip("소환 후 다음 행동까지 대기 (초)")]
        public float waitAfter = 1f;

        /// <summary>소환을 시작할 때 (사운드용)</summary>
        public static event System.Action Summoned;

        public override IEnumerator Run(BossController boss)
        {
            Summoned?.Invoke();
            Vector3 center = boss.CorePosition;
            for (int i = 0; i < formation.memberOffsets.Length; i++)
            {
                if (i > 0 && formation.memberDelay > 0f) yield return new WaitForSeconds(formation.memberDelay);
                Vector2 o = formation.memberOffsets[i];
                Vector3 origin = center + new Vector3(o.x, 0f, o.y);
                EnemyUnit e = boss.EnemyPool.Get(enemy.prefab);
                e.Spawn(enemy, formation.path, origin, false, false, boss.Context, boss.EnemyPool);
            }
            if (waitAfter > 0f) yield return new WaitForSeconds(waitAfter);
        }
    }
}

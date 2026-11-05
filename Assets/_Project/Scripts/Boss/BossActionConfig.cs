using System.Collections;
using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>보스 행동 하나 (탄막, 레이저, 소환). 페이즈별로 순서대로 반복된다.</summary>
    public abstract class BossActionConfig : ScriptableObject
    {
        [Tooltip("켜면 이 행동을 시작만 하고 바로 다음 행동으로 넘어간다 (동시 실행)")]
        public bool runInParallel;

        public abstract IEnumerator Run(BossController boss);
    }
}

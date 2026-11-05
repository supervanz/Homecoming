using System.Collections.Generic;

namespace Homecoming.Enemy
{
    /// <summary>현재 살아 있는 적 목록. 충돌 판정과 (나중에) 봇·폭탄이 읽는다.</summary>
    public static class EnemyRegistry
    {
        private static readonly List<EnemyUnit> _active = new List<EnemyUnit>();

        public static IReadOnlyList<EnemyUnit> Active => _active;

        public static void Add(EnemyUnit enemy)
        {
            if (!_active.Contains(enemy)) _active.Add(enemy);
        }

        public static void Remove(EnemyUnit enemy) => _active.Remove(enemy);

        // Enter Play Mode 옵션으로 도메인 리로드를 끈 경우에도 이전 플레이의 잔여 목록을 비운다.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _active.Clear();
    }
}

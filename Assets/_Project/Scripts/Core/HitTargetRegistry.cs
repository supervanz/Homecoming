using System.Collections.Generic;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>지금 맞을 수 있는 대상 목록. 충돌 판정과 폭탄이 읽는다.</summary>
    public static class HitTargetRegistry
    {
        private static readonly List<HitTarget> _active = new List<HitTarget>();

        public static IReadOnlyList<HitTarget> Active => _active;

        public static void Add(HitTarget target)
        {
            if (!_active.Contains(target)) _active.Add(target);
        }

        public static void Remove(HitTarget target) => _active.Remove(target);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _active.Clear();
    }
}

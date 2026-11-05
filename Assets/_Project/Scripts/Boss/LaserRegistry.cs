using System.Collections.Generic;
using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>지금 발사 중인 (판정이 살아 있는) 레이저 목록.</summary>
    public static class LaserRegistry
    {
        private static readonly List<BossLaser> _active = new List<BossLaser>();

        public static IReadOnlyList<BossLaser> Active => _active;

        public static void Add(BossLaser laser)
        {
            if (!_active.Contains(laser)) _active.Add(laser);
        }

        public static void Remove(BossLaser laser) => _active.Remove(laser);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _active.Clear();
    }
}

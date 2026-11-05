using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 게임 로직용 난수. 시드를 기록해 두면 같은 판(드롭 등)을 그대로 재현할 수 있다.
    /// 연출용 난수(파티클 등)에는 쓰지 않는다.
    /// </summary>
    public static class GameRandom
    {
        private static System.Random _rng;

        public static int Seed { get; private set; }

        public static void Init(int seed)
        {
            Seed = seed;
            _rng = new System.Random(seed);
        }

        /// <summary>0 이상 1 미만</summary>
        public static float Value
        {
            get
            {
                if (_rng == null) Init(System.Environment.TickCount);
                return (float)_rng.NextDouble();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _rng = null;
    }
}

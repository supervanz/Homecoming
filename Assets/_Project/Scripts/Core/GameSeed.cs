using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>판 시작 시 시드를 정해 GameRandom을 초기화하고 콘솔에 기록한다.</summary>
    [DefaultExecutionOrder(-200)]
    public class GameSeed : MonoBehaviour
    {
        [Tooltip("켜면 아래 시드로 고정 (재현용)")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed;

        /// <summary>시뮬레이션이 판마다 지정하는 시드. 있으면 씬 설정보다 우선한다.</summary>
        public static int? ForcedSeed { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => ForcedSeed = null;

        private void Awake()
        {
            int seed = ForcedSeed ?? (useFixedSeed ? fixedSeed : System.Environment.TickCount);
            GameRandom.Init(seed);
            Debug.Log($"[GameSeed] seed = {seed}");
        }
    }
}

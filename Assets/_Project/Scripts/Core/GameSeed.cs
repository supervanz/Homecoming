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

        private void Awake()
        {
            int seed = useFixedSeed ? fixedSeed : System.Environment.TickCount;
            GameRandom.Init(seed);
            Debug.Log($"[GameSeed] seed = {seed}");
        }
    }
}

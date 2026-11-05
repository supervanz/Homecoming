using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>이 씬에서 쓸 난이도 프리셋. 다른 스크립트의 Awake보다 먼저 적용된다.</summary>
    [DefaultExecutionOrder(-300)]
    public class DifficultySelector : MonoBehaviour
    {
        [SerializeField] private DifficultyPreset preset;

        private void Awake()
        {
            Difficulty.SetSceneDefault(preset);
            Debug.Log($"[Difficulty] {Difficulty.Current.name}{(Difficulty.Override != null ? " (simulation override)" : "")}");
        }
    }
}

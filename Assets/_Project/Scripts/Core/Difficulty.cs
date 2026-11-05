using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 지금 적용 중인 난이도 프리셋. 수치를 읽는 스크립트들이 이 값을 거쳐 간다.
    /// 시뮬레이션은 Override로 씬 설정보다 우선해 프리셋을 고정한다.
    /// </summary>
    public static class Difficulty
    {
        private static DifficultyPreset _sceneDefault;
        private static DifficultyPreset _neutral;

        /// <summary>시뮬레이션이 지정한 프리셋. 있으면 씬 설정보다 우선한다.</summary>
        public static DifficultyPreset Override { get; set; }

        public static DifficultyPreset Current
        {
            get
            {
                if (Override != null) return Override;
                if (_sceneDefault != null) return _sceneDefault;
                if (_neutral == null) _neutral = ScriptableObject.CreateInstance<DifficultyPreset>();
                return _neutral;
            }
        }

        public static void SetSceneDefault(DifficultyPreset preset) => _sceneDefault = preset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _sceneDefault = null;
            Override = null;
        }
    }
}

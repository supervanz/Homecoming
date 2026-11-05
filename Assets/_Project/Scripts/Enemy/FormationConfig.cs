using UnityEngine;

namespace Homecoming.Enemy
{
    /// <summary>편대 데이터: 몇 기가, 출발점 기준 어디에서, 얼마 간격으로, 어떤 경로로 나오는지.</summary>
    [CreateAssetMenu(menuName = "Homecoming/Formation", fileName = "Formation")]
    public class FormationConfig : ScriptableObject
    {
        [Tooltip("편대원별 출발 위치 오프셋 (X = 가로, Y = 세로). 개수 = 편대원 수")]
        public Vector2[] memberOffsets = { Vector2.zero };
        [Tooltip("편대원 사이 등장 시간 간격 (초)")]
        public float memberDelay = 0f;
        public PathConfig path;
    }
}

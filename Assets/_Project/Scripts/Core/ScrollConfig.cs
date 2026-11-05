using UnityEngine;

namespace Homecoming.Core
{
    [CreateAssetMenu(menuName = "Homecoming/Scroll Config", fileName = "ScrollConfig")]
    public class ScrollConfig : ScriptableObject
    {
        [Tooltip("배경이 아래로 흐르는 속도 (유닛/초). 궤도 포대도 이 속도로 내려온다.")]
        public float scrollSpeed = 3f;

        [Header("Background stars (primitive)")]
        public int starCount = 80;
        [Tooltip("별을 깔 영역 크기 (플레이 영역보다 넓게)")]
        public Vector2 fieldSize = new Vector2(30f, 30f);
        [Tooltip("플레이 평면 아래 깊이")]
        public float depth = 4f;
        public float minStarSize = 0.05f;
        public float maxStarSize = 0.15f;
        [Tooltip("별마다 스크롤 속도에 곱하는 배율 범위 (원근감)")]
        public Vector2 parallaxRange = new Vector2(0.5f, 1.2f);
        public int seed = 1234;
    }
}

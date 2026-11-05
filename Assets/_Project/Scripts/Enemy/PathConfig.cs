using System;
using UnityEngine;

namespace Homecoming.Enemy
{
    [Serializable]
    public struct PathPoint
    {
        [Tooltip("출발점 기준 상대 위치 (X = 가로, Y = 세로/Z축, 위가 +)")]
        public Vector2 position;
        [Tooltip("이 점에 도착한 뒤 멈춰 있는 시간 (초)")]
        public float wait;
    }

    /// <summary>적 이동 경로 데이터. 편대들이 재사용한다.</summary>
    [CreateAssetMenu(menuName = "Homecoming/Path", fileName = "Path")]
    public class PathConfig : ScriptableObject
    {
        public PathPoint[] points = { new PathPoint { position = Vector2.zero }, new PathPoint { position = new Vector2(0f, -22f) } };
        [Tooltip("이동 속도 (유닛/초)")]
        public float speed = 6f;
        [Tooltip("켜면 speed 대신 배경 스크롤 속도로 이동 (궤도 포대)")]
        public bool useScrollSpeed = false;
        [Tooltip("켜면 점 사이를 곡선(Catmull-Rom)으로 잇는다")]
        public bool smooth = false;
        [Tooltip("곡선 한 구간을 나누는 수")]
        [Min(1)] public int smoothSubdivisions = 8;
    }
}

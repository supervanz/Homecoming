using System;
using UnityEngine;

namespace Homecoming.Enemy
{
    [Serializable]
    public class SpawnEntry
    {
        [Tooltip("스테이지 시작 후 등장 시각 (초)")]
        public float time;
        public EnemyConfig enemy;
        public FormationConfig formation;
        [Tooltip("플레이 영역 중심 기준 출발 위치 (X = 가로, Y = 세로)")]
        public Vector2 spawnPosition;
        [Tooltip("켜면 경로와 오프셋을 좌우 반전")]
        public bool mirror;
    }

    /// <summary>스테이지 전체 웨이브 구성. 시간·적 종류·편대를 코드 수정 없이 바꾼다.</summary>
    [CreateAssetMenu(menuName = "Homecoming/Stage Timeline", fileName = "StageTimeline")]
    public class StageTimeline : ScriptableObject
    {
        [Tooltip("시간 순으로 정렬해 둔다")]
        public SpawnEntry[] entries = new SpawnEntry[0];
    }
}

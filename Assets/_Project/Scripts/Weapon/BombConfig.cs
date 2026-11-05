using UnityEngine;

namespace Homecoming.Weapon
{
    [CreateAssetMenu(menuName = "Homecoming/Bomb Config", fileName = "BombConfig")]
    public class BombConfig : ScriptableObject
    {
        [Tooltip("시작 폭탄 개수")]
        public int startCount = 3;
        [Tooltip("발동 순간 화면의 모든 적에게 주는 피해")]
        public int damage = 30;
        [Tooltip("지속 시간 (초). 이 동안 적 탄을 계속 지우고 플레이어는 무적")]
        public float duration = 1f;

        [Header("Placeholder visual (M8에서 교체)")]
        [Tooltip("퍼지는 원판의 최대 지름")]
        public float waveMaxSize = 30f;
        [Tooltip("원판 시작 불투명도")]
        [Range(0f, 1f)] public float waveStartAlpha = 0.45f;
    }
}

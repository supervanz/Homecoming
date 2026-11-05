using UnityEngine;

namespace Homecoming.Player
{
    [CreateAssetMenu(menuName = "Homecoming/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("이동 속도 (유닛/초)")]
        public float moveSpeed = 9f;
        [Tooltip("플레이 영역 가장자리에서 기체 중심이 유지할 여백")]
        public float edgeMargin = 0.6f;

        [Header("Damage")]
        [Tooltip("최대 체력")]
        public int maxHp = 5;
        [Tooltip("피격 판정 반경. 기체 외형보다 작게 잡는다.")]
        public float hitRadius = 0.25f;
        [Tooltip("피격 후 무적 시간 (초)")]
        public float invincibleTime = 1.5f;
        [Tooltip("무적 중 깜빡임 간격 (초)")]
        public float blinkInterval = 0.08f;

        [Header("Roll (외형 연출)")]
        [Tooltip("좌우 최대 속도일 때 기울기 각도")]
        public float rollMaxAngle = 30f;
        [Tooltip("기울기가 목표 각도를 따라가는 빠르기 (클수록 빠름)")]
        public float rollResponse = 10f;
    }
}

using UnityEngine;

namespace Homecoming.Weapon
{
    public enum BulletPatternType
    {
        /// <summary>기준 방향을 중심으로 spreadAngle만큼 퍼진 부채꼴. 1발이면 단발 조준탄.</summary>
        Fan,
        /// <summary>360도 원형 방사</summary>
        Ring,
    }

    [CreateAssetMenu(menuName = "Homecoming/Bullet Pattern", fileName = "BulletPattern")]
    public class BulletPatternConfig : ScriptableObject
    {
        public BulletPatternType type = BulletPatternType.Fan;
        [Tooltip("한 번에 쏘는 탄 수")]
        [Min(1)] public int bulletCount = 1;
        [Tooltip("부채꼴 전체 각도 (Fan만 사용)")]
        public float spreadAngle = 0f;
        [Tooltip("탄 속도 (유닛/초)")]
        public float bulletSpeed = 9f;
        [Tooltip("기준 방향을 플레이어 쪽으로 맞출지. 끄면 화면 아래쪽")]
        public bool aimAtPlayer = true;
        [Tooltip("연발 횟수")]
        [Min(1)] public int burstCount = 1;
        [Tooltip("연발 사이 간격 (초)")]
        public float burstInterval = 0.12f;
        [Tooltip("연발마다 기준 각도를 이만큼 돌림 (회전 링 등)")]
        public float rotationPerBurst = 0f;
    }
}

using UnityEngine;

namespace Homecoming.Weapon
{
    [CreateAssetMenu(menuName = "Homecoming/Weapon Config", fileName = "WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        [Tooltip("연사 간격 (초)")]
        public float fireInterval = 0.1f;
        [Tooltip("탄 속도 (유닛/초)")]
        public float bulletSpeed = 28f;
        [Tooltip("한 번에 쏘는 탄 수")]
        [Min(1)] public int bulletCount = 1;
        [Tooltip("탄 여러 발의 전체 퍼짐 각도 (도)")]
        public float spreadAngle = 0f;
    }
}

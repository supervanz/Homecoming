using System;
using UnityEngine;

namespace Homecoming.Weapon
{
    [Serializable]
    public class WeaponLevel
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

    /// <summary>무기 단계별 수치. 배열 길이 = 최대 단계.</summary>
    [CreateAssetMenu(menuName = "Homecoming/Weapon Config", fileName = "WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        public WeaponLevel[] levels =
        {
            new WeaponLevel { bulletCount = 1, spreadAngle = 0f },
            new WeaponLevel { bulletCount = 3, spreadAngle = 12f },
            new WeaponLevel { bulletCount = 5, spreadAngle = 24f },
        };

        public int MaxLevel => levels.Length;

        /// <summary>1부터 시작하는 단계 번호</summary>
        public WeaponLevel GetLevel(int level) => levels[Mathf.Clamp(level - 1, 0, levels.Length - 1)];
    }
}

using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Enemy
{
    [CreateAssetMenu(menuName = "Homecoming/Enemy Config", fileName = "EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        [Tooltip("외형 + EnemyUnit 컴포넌트가 붙은 프리팹")]
        public EnemyUnit prefab;

        [Header("Durability")]
        public int maxHp = 2;
        [Tooltip("판정 반경")]
        public float hitRadius = 0.5f;
        [Tooltip("플레이어와 몸통이 부딪혔을 때 주는 피해")]
        public int contactDamage = 1;

        [Header("Firing")]
        [Tooltip("순서대로 반복할 탄 패턴. 비우면 쏘지 않는다.")]
        public BulletPatternConfig[] patterns = new BulletPatternConfig[0];
        [Tooltip("패턴 사이 간격 (초)")]
        public float fireInterval = 1.5f;
        [Tooltip("등장 후 첫 발까지 대기 (초)")]
        public float firstShotDelay = 0.8f;
    }
}

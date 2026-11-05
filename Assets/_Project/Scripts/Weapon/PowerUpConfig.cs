using UnityEngine;

namespace Homecoming.Weapon
{
    [CreateAssetMenu(menuName = "Homecoming/Power-Up Config", fileName = "PowerUpConfig")]
    public class PowerUpConfig : ScriptableObject
    {
        [Tooltip("아래로 떨어지는 속도 (유닛/초)")]
        public float fallSpeed = 2.5f;
        [Tooltip("획득 판정 반경. 먹기 쉽게 크게 잡는다.")]
        public float pickupRadius = 0.8f;
        [Tooltip("플레이 영역 밖으로 이만큼 나가면 사라짐")]
        public float despawnMargin = 1f;
        [Tooltip("외형 회전 속도 (도/초, 연출)")]
        public float spinSpeed = 120f;
    }
}

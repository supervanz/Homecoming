using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>스폰된 적이 필요로 하는 씬 참조 묶음 (플레이어 위치, 영역, 적 탄 풀, 스크롤 속도).</summary>
    public class StageContext : MonoBehaviour
    {
        [SerializeField] private PlayArea playArea;
        [SerializeField] private Transform player;
        [SerializeField] private BulletPool enemyBullets;
        [SerializeField] private ScrollConfig scroll;

        public PlayArea PlayArea => playArea;
        public Transform Player => player;
        public bool PlayerAlive => player != null && player.gameObject.activeInHierarchy;
        public BulletPool EnemyBullets => enemyBullets;
        public float ScrollSpeed => scroll.scrollSpeed;
    }
}
